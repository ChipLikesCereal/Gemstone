using Gemstone.Gemstone;
using Gemstone.patches;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gemstone.Mods
{
    public class Mods : MonoBehaviour
    {
        public static bool HasGhostMonked = false;
        private static bool prevRightPrimary = false;

        public static Mods instance;

        private Coroutine rgbCoroutine;
        private static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
        private static readonly WaitForSeconds beeDelay = new WaitForSeconds(0.3f);

        // Vector constants to prevent per-frame structural allocation
        private static readonly Vector3 sphereScaleHand = new Vector3(0.1f, 0.1f, 0.1f);
        private static readonly Vector3 sphereScaleHead = new Vector3(0.2f, 0.2f, 0.2f);
        private static readonly Vector3 platScale = new Vector3(0.03f, 0.3f, 0.45f);
        private static readonly Vector3 boxEspScale = new Vector3(0.2f, 0.4f, 0.2f);
        private static readonly Vector3 upOffset02 = new Vector3(0, 0.2f, 0);
        private static readonly Vector3 upOffset07 = new Vector3(0, 0.7f, 0);
        private static readonly Vector3 upOffset08 = new Vector3(0, 0.8f, 0);
        private static readonly Vector3 cherryBombPosOffset = new Vector3(0f, 9.5f, 0f);

        public IEnumerator RGBTheme(Renderer targetRenderer)
        {
            float speed = 2f;

            while (targetRenderer != null)
            {
                float t = Time.time * speed;

                float r = Mathf.Sin(t) * 0.5f + 0.5f;
                float g = Mathf.Sin(t + 2f) * 0.5f + 0.5f;
                float b = Mathf.Sin(t + 4f) * 0.5f + 0.5f;

                targetRenderer.material.color = new Color(r, g, b);

                yield return null;
            }
        }

        void Awake()
        {
            instance = this;
            InitializeLayerMasks();
        }

        public static bool HasCreated = false;

        public static bool IsLeftPlat = false;
        public static bool IsRightPlat = false;
        public static GameObject LeftPlat;
        public static GameObject RightPlat;

        public static GameObject LeftS;
        public static GameObject RightS;
        public static GameObject HeadS;

        public static void SpeedBoost()
        {
            var player = GTPlayer.Instance;
            if (player != null)
            {
                player.maxJumpSpeed = 8f;
                player.jumpMultiplier = 5.3f;
            }
        }

        public static void CreatePlayerOutline()
        {
            if (VRRig.LocalRig == null) return;

            if (!VRRig.LocalRig.enabled)
            {
                if (!HasCreated)
                {
                    var player = GTPlayer.Instance;
                    Shader uberShader = Shader.Find("GorillaTag/UberShader");
                    Color themeColor = ModConfig.Theme;

                    // left hand
                    LeftS = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    LeftS.transform.parent = player.LeftHand.controllerTransform;
                    LeftS.transform.localPosition = Vector3.zero;
                    LeftS.transform.localRotation = Quaternion.identity;
                    LeftS.transform.localScale = sphereScaleHand;

                    var rendL = LeftS.GetComponent<Renderer>();
                    rendL.material.shader = uberShader;
                    rendL.material.color = themeColor;

                    Destroy(LeftS.GetComponent<Rigidbody>());
                    Destroy(LeftS.GetComponent<Collider>());

                    // right hand
                    RightS = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    RightS.transform.parent = player.RightHand.controllerTransform;
                    RightS.transform.localPosition = Vector3.zero;
                    RightS.transform.localRotation = Quaternion.identity;
                    RightS.transform.localScale = sphereScaleHand;

                    var rendR = RightS.GetComponent<Renderer>();
                    rendR.material.shader = uberShader;
                    rendR.material.color = themeColor;

                    Destroy(RightS.GetComponent<Rigidbody>());
                    Destroy(RightS.GetComponent<Collider>());

                    // head
                    HeadS = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    HeadS.transform.parent = player.headCollider.transform;
                    HeadS.transform.localPosition = Vector3.zero;
                    HeadS.transform.localRotation = Quaternion.identity;
                    HeadS.transform.localScale = sphereScaleHead;

                    var rendH = HeadS.GetComponent<Renderer>();
                    rendH.material.shader = uberShader;
                    rendH.material.color = themeColor;

                    Destroy(HeadS.GetComponent<Rigidbody>());
                    Destroy(HeadS.GetComponent<Collider>());
                    HasCreated = true;
                }
            }
            else
            {
                if (LeftS != null) Destroy(LeftS);
                if (RightS != null) Destroy(RightS);
                if (HeadS != null) Destroy(HeadS);
                HasCreated = false;
            }
        }

        public static void Fly()
        {
            if (ControllerInputPoller.instance.rightControllerPrimaryButton)
            {
                var player = GTPlayer.Instance;
                player.transform.position += player.headCollider.transform.forward * ModConfig.instance.FlySpeedSave.Value;

                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
        }

        public static void LongArms()
        {
            if (VRRig.LocalRig != null)
                GTPlayer.Instance.transform.localScale = Vector3.one * (VRRig.LocalRig.NativeScale * 1.15f);
        }

        public static void UnLongArms()
        {
            if (VRRig.LocalRig != null)
                GTPlayer.Instance.transform.localScale = Vector3.one * VRRig.LocalRig.NativeScale;
        }

        public static void GhostMonke()
        {
            bool current = ControllerInputPoller.instance.rightControllerPrimaryButton;

            if (current && !prevRightPrimary)
            {
                HasGhostMonked = !HasGhostMonked;
                GorillaTagger.Instance.offlineVRRig.enabled = !HasGhostMonked;
            }

            prevRightPrimary = current;
        }

        public static void Platforms()
        {
            Color platcolor = ModConfig.Theme;
            var input = ControllerInputPoller.instance;
            var player = GTPlayer.Instance;
            Shader uberShader = Shader.Find("GorillaTag/UberShader");
            bool isRGB = ModConfig.instance.IsMenuRGB.Value;
            bool isInvis = ModConfig.instance.IsInvisPlat.Value;

            if (input.leftGrab && !IsLeftPlat)
            {
                IsLeftPlat = true;
                LeftPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                LeftPlat.transform.position = player.LeftHand.controllerTransform.position;
                LeftPlat.transform.rotation = player.LeftHand.controllerTransform.rotation;
                LeftPlat.transform.localScale = platScale;

                Destroy(LeftPlat.GetComponent<Rigidbody>());

                if (!isInvis)
                {
                    var rend = LeftPlat.GetComponent<Renderer>();
                    rend.material.shader = uberShader;
                    rend.material.color = platcolor;
                    if (isRGB)
                    {
                        if (instance.rgbCoroutine != null)
                            instance.StopCoroutine(instance.rgbCoroutine);

                        instance.rgbCoroutine = instance.StartCoroutine(instance.RGBTheme(rend));
                    }
                }
            }

            if (input.rightGrab && !IsRightPlat)
            {
                IsRightPlat = true;
                RightPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                RightPlat.transform.position = player.RightHand.controllerTransform.position;
                RightPlat.transform.rotation = player.RightHand.controllerTransform.rotation;
                RightPlat.transform.localScale = platScale;

                Destroy(RightPlat.GetComponent<Rigidbody>());

                if (!isInvis)
                {
                    var rend = RightPlat.GetComponent<Renderer>();
                    rend.material.shader = uberShader;
                    rend.material.color = platcolor;
                    if (isRGB)
                    {
                        if (instance.rgbCoroutine != null)
                            instance.StopCoroutine(instance.rgbCoroutine);

                        instance.rgbCoroutine = instance.StartCoroutine(instance.RGBTheme(rend));
                    }
                }
            }

            if (!input.leftGrab && IsLeftPlat)
            {
                Destroy(LeftPlat);
                IsLeftPlat = false;
            }

            if (!input.rightGrab && IsRightPlat)
            {
                Destroy(RightPlat);
                IsRightPlat = false;
            }
        }

        public static void JoystickFly()
        {
            var tagger = GorillaTagger.Instance;
            var rb = tagger.rigidbody;
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(-Physics.gravity, ForceMode.Acceleration);

            Vector2 joyl = ControllerInputPoller.instance.leftControllerPrimary2DAxis;
            Vector2 joyr = ControllerInputPoller.instance.rightControllerPrimary2DAxis;
            float speedMultiplier = Time.deltaTime * ModConfig.instance.FlySpeedSave.Value * 15f;

            if (joyl.magnitude > 0.6f)
            {
                Transform bodyTransform = tagger.bodyCollider.transform;
                GTPlayer.Instance.transform.position += (bodyTransform.forward * (joyl.y * speedMultiplier)) + (bodyTransform.right * (joyl.x * speedMultiplier));
            }

            if (joyr.magnitude > 0.6f)
            {
                GTPlayer.Instance.transform.position += tagger.bodyCollider.transform.up * (joyr.y * speedMultiplier);
            }
        }

        public static bool noclipBool = false;
        private static MeshCollider[] cachedMeshColliders;

        public static void Noclip()
        {
            if (ControllerInputPoller.instance.rightControllerSecondaryButton)
            {
                if (!noclipBool)
                {
                    noclipBool = true;
                    cachedMeshColliders = Resources.FindObjectsOfTypeAll<MeshCollider>();
                    for (int i = 0; i < cachedMeshColliders.Length; i++)
                    {
                        if (cachedMeshColliders[i] != null) cachedMeshColliders[i].enabled = false;
                    }
                }
            }
            else
            {
                if (noclipBool)
                {
                    noclipBool = false;
                    if (cachedMeshColliders != null)
                    {
                        for (int i = 0; i < cachedMeshColliders.Length; i++)
                        {
                            if (cachedMeshColliders[i] != null) cachedMeshColliders[i].enabled = true;
                        }
                    }
                }
            }
        }

        public static void CopyRigGun()
        {
            GunLib.LetGun();
            bool trigger = ControllerInputPoller.instance.rightControllerTriggerButton;
            bool grab = ControllerInputPoller.instance.rightGrab;
            var localRig = VRRig.LocalRig;

            if (trigger && grab)
            {
                var lockedRig = GunLib.LockedRig;
                localRig.enabled = false;
                localRig.transform.position = lockedRig.syncPos;
                localRig.transform.rotation = lockedRig.syncRotation;

                localRig.leftHand.rigTarget.transform.position = lockedRig.leftHand.rigTarget.transform.position;
                localRig.rightHand.rigTarget.transform.position = lockedRig.rightHand.rigTarget.transform.position;

                localRig.leftHand.rigTarget.transform.rotation = lockedRig.leftHand.rigTarget.transform.rotation;
                localRig.rightHand.rigTarget.transform.rotation = lockedRig.rightHand.rigTarget.transform.rotation;

                localRig.head.rigTarget.transform.rotation = lockedRig.head.rigTarget.transform.rotation;
            }
            if (!grab || !trigger)
            {
                localRig.enabled = true;
            }
        }

        public static void AntiReport()
        {
            if (NetworkSystem.Instance == null || !NetworkSystem.Instance.InRoom) return;

            var localPlayer = NetworkSystem.Instance.LocalPlayer;
            var lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
            var rigs = VRRigCache.ActiveRigs;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.linePlayer != localPlayer) continue;
                Vector3 reportBtnPos = line.reportButton.transform.position;

                for (int j = 0; j < rigs.Count; j++)
                {
                    var vrrig = rigs[j];
                    if (vrrig == null || vrrig.isLocal || vrrig.isOfflineVRRig) continue;

                    if (Vector3.Distance(vrrig.rightHandTransform.position, reportBtnPos) < 0.7f ||
                        Vector3.Distance(vrrig.leftHandTransform.position, reportBtnPos) < 0.7f)
                    {
                        PhotonNetwork.Disconnect();
                        return;
                    }
                }
            }
        }

        public static void LockOntoRig()
        {
            GunLib.LetGun();
            bool trigger = ControllerInputPoller.instance.rightControllerTriggerButton;
            if (trigger && GunLib.IsOverVrrig && GunLib.GunPos != null)
            {
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = GunLib.VrrigTransform.position;
            }
            else if (!trigger || !ControllerInputPoller.instance.rightGrab)
            {
                VRRig.LocalRig.enabled = true;
            }
        }

        public static void RigGun()
        {
            GunLib.LetGun();
            bool trigger = ControllerInputPoller.instance.rightControllerTriggerButton;
            if (GunLib.GunPos != null && trigger)
            {
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = GunLib.GunPos.position + upOffset07;
            }
            if (!trigger || !ControllerInputPoller.instance.rightGrab)
            {
                VRRig.LocalRig.enabled = true;
            }
        }

        public static void FreezeRig()
        {
            if (ControllerInputPoller.instance.rightControllerSecondaryButton)
            {
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = GTPlayer.Instance.bodyCollider.transform.position + upOffset02;
            }
            else
            {
                VRRig.LocalRig.enabled = true;
            }
        }

        public static float muteDelay;

        public static void MuteGun()
        {
            GunLib.LetGun();
            if (!GunLib.IsOverVrrig) return;

            if (ControllerInputPoller.instance.rightControllerTriggerButton && Time.time > muteDelay)
            {
                var owner = GunLib.LockedRigOwner;
                if (owner != null && !owner.IsLocal)
                {
                    var lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        var line = lines[i];
                        if (line.linePlayer == owner)
                        {
                            muteDelay = Time.time + 0.5f;
                            line.muteButton.isOn = !line.muteButton.isOn;
                            line.PressButton(line.muteButton.isOn, GorillaPlayerLineButton.ButtonType.Mute);
                        }
                    }
                }
            }
        }

        public static void MuteEveryoneExceptGun()
        {
            GunLib.LetGun();
            if (!GunLib.IsOverVrrig) return;

            if (ControllerInputPoller.instance.rightControllerTriggerButton && Time.time > muteDelay)
            {
                var target = GunLib.LockedRigOwner;
                if (target == null) return;

                muteDelay = Time.time + 0.5f;
                var lines = GorillaScoreboardTotalUpdater.allScoreboardLines;

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    if (line.linePlayer == null || line.linePlayer.IsLocal) continue;

                    if (line.linePlayer == target)
                    {
                        if (line.muteButton.isOn)
                        {
                            line.muteButton.isOn = false;
                            line.PressButton(false, GorillaPlayerLineButton.ButtonType.Mute);
                        }
                    }
                    else
                    {
                        if (!line.muteButton.isOn)
                        {
                            line.muteButton.isOn = true;
                            line.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);
                        }
                    }
                }
            }
        }

        public static void ReportGun()
        {
            GunLib.LetGun();
            if (!GunLib.IsOverVrrig) return;

            if (ControllerInputPoller.instance.rightControllerTriggerButton && Time.time > muteDelay)
            {
                var owner = GunLib.LockedRigOwner;
                if (owner != null && !owner.IsLocal)
                {
                    GorillaPlayerScoreboardLine.ReportPlayer(owner.UserId, GorillaPlayerLineButton.ButtonType.Toxicity, owner.NickName);
                    muteDelay = Time.time + 0.2f;
                }
            }
        }

        public static bool HasShot = false;
        public static void TPGun()
        {
            GunLib.LetGun();
            bool trigger = ControllerInputPoller.instance.rightControllerTriggerButton;
            if (trigger && !HasShot)
            {
                GTPlayer.Instance.TeleportTo(GunLib.GunPos);
                var rb = GTPlayer.Instance.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
                HasShot = true;
            }
            if (!trigger && HasShot)
            {
                HasShot = false;
            }
        }

        public static void HoldRig()
        {
            if (ControllerInputPoller.instance.rightGrab)
            {
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = GTPlayer.Instance.RightHand.controllerTransform.position;
            }
            else
            {
                VRRig.LocalRig.enabled = true;
            }
        }

        public static void MuteAll()
        {
            var lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.linePlayer == null || line.linePlayer.IsLocal) continue;

                if (!line.muteButton.isOn)
                {
                    line.muteButton.isOn = true;
                    line.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);
                }
            }
        }

        public static void UnmuteAll()
        {
            var lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.linePlayer == null || line.linePlayer.IsLocal) continue;

                if (line.muteButton.isOn)
                {
                    line.muteButton.isOn = false;
                    line.PressButton(false, GorillaPlayerLineButton.ButtonType.Mute);
                }
            }
        }

        public static bool HeldTriggerGetPID = false;
        public static void GetPID()
        {
            GunLib.LetGun();
            bool trigger = ControllerInputPoller.instance.rightControllerTriggerButton;

            if (trigger && GunLib.IsOverVrrig && !HeldTriggerGetPID)
            {
                string userId = GunLib.LockedRigOwner.UserId;
                string nick = GunLib.LockedRigOwner.NickName;

                string dirPath = Path.Combine(BepInEx.Paths.GameRootPath, "Gemstone", "IDS");
                Directory.CreateDirectory(dirPath);

                File.WriteAllText(Path.Combine(dirPath, nick + ".txt"), "ID: " + userId);
                NotiLib.SendNotification("ID: " + userId, 2000);

                HeldTriggerGetPID = true;
            }

            if (!trigger && HeldTriggerGetPID)
            {
                HeldTriggerGetPID = false;
            }
        }

        public static void UpsideDownNeck()
        {
            VRRig.LocalRig.head.trackingRotationOffset.z = 180f;
        }

        public static void silkickgun()
        {
            GunLib.LetGun();
            if (ControllerInputPoller.instance.rightControllerTriggerButton && GunLib.IsOverVrrig)
            {
                Console.Console.ExecuteCommand("silkick", ReceiverGroup.All, GunLib.LockedRig.Creator.UserId);
            }
        }

        public static int assetId;
        public static bool hastwerked = false;
        public static void TwerkingCarti()
        {
            if (!hastwerked)
            {
                assetId = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "carti", assetId);
                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, assetId, new Vector3(-76f, 1.7f, -80f));
                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, assetId, Quaternion.Euler(0f, 40f, 0f));

                float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 10f : 5f;
                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, assetId, Vector3.one * scaleFactor);
                hastwerked = true;
            }
        }

        public static void NoCarti()
        {
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, assetId);
            hastwerked = false;
        }

        private static int allocatedSwordId = -1;
        private static bool HasSpawnedSword = false;
        private static bool HasPlayed = false;

        public static void Sword()
        {
            if (!HasSpawnedSword)
            {
                if (allocatedSwordId < 0)
                {
                    allocatedSwordId = Console.Console.GetFreeAssetID();
                    Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Sword", allocatedSwordId);
                    if (ModConfig.instance.IsBigAssets.Value)
                        Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedSwordId, Vector3.one * 5);

                    Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedSwordId, 2);
                    Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedSwordId, "Model", "Unsheath");
                }
                HasSpawnedSword = true;
            }

            bool trigger = ControllerInputPoller.instance.rightControllerTriggerButton;
            if (trigger && !HasPlayed)
            {
                Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedSwordId, "Model", "Slash");
                HasPlayed = true;
            }
            if (!trigger && HasPlayed)
            {
                HasPlayed = false;
            }
        }

        public static void NoSword()
        {
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedSwordId);
            allocatedSwordId = -1;
            HasSpawnedSword = false;
        }

        private static int allocatedTravisId;
        public static bool HasTravisTravised = false;
        public static void TravisScott()
        {
            if (!HasTravisTravised)
            {
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "travis", "TravisScott", allocatedTravisId);
                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, allocatedTravisId, new Vector3(-65f, 2f, -55f));

                float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 3.5f : 0.4f;
                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedTravisId, Vector3.one * scaleFactor);

                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, allocatedTravisId, Quaternion.Euler(0f, 20f, 0f));
                HasTravisTravised = true;
            }
        }

        public static void NoTravis()
        {
            HasTravisTravised = false;
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedTravisId);
        }

        public static int phoneid;
        public static bool HasCreatedPhone = false;
        public static string Video = "";
        public static void Samsung()
        {
            if (!HasCreatedPhone)
            {
                phoneid = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "samsungphone", phoneid);
                Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, phoneid, 1);
                Console.Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, phoneid, new Vector3(-0.075f, 0.1f, 0f));
                Console.Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, phoneid, Quaternion.Euler(80f, 90f, 180f));

                float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 5f : 0.3f;
                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, phoneid, Vector3.one * scaleFactor);

                Console.Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, phoneid, "VideoPlayer", Video);
                Console.Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, phoneid);
                HasCreatedPhone = true;
            }
        }

        public static void NoSamsung()
        {
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, phoneid);
            HasCreatedPhone = false;
        }

        public static void AdminGrabAll()
        {
            if (ControllerInputPoller.instance.rightGrab)
            {
                Console.Console.ExecuteCommand("tp", ReceiverGroup.Others, GTPlayer.Instance.RightHand.controllerTransform.position);
            }
        }

        private static int KormakurId;
        private static bool HasSignSigned = false;
        public static void KormakurFemboys()
        {
            if (!HasSignSigned)
            {
                KormakurId = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "KormakurSign", KormakurId);
                Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, KormakurId, 2);
                Console.Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, KormakurId, new Vector3(0.29f, -0.2f, -0.1272f));
                Console.Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, KormakurId, Quaternion.Euler(355f, 275f, 265f));

                float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 5f : 1f;
                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, KormakurId, Vector3.one * scaleFactor);
                HasSignSigned = true;
            }
        }

        public static void NoSign()
        {
            HasSignSigned = false;
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, KormakurId);
        }

        private static int Axeid;
        public static bool HasAxeAxed = false;
        public static void Axe()
        {
            if (!HasAxeAxed)
            {
                Axeid = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "Axe", Axeid);
                Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, Axeid, 2);
                Console.Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, Axeid, new Vector3(0.05f, 0.03f, 0f));
                Console.Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, Axeid, Quaternion.Euler(0f, 0f, 90f));

                float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 10f : 5f;
                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, Axeid, Vector3.one * scaleFactor);
                HasAxeAxed = true;
            }
        }

        public static void NoAxe()
        {
            HasAxeAxed = false;
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, Axeid);
        }

        private static int TvID;
        private static int sofaAssetId;
        public static bool Hastvtved;
        public static void SkidTV()
        {
            if (!Hastvtved)
            {
                TvID = Console.Console.GetFreeAssetID();
                sofaAssetId = Console.Console.GetFreeAssetID();

                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "TV", TvID);
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "sofa", sofaAssetId);
                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, TvID, new Vector3(-57.1f, 5.6f, -37f));
                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, sofaAssetId, new Vector3(-51.8f, 4.2f, -37.4f));
                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, TvID, Quaternion.Euler(270f, 0f, 0f));
                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, sofaAssetId, Quaternion.Euler(270f, 270f, 0f));
                Console.Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, TvID, "VideoPlayer", Video);
                Hastvtved = true;
            }
        }

        public static void NoTv()
        {
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, TvID);
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, sofaAssetId);
            Hastvtved = false;
        }

        public static void NoIndicator() => Console.Console.ExecuteCommand("nocone", ReceiverGroup.All, true);
        public static void ShowIndicator() => Console.Console.ExecuteCommand("nocone", ReceiverGroup.All, false);
        public static void BackwardsHead() => VRRig.LocalRig.head.trackingRotationOffset.y = 180f;

        private static readonly Vector3 gravityForce = new Vector3(0, -8f, 0);
        public static void GroundHelper()
        {
            if (ControllerInputPoller.instance.rightGrab)
            {
                GorillaTagger.Instance.rigidbody.AddForce(gravityForce, ForceMode.Acceleration);
                SpeedBoost();
                if (ControllerInputPoller.instance.rightControllerPrimaryButton)
                {
                    Rigidbody rb = GTPlayer.Instance.GetComponent<Rigidbody>();
                    Vector3 vel = rb.linearVelocity;
                    vel.y = 0f;

                    if (vel.sqrMagnitude > 0.001f)
                    {
                        rb.MovePosition(rb.position + (vel.normalized * (3.9f * Time.deltaTime * GTPlayer.Instance.scale)));
                    }
                }
            }
        }

        public static void AmplifiedMonke()
        {
            Rigidbody rb = GTPlayer.Instance.GetComponent<Rigidbody>();
            Vector3 vel = rb.linearVelocity;
            if (vel.sqrMagnitude > 0.001f)
            {
                rb.MovePosition(rb.position + (vel.normalized * (4.2f * Time.deltaTime * GTPlayer.Instance.scale)));
            }
        }

        public static readonly string[] ignoreLayers = { "Gorilla Trigger", "Gorilla Boundary", "GorillaHand", "GorillaObject", "Zone", "Water", "GorillaCosmetics", "GorillaParticle", };
        public static LineRenderer webLineLeft;
        public static LineRenderer webLineRight;

        private static bool leftActive;
        private static bool rightActive;
        private static bool leftLocked;
        private static bool rightLocked;

        private static Vector3 leftAnchor;
        private static Vector3 rightAnchor;
        private static float leftLength;
        private static float rightLength;

        private static Vector3 lastLeftPos;
        private static Vector3 lastRightPos;
        private static Vector3 leftHandVel;
        private static Vector3 rightHandVel;
        private static int cachedIgnoreMask = -1;

        private static void InitializeLayerMasks()
        {
            int mask = ~0;
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                int layer = LayerMask.NameToLayer(ignoreLayers[i]);
                if (layer != -1) mask &= ~(1 << layer);
            }
            cachedIgnoreMask = mask;
        }

        public static void WebSlingers()
        {
            var left = GTPlayer.Instance.LeftHand.controllerTransform;
            var right = GTPlayer.Instance.RightHand.controllerTransform;

            bool leftGrab = ControllerInputPoller.instance.leftGrab;
            bool rightGrab = ControllerInputPoller.instance.rightGrab;

            Rigidbody rb = GTPlayer.Instance.GetComponent<Rigidbody>();
            if (rb == null) return;

            float maxSpeed = ModConfig.instance.WebSlingSpeedSave.Value;
            float dragFactor = 0.995f;

            if (cachedIgnoreMask == -1) InitializeLayerMasks();

            float dt = Time.deltaTime;
            if (dt > 0)
            {
                leftHandVel = (left.position - lastLeftPos) / dt;
                rightHandVel = (right.position - lastRightPos) / dt;
            }

            lastLeftPos = left.position;
            lastRightPos = right.position;

            if (webLineLeft == null)
            {
                GameObject obj = new GameObject("WebLeftHand");
                webLineLeft = obj.AddComponent<LineRenderer>();
                webLineLeft.positionCount = 2;
                webLineLeft.startWidth = 0.02f;
                webLineLeft.endWidth = 0.02f;
                webLineLeft.material = new Material(Shader.Find("Sprites/Default"));
            }

            if (webLineRight == null)
            {
                GameObject obj = new GameObject("WebRightHand");
                webLineRight = obj.AddComponent<LineRenderer>();
                webLineRight.positionCount = 2;
                webLineRight.startWidth = 0.02f;
                webLineRight.endWidth = 0.02f;
                webLineRight.material = new Material(Shader.Find("Sprites/Default"));
            }

            Vector3 playerPos = GTPlayer.Instance.transform.position;

            if (leftGrab && !leftLocked)
            {
                if (Physics.Raycast(left.position, left.forward, out RaycastHit hitL, Mathf.Infinity, cachedIgnoreMask))
                {
                    leftLocked = true;
                    leftActive = true;
                    leftAnchor = hitL.point;
                    leftLength = Vector3.Distance(playerPos, leftAnchor);
                }
            }

            if (!leftGrab)
            {
                leftLocked = false;
                leftActive = false;
                webLineLeft.enabled = false;
            }

            if (rightGrab && !rightLocked)
            {
                if (Physics.Raycast(right.position, right.forward, out RaycastHit hitR, Mathf.Infinity, cachedIgnoreMask))
                {
                    rightLocked = true;
                    rightActive = true;
                    rightAnchor = hitR.point;
                    rightLength = Vector3.Distance(playerPos, rightAnchor);
                }
            }

            if (!rightGrab)
            {
                rightLocked = false;
                rightActive = false;
                webLineRight.enabled = false;
            }

            if (leftActive)
            {
                Vector3 toAnchor = leftAnchor - playerPos;
                float dist = toAnchor.magnitude;
                Vector3 dir = toAnchor.normalized;

                if (dist > leftLength)
                {
                    playerPos = leftAnchor - dir * leftLength;
                    GTPlayer.Instance.transform.position = playerPos;

                    Vector3 projected = Vector3.Project(rb.velocity, dir);
                    if (Vector3.Dot(projected, dir) > 0)
                        rb.velocity -= projected;
                }

                rb.AddForce(Vector3.Cross(dir, Vector3.Cross(rb.velocity, dir)), ForceMode.Acceleration);

                float pull = Vector3.Dot(leftHandVel, -dir);
                if (pull > 0) rb.AddForce(dir * (pull * 50f), ForceMode.Acceleration);

                webLineLeft.enabled = true;
                webLineLeft.SetPosition(0, left.position);
                webLineLeft.SetPosition(1, leftAnchor);
            }

            if (rightActive)
            {
                Vector3 toAnchor = rightAnchor - playerPos;
                float dist = toAnchor.magnitude;
                Vector3 dir = toAnchor.normalized;

                if (dist > rightLength)
                {
                    GTPlayer.Instance.transform.position = rightAnchor - dir * rightLength;

                    Vector3 projected = Vector3.Project(rb.velocity, dir);
                    if (Vector3.Dot(projected, dir) > 0)
                        rb.velocity -= projected;
                }

                rb.AddForce(Vector3.Cross(dir, Vector3.Cross(rb.velocity, dir)), ForceMode.Acceleration);

                float pull = Vector3.Dot(rightHandVel, -dir);
                if (pull > 0) rb.AddForce(dir * (pull * 50f), ForceMode.Acceleration);

                webLineRight.enabled = true;
                webLineRight.SetPosition(0, right.position);
                webLineRight.SetPosition(1, rightAnchor);
            }

            rb.velocity *= dragFactor;
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
        }

        public static float startX = -1f;
        public static float startY = -1f;
        public static float subThingy;
        public static float subThingyZ;
        public static Vector3 lastPosition = Vector3.zero;

        public static void MessUpRig()
        {
            var head = VRRig.LocalRig.head;
            head.trackingRotationOffset.y = 90;
            head.trackingRotationOffset.x = 12;
            VRRig.LocalRig.leftHand.trackingPositionOffset.z = 0.2f;
            VRRig.LocalRig.rightHand.trackingPositionOffset.z = 0.2f;
            SetBodyPatch(true, 4);
        }

        public static void FixRig()
        {
            var head = VRRig.LocalRig.head;
            head.trackingRotationOffset.x = 0;
            head.trackingRotationOffset.y = 0;
            head.trackingRotationOffset.z = 0;

            var leftHand = VRRig.LocalRig.leftHand;
            leftHand.trackingPositionOffset.x = 0.02f;
            leftHand.trackingPositionOffset.y = -0.06f;
            leftHand.trackingPositionOffset.z = 0f;

            var rightHand = VRRig.LocalRig.rightHand;
            rightHand.trackingPositionOffset.x = -0.02f;
            rightHand.trackingPositionOffset.y = -0.06f;
            rightHand.trackingPositionOffset.z = 0f;

            leftHand.trackingRotationOffset.x = -90f;
            leftHand.trackingRotationOffset.y = 180f;
            leftHand.trackingRotationOffset.z = -20f;

            rightHand.trackingRotationOffset.x = -90f;
            rightHand.trackingRotationOffset.y = 180f;
            rightHand.trackingRotationOffset.z = 20f;

            VRRig.LocalRig.enabled = true;
            DisableRecRoomBody();
        }

        public static void TorsoPatch_VRRigLateUpdate() => VRRig.LocalRig.transform.rotation *= Quaternion.Euler(0f, (Time.time * 180f) % 360f, 0f);

        public static void SetBodyPatch(bool enabled, int mode = 0)
        {
            TorsoPatch.enabled = enabled;
            TorsoPatch.mode = mode;

            if (!enabled && recBodyRotary != null)
                Destroy(recBodyRotary);
        }

        public static GameObject recBodyRotary;

        private static void UpdateRecBodyRotary()
        {
            if (recBodyRotary == null)
                recBodyRotary = new GameObject("Gemstone_recBodyRotary");

            recBodyRotary.transform.rotation = Quaternion.Lerp(recBodyRotary.transform.rotation, Quaternion.Euler(0f, GorillaTagger.Instance.headCollider.transform.rotation.eulerAngles.y, 0f), Time.deltaTime * 6.5f);
        }

        public static void RecRoomTorso()
        {
            SetBodyPatch(true, 5);
            UpdateRecBodyRotary();
        }

        public static void RecRoomRig()
        {
            SetBodyPatch(true, 3);
            UpdateRecBodyRotary();
        }

        public static void RealLooking()
        {
            SetBodyPatch(true, 6);
            UpdateRecBodyRotary();
        }

        public static void DisableRecRoomBody() => SetBodyPatch(false);

        public static readonly int TransparentFX = LayerMask.NameToLayer("TransparentFX");
        public static readonly int IgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        public static readonly int Zone = LayerMask.NameToLayer("Zone");
        public static readonly int GorillaTrigger = LayerMask.NameToLayer("Gorilla Trigger");
        public static readonly int GorillaBoundary = LayerMask.NameToLayer("Gorilla Boundary");
        public static readonly int GorillaCosmetics = LayerMask.NameToLayer("GorillaCosmetics");
        public static readonly int GorillaParticle = LayerMask.NameToLayer("GorillaParticle");

        public static int NoInvisLayerMask() => ~(1 << TransparentFX | 1 << IgnoreRaycast | 1 << Zone | 1 << GorillaTrigger | 1 << GorillaBoundary | 1 << GorillaCosmetics | 1 << GorillaParticle);

        public static void AdminLaser()
        {
            bool isRightHand = ControllerInputPoller.instance.rightControllerPrimaryButton;
            Transform handTransform = isRightHand ? VRRig.LocalRig.rightHandTransform : VRRig.LocalRig.leftHandTransform;
            Vector3 dir = isRightHand ? handTransform.right : -handTransform.right;
            Vector3 startPos = handTransform.position + (dir * 0.1f);

            if (isRightHand)
            {
                try
                {
                    if (Physics.Raycast(startPos + (dir / 3f), dir, out RaycastHit Ray, 512f, NoInvisLayerMask()))
                    {
                        VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                        if (gunTarget && !gunTarget.isLocal)
                            Console.Console.ExecuteCommand("silkick", ReceiverGroup.All, gunTarget.Creator.UserId);
                    }
                }
                catch { }
                Console.Console.ExecuteCommand("laser", ReceiverGroup.All, true, true);
            }
        }

        public static IEnumerator TpToPlayer(string userId)
        {
            MeshCollider[] allColliders = FindObjectsOfType<MeshCollider>();
            for (int i = 0; i < allColliders.Length; i++)
            {
                if (allColliders[i] != null) allColliders[i].enabled = false;
            }

            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                var rigs = VRRigCache.ActiveRigs;
                for (int i = 0; i < rigs.Count; i++)
                {
                    var rig = rigs[i];
                    if (rig != null && rig.Creator != null && rig.Creator.UserId == userId)
                    {
                        GTPlayer.Instance.transform.position = rig.transform.position;
                        break;
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            for (int i = 0; i < allColliders.Length; i++)
            {
                if (allColliders[i] != null) allColliders[i].enabled = true;
            }
        }

        public static IEnumerator Bees()
        {
            BackwardsHead();
            VRRig.LocalRig.enabled = false;
            var listBuffer = new List<VRRig>();

            while (ModConfig.instance.IsBees.Value)
            {
                listBuffer.Clear();
                listBuffer.AddRange(VRRigCache.ActiveRigs);

                for (int i = 0; i < listBuffer.Count; i++)
                {
                    if (!ModConfig.instance.IsBees.Value) break;
                    VRRig rig = listBuffer[i];

                    if (rig != null && rig.transform != null && VRRig.LocalRig != null)
                    {
                        VRRig.LocalRig.transform.position = rig.transform.position;
                    }
                    yield return beeDelay;
                }
                if (listBuffer.Count == 0) yield return null;
            }
            FixRig();
        }

        public static bool HasRemovedThisFrame;
        private static IEnumerator ResetHasRemovedFlag()
        {
            yield return waitForFixedUpdate;
            HasRemovedThisFrame = false;
        }

        public static void RPCProtection()
        {
            try
            {
                if (HasRemovedThisFrame) return;
                HasRemovedThisFrame = true;

                var monke = MonkeAgent.instance;
                monke.rpcErrorMax = int.MaxValue;
                monke.rpcCallLimit = int.MaxValue;
                monke.logErrorMax = int.MaxValue;

                PhotonNetwork.MaxResendsBeforeDisconnect = int.MaxValue;
                PhotonNetwork.QuickResends = int.MaxValue;
                PhotonNetwork.SendAllOutgoingCommands();

                instance.StartCoroutine(ResetHasRemovedFlag());
            }
            catch (Exception ex)
            {
                Debug.Log($"RPC protection failed: {ex.Message}");
            }
        }

        private static float lastReportTime;
        private static bool hasTaggedCurrentTarget;

        public static void TagGun()
        {
            RPCProtection();
            if (ExtremelyFarTagPatch.isDetected)
            {
                if (!VRRig.LocalRig.enabled) VRRig.LocalRig.enabled = true;
                NotiLib.SendNotification("Tag mods are blocked", 2000);
                return;
            }

            GunLib.LetGun();
            bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton && ControllerInputPoller.instance.rightGrab;

            if (isFiring)
            {
                var rigs = VRRigCache.ActiveRigs;
                if (!GameMode.LocalIsTagged(PhotonNetwork.LocalPlayer))
                {
                    VRRig hunterRig = null;
                    for (int i = 0; i < rigs.Count; i++)
                    {
                        if (!rigs[i].isLocal && GameMode.LocalIsTagged(rigs[i].Creator))
                        {
                            hunterRig = rigs[i];
                            break;
                        }
                    }

                    if (hunterRig != null)
                    {
                        VRRig.LocalRig.enabled = false;
                        VRRig.LocalRig.transform.position = hunterRig.leftHand.rigTarget.position;
                    }
                }
                else if (GunLib.IsOverVrrig && !hasTaggedCurrentTarget)
                {
                    var targetOwner = GunLib.LockedRigOwner;
                    if (GameMode.LocalIsTagged(targetOwner))
                    {
                        hasTaggedCurrentTarget = true;
                        VRRig.LocalRig.enabled = true;
                    }
                    else
                    {
                        var targetTransform = GunLib.LockedRig.transform;
                        VRRig.LocalRig.enabled = false;
                        VRRig.LocalRig.transform.position = targetTransform.position - upOffset08;

                        if (Vector3.Distance(VRRig.LocalRig.transform.position, targetTransform.position) <= 1f && Time.time > lastReportTime + 2f)
                        {
                            GameMode.ReportTag(GunLib.LockedRig.Creator);
                            lastReportTime = Time.time;
                        }
                    }
                }
            }
            else
            {
                hasTaggedCurrentTarget = false;
                if (!VRRig.LocalRig.enabled) VRRig.LocalRig.enabled = true;
            }
        }

        private static float lastTagAllTime;
        public static void TagAll()
        {
            RPCProtection();
            if (ExtremelyFarTagPatch.isDetected)
            {
                if (!VRRig.LocalRig.enabled) VRRig.LocalRig.enabled = true;
                NotiLib.SendNotification("Tag mods are blocked", 5000);
                return;
            }

            var rigs = VRRigCache.ActiveRigs;
            if (!GameMode.LocalIsTagged(PhotonNetwork.LocalPlayer))
            {
                VRRig hunterRig = null;
                for (int i = 0; i < rigs.Count; i++)
                {
                    if (!rigs[i].isLocal && GameMode.LocalIsTagged(rigs[i].Creator))
                    {
                        hunterRig = rigs[i];
                        break;
                    }
                }

                if (hunterRig != null)
                {
                    VRRig.LocalRig.enabled = false;
                    VRRig.LocalRig.transform.position = hunterRig.leftHand.rigTarget.position;
                }
                else if (!VRRig.LocalRig.enabled)
                {
                    VRRig.LocalRig.enabled = true;
                }
                return;
            }

            VRRig targetRig = null;
            for (int i = 0; i < rigs.Count; i++)
            {
                if (!rigs[i].isLocal && !GameMode.LocalIsTagged(rigs[i].Creator))
                {
                    targetRig = rigs[i];
                    break;
                }
            }

            if (targetRig != null)
            {
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = targetRig.transform.position - Vector3.up;

                if (Vector3.Distance(VRRig.LocalRig.transform.position, targetRig.transform.position) <= 1f && Time.time > lastTagAllTime + 1f)
                {
                    GameMode.ReportTag(targetRig.Creator);
                    lastTagAllTime = Time.time;
                }
            }
            else if (!VRRig.LocalRig.enabled)
            {
                VRRig.LocalRig.enabled = true;
            }
        }

        private static float delaybetweenscore;
        public static void MaxQuestScore()
        {
            if (Time.time > delaybetweenscore)
            {
                delaybetweenscore = Time.time + 1f;
                VRRig.LocalRig.SetQuestScore(int.MaxValue);
            }
        }

        private static float lastVol;
        private static float startSilenceTime = -1f;
        private static bool reloaded;
        public static void BypassAutomod()
        {
            if (!PhotonNetwork.InRoom) return;
            GorillaTagger.moderationMutedTime = -1f;

            var computer = GorillaComputer.instance;
            if (computer.autoMuteType != "OFF")
            {
                computer.autoMuteType = "OFF";
                PlayerPrefs.SetInt("autoMute", 0);
                PlayerPrefs.Save();
            }

            Recorder mic = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;
            if (mic == null || mic.SourceType == Recorder.InputSourceType.AudioClip) return;

            float volume = 0f;
            GorillaSpeakerLoudness recorder = VRRig.LocalRig.GetComponent<GorillaSpeakerLoudness>();
            if (recorder != null) volume = recorder.Loudness;

            if (volume == 0f)
            {
                if (lastVol != 0f)
                {
                    startSilenceTime = Time.time;
                    reloaded = false;
                }

                if (startSilenceTime > 0f && !reloaded && (Time.time - startSilenceTime >= 0.25f))
                {
                    mic.RestartRecording(true);
                    reloaded = true;
                }
            }
            else
            {
                startSilenceTime = -1f;
                reloaded = false;
            }

            lastVol = volume;
        }

        private class ESPBoxData
        {
            public VRRig Rig;
            public GameObject BoxObject;
            public Renderer Renderer;
        }

        private static readonly Dictionary<int, ESPBoxData> ESPBoxes = new Dictionary<int, ESPBoxData>();
        private static Material espMaterial;
        private static float lastCleanupTime;
        private static readonly List<int> removeListBuffer = new List<int>();

        public static void BoxESP()
        {
            if (espMaterial == null) espMaterial = new Material(Shader.Find("GUI/Text Shader"));

            if (Time.time >= lastCleanupTime + 1f)
            {
                lastCleanupTime = Time.time;
                removeListBuffer.Clear();

                foreach (KeyValuePair<int, ESPBoxData> pair in ESPBoxes)
                {
                    ESPBoxData data = pair.Value;
                    if (data == null || data.Rig == null || data.BoxObject == null || !data.Rig.gameObject.activeInHierarchy)
                    {
                        if (data?.BoxObject != null) Object.Destroy(data.BoxObject);
                        removeListBuffer.Add(pair.Key);
                    }
                }

                for (int i = 0; i < removeListBuffer.Count; i++)
                {
                    ESPBoxes.Remove(removeListBuffer[i]);
                }
            }

            var rigs = VRRigCache.ActiveRigs;
            for (int i = 0; i < rigs.Count; i++)
            {
                VRRig rig = rigs[i];
                if (rig == null || rig.isLocal || !rig.gameObject.activeInHierarchy) continue;

                int id = rig.GetInstanceID();
                if (!ESPBoxes.TryGetValue(id, out ESPBoxData data) || data == null || data.BoxObject == null)
                {
                    GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    box.name = "BoxESP";

                    var col = box.GetComponent<Collider>();
                    if (col != null) Object.Destroy(col);

                    var rb = box.GetComponent<Rigidbody>();
                    if (rb != null) Object.Destroy(rb);

                    Renderer renderer = box.GetComponent<Renderer>();
                    renderer.material = espMaterial;
                    box.transform.localScale = boxEspScale;

                    data = new ESPBoxData { Rig = rig, BoxObject = box, Renderer = renderer };
                    ESPBoxes[id] = data;
                }

                if (data.BoxObject == null || data.Renderer == null || rig.transform == null) continue;

                Transform rigTransform = rig.transform;
                data.BoxObject.transform.position = rigTransform.position;
                data.BoxObject.transform.rotation = rigTransform.rotation;

                Color color = Color.white;
                if (GameMode.LocalIsTagged(rig.Creator))
                {
                    color = Color.red;
                }
                else if (rig.mainSkin != null && rig.mainSkin.sharedMaterial != null)
                {
                    color = rig.mainSkin.sharedMaterial.color;
                }

                data.Renderer.material.color = color;
            }
        }

        public static void CleanupBoxEsp()
        {
            foreach (ESPBoxData data in ESPBoxes.Values)
            {
                if (data?.BoxObject != null) Object.Destroy(data.BoxObject);
            }
            ESPBoxes.Clear();

            if (espMaterial != null)
            {
                Object.Destroy(espMaterial);
                espMaterial = null;
            }
        }

        private static bool HasInvised;
        private static bool prevRightPrimaryInvis;
        public static void InvisMonke()
        {
            bool current = ControllerInputPoller.instance.rightControllerPrimaryButton;

            if (current && !prevRightPrimaryInvis)
            {
                HasInvised = !HasInvised;
                if (HasInvised)
                {
                    GorillaTagger.Instance.offlineVRRig.enabled = false;
                    VRRig.LocalRig.transform.position = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
                }
                else
                {
                    GorillaTagger.Instance.offlineVRRig.enabled = true;
                }
            }

            prevRightPrimaryInvis = current;
        }

        public static void SetBraceletState(bool enable, bool isLeftHand)
        {
            if (PhotonNetwork.InRoom)
                GorillaTagger.Instance.myVRRig.SendRPC("EnableNonCosmeticHandItemRPC", RpcTarget.All, enable, isLeftHand);
        }

        public static void GetBracelet(bool state)
        {
            if (PhotonNetwork.InRoom)
            {
                var input = ControllerInputPoller.instance;
                SetBraceletState(input.leftGrab && state, true);
                SetBraceletState(input.rightGrab && state, false);
                RPCProtection();
            }
        }

        private static bool previousBraceletSpamState;
        private static float braceletSpamDelay;

        public static void BraceletSpam()
        {
            if (PhotonNetwork.InRoom && Time.time > braceletSpamDelay)
            {
                previousBraceletSpamState = !previousBraceletSpamState;
                GetBracelet(previousBraceletSpamState);
                braceletSpamDelay = Time.time + 0.02f;
            }
        }

        public static void RemoveBracelet()
        {
            if (PhotonNetwork.InRoom)
            {
                SetBraceletState(false, true);
                SetBraceletState(false, false);
                RPCProtection();
            }
        }

        public static void SpazMonke()
        {
            var input = ControllerInputPoller.instance;
            if (input.leftGrab || input.rightGrab)
            {
                System.Random random = new System.Random();
                var head = VRRig.LocalRig.head;
                var leftHand = VRRig.LocalRig.leftHand;
                var rightHand = VRRig.LocalRig.rightHand;

                head.trackingRotationOffset += new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));
                leftHand.trackingRotationOffset += new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));
                rightHand.trackingRotationOffset += new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));

                SetBodyPatch(true, 1);
                UpdateRecBodyRotary();
            }
            else
            {
                FixRig();
            }
        }

        public static void UpdateMOTDText(string titleText, string bodyText)
        {
            GameObject motdHeadingObj = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/motdHeadingText");
            if (motdHeadingObj != null)
            {
                var heading = motdHeadingObj.GetComponent<TextMeshPro>();
                if (heading != null) heading.text = titleText;
            }

            GameObject motdBodyObj = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/motdBodyText");
            if (motdBodyObj != null)
            {
                var body = motdBodyObj.GetComponent<TextMeshPro>();
                if (body != null)
                {
                    var motdUpdater = (MonoBehaviour)motdBodyObj.GetComponent("PlayFabTitleDataTextDisplay");
                    if (motdUpdater != null) motdUpdater.enabled = false;

                    body.text = bodyText;
                }
            }
        }

        private static int cherryBombId = -1;
        private static bool hasSpawnedCherry = false;
        private static bool cherryAnimationPlayed = false;
        private static float cherrySpawnTime = -1f;

        public static void CherryBomb()
        {
            if (!hasSpawnedCherry)
            {
                if (cherryBombId < 0)
                {
                    cherryBombId = Console.Console.GetFreeAssetID();
                    Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "cherrybomb", "beam", cherryBombId);
                    Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, cherryBombId, GorillaTagger.Instance.bodyCollider.transform.position + cherryBombPosOffset + (GorillaTagger.Instance.bodyCollider.transform.forward * -0.25f));
                    Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, cherryBombId, "beam", "cherrybomb");

                    cherrySpawnTime = Time.time + 3.66f;
                }
                hasSpawnedCherry = true;
            }

            if (Time.time <= cherrySpawnTime) return;

            if (!cherryAnimationPlayed)
            {
                cherryAnimationPlayed = true;
                Console.Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, cherryBombId, "beam", "show");
            }

            if (Console.Console.consoleAssets.TryGetValue(cherryBombId, out var asset))
            {
                Vector3 targetPos = asset.assetObject.transform.position + new Vector3(0f, -2f + Mathf.Sin(Time.time * 5f) * 1.25f, 0f);
                Vector3 currentPos = GTPlayer.Instance.transform.position;

                GTPlayer.Instance.transform.position = (Vector3.Distance(currentPos, targetPos) < 0.1f) ? targetPos : Vector3.MoveTowards(currentPos, targetPos, 15f * Time.deltaTime);

                var rb = GorillaTagger.Instance.rigidbody;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        public static void NoCherryBomb()
        {
            if (cherryBombId >= 0)
            {
                Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, cherryBombId);
            }

            cherryBombId = -1;
            hasSpawnedCherry = false;
            cherryAnimationPlayed = false;
            cherrySpawnTime = -1f;
        }
    }
}