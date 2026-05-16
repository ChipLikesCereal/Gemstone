using BepInEx;
using BepInEx.Configuration;
using Gemstone.patches;
using GorillaLocomotion;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Photon.Voice.Unity;
using CosmeticRoom;
using Gemstone.Mods.Cosmetx;

namespace Gemstone.Gemstone
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance { get; private set; }
        public static bool hasGivenNotif = false;
        public static int Pages = 0;
        private bool isMenuCreated;
        private GameObject menuObj;
        private GameObject menuPrefab;
        private AssetBundle menuBundle;


        private bool menuOpen = false;
        private bool buttonWasPressed = false;

        private Vector3 menuForwardOffset = new Vector3(0.08f, 0f, 0f);
        private void LoadMenuAssetBundle()
        {
            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = "Gemstone.EmbedResources.menuobject";

            if (menuBundle == null)
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Logger.LogError($"rsrc not found");
                        return;
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        menuBundle = AssetBundle.LoadFromMemory(ms.ToArray());
                    }
                }
            }

            if (menuBundle == null)
            {
                Logger.LogError("asset bundle failed to load");
                return;
            }

            menuPrefab = menuBundle.LoadAsset<GameObject>("Assets/MenuObject.prefab");

            if (menuPrefab == null)
                Logger.LogError("prefab not found");
        }
        private void LoadSound()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                string resourceName = "Gemstone.EmbedResources.buttonpress.ogg";

                using Stream stream = assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    Logger.LogError("sound rsrc not found");
                    return;
                }

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                var tempClip = WavOrOggToAudioClip(data);

                if (tempClip != null)
                {
                    cachedClip = tempClip;
                    soundReady = true;
                }
            }
            catch (Exception e)
            {
                Logger.LogError("failed loading embedded sound " + e);
            }
        }
        private AudioClip WavOrOggToAudioClip(byte[] data)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "Gemstone_click.ogg");
            File.WriteAllBytes(tempPath, data);

            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.OGGVORBIS);
            var op = www.SendWebRequest();

            while (!op.isDone) { }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Logger.LogError(www.error);
                return null;
            }

            return DownloadHandlerAudioClip.GetContent(www);
        }

        public GameObject HandMenuCollider;
        public List<GameObject> btnObjs = new List<GameObject>();
        public float globalClickCooldown = 0f;
        public int currentCategoryIndex = -1;
        public int currentPageIndex = 0;

        public AudioSource audioSource;
        private AudioClip cachedClip;
        private bool soundReady;
        private Coroutine rgbCoroutine;
        public bool IsAdmin = false;

        private List<AudioClip> soundboardClips = new List<AudioClip>();
        private List<string> soundboardFileNames = new List<string>();

        private static Coroutine activeSoundCoroutine;
        private static AudioClip currentlyPlayingClip;

        void Awake()
        {
            LoadMenuAssetBundle();
            instance = this;
            string dirPath = Path.Combine(Paths.GameRootPath, "Gemstone");
            Directory.CreateDirectory(dirPath);

            Harmony harmony = new Harmony(PluginInfo.GUID);
            harmony.PatchAll();

            audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            LoadSound();
            StartCoroutine(LoadSoundboardFiles());
        }

        void Start()
        {
            gameObject.AddComponent<GunLib>();
            gameObject.AddComponent<ModConfig>();
            gameObject.AddComponent<RunMods>();
            gameObject.AddComponent<Cosmetx>();
            gameObject.AddComponent<JoinNotifs>();
            if (NotiLib.Instance == null)
            {
                var notiObj = new GameObject("NotiLib");
                DontDestroyOnLoad(notiObj);
                notiObj.AddComponent<NotiLib>();
            }
            Console.Console.LoadConsole();
        }

        public void EnableAdminMenu()
        {
            IsAdmin = true;
            if (isMenuCreated) RefreshMenu();
        }
        public static Coroutine beesCoroutine;
        public static void ToggleSoundboard(AudioClip sound)
        {
            if (currentlyPlayingClip == sound)
            {
                StopActiveSound();
                return;
            }

            if (activeSoundCoroutine != null)
            {
                StopActiveSound();
            }

            activeSoundCoroutine = instance.StartCoroutine(PlaySoundMicrophone(sound));
        }

        private static void StopActiveSound()
        {
            if (activeSoundCoroutine != null)
            {
                instance.StopCoroutine(activeSoundCoroutine);
                activeSoundCoroutine = null;
            }

            currentlyPlayingClip = null;

            var recorder = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;
            recorder.SourceType = Recorder.InputSourceType.Microphone;
            recorder.AudioClip = null;
            recorder.RestartRecording(true);
            recorder.DebugEchoMode = false;
        }
        public static IEnumerator PlaySoundMicrophone(AudioClip sound)
        {
            if (sound == null) yield break;

            currentlyPlayingClip = sound;

            NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.SourceType = Recorder.InputSourceType.AudioClip;
            NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.AudioClip = sound;
            NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.RestartRecording(true);
            NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.DebugEchoMode = true;

            yield return new WaitForSeconds(sound.length + 0.1f);

            StopActiveSound();

            if (instance.menuOpen && instance.isMenuCreated)
            {
                instance.RefreshMenu();
            }
        }

        private IEnumerator LoadSoundboardFiles()
        {
            soundboardClips.Clear();
            soundboardFileNames.Clear();

            string soundPath = Path.Combine(Paths.GameRootPath, "Gemstone", "Sounds");
            if (!Directory.Exists(soundPath))
            {
                Directory.CreateDirectory(soundPath);
                yield break;
            }

            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(soundPath, "*.ogg"));
            files.AddRange(Directory.GetFiles(soundPath, "*.mp3"));

            foreach (string file in files)
            {
                AudioType type = file.EndsWith(".mp3") ? AudioType.MPEG : AudioType.OGGVORBIS;

                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + file, type))
                {
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                        clip.name = Path.GetFileNameWithoutExtension(file);
                        soundboardClips.Add(clip);
                        soundboardFileNames.Add(clip.name);
                    }
                    else
                    {
                        Logger.LogError($"Failed to load sound {file}: {www.error}");
                    }
                }
            }
        }
        void Update()
        {
            Mods.Mods.UpdateMOTDText($"Welcome To Gemstone Version: {PluginInfo.Version}!", "Welcome to gemstone! This menu has a few fun mods made just for you!\n\n\n If you get banned it is not I, The developers responsibility.");
            if (globalClickCooldown > 0) globalClickCooldown -= Time.deltaTime;
            bool isButtonPressed = ControllerInputPoller.instance.leftControllerSecondaryButton;

            if (isButtonPressed && !buttonWasPressed)
            {
                menuOpen = !menuOpen;

                if (menuOpen)
                {
                    CreateMenu();
                }
                else
                {
                    DestroyMenu(false);
                }
            }




            buttonWasPressed = isButtonPressed;
            if (menuObj != null)
            {
                Vector3 parentScale = GTPlayer.Instance.LeftHand.controllerTransform.lossyScale;

                Vector3 desired = new Vector3(1.24208f, 13.04792f, 15.86129f);

                menuObj.transform.localScale = new Vector3(
                    desired.x / parentScale.x,
                    desired.y / parentScale.y,
                    desired.z / parentScale.z
                );
            }
        }
        private Player selectedPlayer;
        private bool inPlayerSubmenu;

        public void CreateMenu()
        {
            var player = GTPlayer.Instance;
            isMenuCreated = true;

            if (menuPrefab != null)
            {
                menuObj = Instantiate(menuPrefab);
            }
            else
            {
                menuObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                menuObj.transform.localScale = new Vector3(0.03f, 0.21f, 0.45f);
            }

            menuObj.transform.parent = player.LeftHand.controllerTransform;
            menuObj.transform.localPosition = menuForwardOffset;
            menuObj.transform.localRotation = Quaternion.identity;

            Transform backBtn = menuObj.transform.Find("Back");
            Transform forwardBtn = menuObj.transform.Find("Forwards");

            GameObject backObj = backBtn != null ? backBtn.gameObject : null;
            GameObject forwardObj = forwardBtn != null ? forwardBtn.gameObject : null;

            Transform Disconnect = menuObj.transform.Find("Disconnect");

            GameObject DisconnectObj = Disconnect != null ? Disconnect.gameObject : null;
            Transform Home = menuObj.transform.Find("Home");
            GameObject HomeObj = Home != null ? Home.gameObject : null;

            void SetupNavButton(GameObject obj)
            {
                if (obj == null) return;

                obj.layer = 2;

                var rb = obj.GetComponent<Rigidbody>() ?? obj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                var col = obj.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                var rend = obj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.shader = Shader.Find("GorillaTag/UberShader");
                    rend.material.color = ModConfig.Theme;

                    if (ModConfig.instance.IsMenuRGB.Value)
                    {
                        StartCoroutine(RGBTheme(rend));
                    }
                }

                var bc = obj.GetComponent<ButtonCollider>() ?? obj.AddComponent<ButtonCollider>();

                bc.OnPressed = null;
            }

            SetupNavButton(backObj);
            SetupNavButton(forwardObj);
            SetupNavButton(DisconnectObj);
            SetupNavButton(HomeObj);

            if (backObj != null)
            {
                backObj.GetComponent<ButtonCollider>().OnPressed = () =>
                {
                    currentPageIndex = Mathf.Max(0, currentPageIndex - 1);
                    RefreshMenu();
                };
            }

            if (forwardObj != null)
            {
                forwardObj.GetComponent<ButtonCollider>().OnPressed = () =>
                {
                    currentPageIndex = Mathf.Min(Pages - 1, currentPageIndex + 1);
                    RefreshMenu();
                };
            }
            if (DisconnectObj != null)
            {
                DisconnectObj.GetComponent<ButtonCollider>().OnPressed = () =>
                {
                    PhotonNetwork.Disconnect();
                };
                var text = DisconnectObj.transform.Find("TextUnchangable").GetComponent<TextMeshPro>();
                text.text = Localization.Get("Disconnect");


            }
            if (HomeObj != null)
            {
                HomeObj.GetComponent<ButtonCollider>().OnPressed = () =>
                {
                    SwitchPage(-1, 0);
                };
            }

            Transform titleTransform = menuObj.transform.Find("Title");

            if (titleTransform != null)
            {
                var text = titleTransform.GetComponent<TextMeshPro>();
                if (text != null)
                {
                    text.text = currentCategoryIndex == -1 ? Localization.Get("Gemstone") + " [" + (currentPageIndex + 1) + "]" : GetCategoryName(currentCategoryIndex) + " [" + (currentPageIndex + 1) + "]";
                    text.color = ModConfig.Theme == Color.white ? Color.black : Color.white;

                    if (text.fontSharedMaterial != null)
                    {
                        text.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
                    }
                }
            }
            else
            {
                Debug.LogWarning("Prefab not found");
            }
            Transform BackText = menuObj.transform.Find(":3");
            if (BackText != null)
            {
                var text = BackText.GetComponent<TextMeshPro>();
                if (text.fontSharedMaterial != null)
                {
                    text.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
                }
            }

            if (menuObj.GetComponent<Rigidbody>()) Destroy(menuObj.GetComponent<Rigidbody>());
            if (menuObj.GetComponent<Collider>()) Destroy(menuObj.GetComponent<Collider>());

            var rend = menuObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.shader = Shader.Find("GorillaTag/UberShader");
                rend.material.color = ModConfig.Theme;
                if (ModConfig.instance.IsMenuRGB.Value) rgbCoroutine = StartCoroutine(RGBTheme(rend));
            }

            HandMenuCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            HandMenuCollider.transform.parent = player.RightHand.controllerTransform;
            HandMenuCollider.transform.localPosition = Vector3.down * 0.094f;
            HandMenuCollider.layer = 2;
            HandMenuCollider.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
            Destroy(HandMenuCollider.GetComponent<Rigidbody>());

            if (ModConfig.instance.ShowHandCollider.Value)
            {
                var rendhand = HandMenuCollider.GetComponent<Renderer>();
                rendhand.material.shader = Shader.Find("GorillaTag/UberShader");
                rendhand.material.color = Color.white;
            }

            float zOffset = 0.06f;
            float step = 0.05f;

            if (currentCategoryIndex == -1)
            {
                if (IsAdmin) Pages = 3;
                else Pages = 2;
                if (currentPageIndex == 0)
                {
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Movement"), () => SwitchPage(0, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Utility"), () => SwitchPage(1, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Rig Mods"), () => SwitchPage(2, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Settings"), () => SwitchPage(3, 0)); zOffset -= step;
                }
                else if (currentPageIndex == 1)
                {
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Important"), () => SwitchPage(4, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Fun"), () => SwitchPage(5, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Player List"), () => SwitchPage(6, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Soundboard"), () => SwitchPage(7, 0)); zOffset -= step;
                }
                else
                {
                    if (!IsAdmin) currentPageIndex = 1;
                    if (IsAdmin)
                    {
                        AddButton(zOffset, 0f, 0.2f, Localization.Get("Admin"), () => SwitchPage(8, 0)); zOffset -= step;
                    }
                }
            }
            else
            {

                switch (currentCategoryIndex)
                {
                    case 0:
                        Pages = 5;
                        if (currentPageIndex == 0)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Speed Boost"), ModConfig.instance.SpeedBoostEnabled);
                            AddToggleButton(ref zOffset, step, Localization.Get("Fly"), ModConfig.instance.FlyEnabled);
                            AddToggleButton(ref zOffset, step, Localization.Get("Platforms"), ModConfig.instance.IsPlatformsEnabled);
                            AddToggleButton(ref zOffset, step, Localization.Get("Joystick Fly"), ModConfig.instance.IsJoystickFly);
                        }
                        else if (currentPageIndex == 1)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Long Arms"), ModConfig.instance.LongArmsEnabled, () => Mods.Mods.UnLongArms());
                            AddToggleButton(ref zOffset, step, Localization.Get("Ground Helper"), ModConfig.instance.IsGroundHelper);
                            AddToggleButton(ref zOffset, step, Localization.Get("Amplified Monke"), ModConfig.instance.IsAmplifiedMonke);
                            AddToggleButton(ref zOffset, step, Localization.Get("Noclip"), ModConfig.instance.IsNoclipEnabled);
                        }
                        else if (currentPageIndex == 2)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Web Slingers"), ModConfig.instance.IsWebSlingers);
                            AddToggleButton(ref zOffset, step, Localization.Get("Teleport Gun"), ModConfig.instance.IsTPGun);
                            AddToggleButton(ref zOffset, step, Localization.Get("Tag Gun (D?)"), ModConfig.instance.IsTagGun, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Tag All (D?)"), ModConfig.instance.IsTagAll, () => Mods.Mods.FixRig());
                        }
                        else
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Box ESP"), ModConfig.instance.IsBoxEsp, () => Mods.Mods.CleanupBoxEsp());
                        }
                            break;

                    case 1:
                        Pages = 2;
                        if (currentPageIndex == 0)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Get PID Gun"), ModConfig.instance.IsGetPIDGun);
                            AddToggleButton(ref zOffset, step, Localization.Get("Mute Gun"), ModConfig.instance.IsMuteGun);
                            AddToggleButton(ref zOffset, step, Localization.Get("Mute Others"), ModConfig.instance.IsMuteEveryoneExceptGun);
                            AddToggleButton(ref zOffset, step, Localization.Get("Report Gun"), ModConfig.instance.IsReportGun);
                        }
                        else
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Mute All"), () => Mods.Mods.MuteAll()); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Unmute All"), () => Mods.Mods.UnmuteAll()); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Ignore Far Tag"), () => ExtremelyFarTagPatch.isDetected = false); zOffset -= step;
                        }
                        break;

                    case 2:
                        Pages = 4;
                        if (currentPageIndex == 0)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Ghost Monke"), ModConfig.instance.IsGhostMonke);
                            AddToggleButton(ref zOffset, step, Localization.Get("Lock Rig"), ModConfig.instance.IsLockOntoRig);
                            AddToggleButton(ref zOffset, step, Localization.Get("Hold Rig"), ModConfig.instance.IsHoldRig);
                            AddToggleButton(ref zOffset, step, Localization.Get("Rig Gun"), ModConfig.instance.IsRigGun);
                        }
                        else if (currentPageIndex == 1)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Freeze Rig"), ModConfig.instance.IsFreezeRig);
                            AddToggleButton(ref zOffset, step, Localization.Get("Upside Down Head"), ModConfig.instance.IsUpsideDownHead, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Backwards Head"), ModConfig.instance.IsBackwardsHead, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("funny rig"), ModConfig.instance.IsFunnyRig, () => Mods.Mods.FixRig());
                        }
                        else if (currentPageIndex == 2)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Recroom Torso"), ModConfig.instance.IsRecroomTorso, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Recroom Rig"), ModConfig.instance.IsRecroomRig, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Realistic Looking"), ModConfig.instance.IsRealisticLooking, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Bees"), ModConfig.instance.IsBees, () => { StopCoroutine(beesCoroutine); beesCoroutine = null; Mods.Mods.FixRig(); });
                        }
                        else
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Copy Rig"), ModConfig.instance.IsCopyRigGun, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Invis Monke"), ModConfig.instance.IsInvisMonke, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Spaz Monke"), ModConfig.instance.IsSpazMonke, () => Mods.Mods.FixRig());
                        }
                            break;

                    case 3:
                        Pages = 8;
                        if (currentPageIndex == 0)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Invis Plats"), ModConfig.instance.IsInvisPlat);
                            AddToggleButton(ref zOffset, step, Localization.Get("Menu RGB"), ModConfig.instance.IsMenuRGB);
                            AddButton(zOffset, 0f, 0.2f, "R +", () =>
                            {
                                ModConfig.instance.R.Value = (ModConfig.instance.R.Value + 1) % 11;
                                NotiLib.SendNotification(ModConfig.instance.R.Value.ToString(), 2000);
                                RefreshMenu();
                            });
                            zOffset -= step;

                            AddButton(zOffset, 0f, 0.2f, "G +", () =>
                            {
                                ModConfig.instance.G.Value = (ModConfig.instance.G.Value + 1) % 11;
                                NotiLib.SendNotification(ModConfig.instance.G.Value.ToString(), 2000);
                                RefreshMenu();
                            });
                            zOffset -= step;
                        }
                        else if (currentPageIndex == 1)
                        {
                            AddButton(zOffset, 0f, 0.2f, "B +", () =>
                            {
                                ModConfig.instance.B.Value = (ModConfig.instance.B.Value + 1) % 11;
                                NotiLib.SendNotification(ModConfig.instance.B.Value.ToString(), 2000);
                                RefreshMenu();
                            });
                            zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Show Hand Collider"), ModConfig.instance.ShowHandCollider);
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Fly Speed +"), () => { ModConfig.instance.FlySpeedSave.Value += 0.1f; NotiLib.SendNotification(ModConfig.instance.FlySpeedSave.Value.ToString("0.0"), 2000); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Fly Speed -"), () => { ModConfig.instance.FlySpeedSave.Value -= 0.1f; NotiLib.SendNotification(ModConfig.instance.FlySpeedSave.Value.ToString("0.0"), 2000); }); zOffset -= step;
                        }
                        else if (currentPageIndex == 2)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Sling Speed -"), () => { ModConfig.instance.WebSlingSpeedSave.Value -= 5f; NotiLib.SendNotification(ModConfig.instance.WebSlingSpeedSave.Value.ToString("0.0"), 2000); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Sling Speed +"), () => { ModConfig.instance.WebSlingSpeedSave.Value += 5f; NotiLib.SendNotification(ModConfig.instance.WebSlingSpeedSave.Value.ToString("0.0"), 2000); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Language english"), () => { ModConfig.instance.Language.Value = 1; RefreshMenu(); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Language spanish"), () => { ModConfig.instance.Language.Value = 2; RefreshMenu(); }); zOffset -= step;
                        }
                        else if (currentPageIndex == 3)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Language german"), () => { ModConfig.instance.Language.Value = 3; RefreshMenu(); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Language russian"), () => { ModConfig.instance.Language.Value = 4; RefreshMenu(); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Language polish"), () => { ModConfig.instance.Language.Value = 5; RefreshMenu(); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Wiggly Gun"), () => ModConfig.instance.GunType.Value = 1); zOffset -= step;
                        }
                        else if (currentPageIndex == 4)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Straight Gun"), () => ModConfig.instance.GunType.Value = 2); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Coil Gun"), () => ModConfig.instance.GunType.Value = 3); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Lighting Gun"), () => ModConfig.instance.GunType.Value = 4); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vortex Gun"), () => ModConfig.instance.GunType.Value = 5); zOffset -= step;
                        }
                        else if (currentPageIndex == 5)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("DNA Gun"), () => ModConfig.instance.GunType.Value = 6); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Pulse Gun"), () => ModConfig.instance.GunType.Value = 7); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Orbital Gun"), () => ModConfig.instance.GunType.Value = 8); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Static Gun"), () => ModConfig.instance.GunType.Value = 9); zOffset -= step;
                        }
                        else if (currentPageIndex == 6)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Sine Wave Gun"), () => ModConfig.instance.GunType.Value = 10); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Digital Gun"), () => ModConfig.instance.GunType.Value = 11); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Square Pulse Gun"), () => ModConfig.instance.GunType.Value = 12); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Ray Gun"), () => ModConfig.instance.GunType.Value = 13); zOffset -= step;
                        }
                        else
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Gun Smoothness -"), () =>
                            {
                                ModConfig.instance.GunSmoothness.Value = Mathf.Max(0f, ModConfig.instance.GunSmoothness.Value - 0.05f);
                                NotiLib.SendNotification(ModConfig.instance.GunSmoothness.Value.ToString("0.00"), 2000);
                            });
                            zOffset -= step;

                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Gun Smoothness +"), () =>
                            {
                                ModConfig.instance.GunSmoothness.Value += 0.05f;
                                NotiLib.SendNotification(ModConfig.instance.GunSmoothness.Value.ToString("0.00"), 2000);
                            });
                            zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Preview Gun"), ModConfig.instance.PreviewGun);
                        }
                            break;
                    case 4:
                        Pages = 1;
                        AddButton(zOffset, 0f, 0.2f, Localization.Get("Reauthenticate"), () => MothershipAuthenticator.Instance.BeginLoginFlow()); zOffset -= step;
                        AddToggleButton(ref zOffset, step, Localization.Get("Anti Report"), ModConfig.instance.IsAntiReportEnabled);
                        AddToggleButton(ref zOffset, step, Localization.Get("Bypass Automod"), ModConfig.instance.IsBypassAutoMod);
                        break;
                    case 5:
                        Pages = 1;
                        AddButton(zOffset, 0f, 0.2f, Localization.Get("Unlock all cosmetics (CS)"), () => Mods.Cosmetx.Cosmetx.instance.ActivateCosmetx()); zOffset -= step;
                        AddButton(zOffset, 0f, 0.2f, Localization.Get("Max Quest Score"), () => Mods.Mods.MaxQuestScore()); zOffset -= step;
                        AddToggleButton(ref zOffset, step, Localization.Get("Bracelet Spam"), ModConfig.instance.IsBraceletSpam, () => Mods.Mods.RemoveBracelet());
                        break;
                    case 6:
                        {
                            int playersPerPage = 4;

                            Player[] players = PhotonNetwork.PlayerList;

                            int totalPlayers = players.Length;

                            Pages = Mathf.CeilToInt(totalPlayers / (float)playersPerPage);
                            Pages = Mathf.Max(1, Pages);

                            int startIndex = currentPageIndex * playersPerPage;
                            int endIndex = Mathf.Min(startIndex + playersPerPage, totalPlayers);

                            if (!inPlayerSubmenu)
                            {
                                for (int i = startIndex; i < endIndex; i++)
                                {
                                    Player player_ = players[i];

                                    AddButton(zOffset, 0f, 0.2f, player_.NickName, () =>
                                    {
                                        selectedPlayer = player_;
                                        inPlayerSubmenu = true;
                                        RefreshMenu();
                                    });

                                    zOffset -= step;
                                }
                            }
                            else
                            {
                                AddButton(zOffset, 0f, 0.2f, Localization.Get("Back"), () =>
                                {
                                    inPlayerSubmenu = false;
                                    selectedPlayer = null;
                                    RefreshMenu();
                                });

                                zOffset -= step;

                                AddButton(zOffset, 0f, 0.2f, Localization.Get("Teleport to"), () =>
                                {
                                    if (selectedPlayer == null) return;

                                    StartCoroutine(Mods.Mods.TpToPlayer(selectedPlayer.UserId));
                                });

                                zOffset -= step;

                                AddButton(zOffset, 0f, 0.2f, Localization.Get("Custom Properties"), () =>
                                {
                                    if (selectedPlayer == null) return;

                                    string output = $"Player: {selectedPlayer.NickName}\n\nCustom Properties:\n";

                                    if (selectedPlayer.CustomProperties != null && selectedPlayer.CustomProperties.Count > 0)
                                    {
                                        foreach (var kvp in selectedPlayer.CustomProperties)
                                        {
                                            output += $"{kvp.Key}: {kvp.Value}\n";
                                        }
                                    }
                                    else
                                    {
                                        output += "None";
                                    }

                                    NotiLib.SendNotification(output, 4000f);
                                });
                                zOffset -= step;
                                AddButton(zOffset, 0f, 0.2f, Localization.Get("Block Player"), () =>
                                {
                                    if (selectedPlayer == null) return;

                                    string userId = selectedPlayer.UserId;

                                    foreach (var rig in VRRigCache.ActiveRigs)
                                    {
                                        if (rig == null || rig.Creator == null) continue;

                                        if (rig.Creator.UserId == userId && rig.Creator.UserId != PhotonNetwork.LocalPlayer.UserId)
                                        {
                                            rig.gameObject.SetActive(false);
                                        }
                                    }

                                    NotiLib.SendNotification(
                                        $"Blocked {selectedPlayer.NickName}",
                                        2000f
                                    );
                                });
                            }

                            break;
                        }
                    case 7: // Soundboard Submenu
                        {
                            int soundsPerPage = 4;
                            int totalSounds = soundboardClips.Count;
                            Pages = Mathf.CeilToInt(totalSounds / (float)soundsPerPage);
                            Pages = Mathf.Max(1, Pages);

                            int startIndex = currentPageIndex * soundsPerPage;
                            int endIndex = Mathf.Min(startIndex + soundsPerPage, totalSounds);

                            if (totalSounds == 0) AddButton(zOffset, 0f, 0.2f, "No Sounds Found", () => { });
                            else
                            {
                                for (int i = startIndex; i < endIndex; i++)
                                {
                                    AudioClip selectedClip = soundboardClips[i];
                                    string btnText = (currentlyPlayingClip == selectedClip) ? $"[PLAYING] {selectedClip.name}" : selectedClip.name;

                                    AddButton(zOffset, 0f, 0.2f, btnText, () =>
                                    {
                                        ToggleSoundboard(selectedClip);
                                        RefreshMenu();
                                    });
                                    zOffset -= step;
                                }
                            }
                            break;
                        }
                    case 8:
                        Pages = 5;
                        if (currentPageIndex == 0)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Silent Kick Gun"), ModConfig.instance.IsSilKick);
                            AddToggleButton(ref zOffset, step, Localization.Get("Admin Laser"), ModConfig.instance.AdminLaser);
                            AddToggleButton(ref zOffset, step, Localization.Get("Travis Scott"), ModConfig.instance.IsTravis, () => Mods.Mods.NoTravis());
                            AddToggleButton(ref zOffset, step, Localization.Get("Tv"), ModConfig.instance.IsTv, () => Mods.Mods.NoTv());
                        }
                        else if (currentPageIndex == 1)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Phone"), ModConfig.instance.IsPhone, () => Mods.Mods.NoSamsung());
                            AddToggleButton(ref zOffset, step, Localization.Get("Twerking Carti"), ModConfig.instance.IsTwerkingCarti, () => Mods.Mods.NoCarti());
                            AddToggleButton(ref zOffset, step, Localization.Get("Grab All"), ModConfig.instance.IsAdminGrab);
                            AddToggleButton(ref zOffset, step, Localization.Get("Roblox Sword"), ModConfig.instance.IsCoolSword, () => Mods.Mods.NoSword());
                        }
                        else if (currentPageIndex == 2)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Kormakur Sign"), ModConfig.instance.IsKormakur, () => Mods.Mods.NoSign());
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Hell"), () => Mods.Mods.Video = "https://github.com/ChipLikesCereal/testvid/raw/refs/heads/main/GirlHell1999.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid OCD"), () => Mods.Mods.Video = "https://github.com/ChipLikesCereal/testvid/raw/refs/heads/main/OCD.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Kitty"), () => Mods.Mods.Video = "https://github.com/ChipLikesCereal/testvid/raw/refs/heads/main/Kitty.mp4"); zOffset -= step;
                        }
                        else if (currentPageIndex == 3)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid AMV"), () => Mods.Mods.Video = "https://github.com/ChipLikesCereal/testvid/raw/refs/heads/main/testvid.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid theresabarrier"), () => Mods.Mods.Video = "https://github.com/ChipLikesCereal/testvid/raw/refs/heads/main/theresabarrier.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Edit"), () => Mods.Mods.Video = "https://github.com/ChipLikesCereal/testvid/raw/refs/heads/main/edit.mp4"); zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Cherry bomb"), ModConfig.instance.IsCherryBomb, () => Mods.Mods.NoCherryBomb());
                        }
                        else
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Zlothy"), () => Mods.Mods.Video = "https://github.com/ChipLikesCereal/testvid/raw/refs/heads/main/Zlothy.mov"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Barrier Remix"), () => Mods.Mods.Video = "https://github.com/ChipLikesCereal/testvid/raw/refs/heads/main/there%20is%20a%20barrier%20remix.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid invincible wobbly edit"), () => Mods.Mods.Video = "https://github.com/ChipLikesCereal/testvid/raw/refs/heads/main/INVINCIBLEWOBBLYANIMATION.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Punch Mod"), () => Mods.Mods.Video = "https://github.com/ChipLikesCereal/testvid/raw/refs/heads/main/punchmod.mp4"); zOffset -= step;
                        }
                            break;
                }

                float navY = 0.08f;
            }

            foreach (GameObject btnObj in btnObjs)
            {
                if (btnObj != null)
                {
                    btnObj.layer = 2;
                    btnObj.GetComponent<Collider>().isTrigger = true;
                }
            }
        }

        private string GetCategoryName(int index) => index switch { 0 => Localization.Get("Movement"), 1 => Localization.Get("Utility"), 2 => Localization.Get("Rig Mods"), 3 => Localization.Get("Settings"), 4 => Localization.Get("Important"), 5 => Localization.Get("Fun"), 6 => Localization.Get("Player List"), 7 => Localization.Get("Soundboard"), 8 => Localization.Get("Admin"), _ => Localization.Get("Gemstone") };
        private void SwitchPage(int cat, int page) { currentCategoryIndex = cat; currentPageIndex = page; RefreshMenu(); }
        public void RefreshMenu()
        {
            if (!menuOpen) return;

            DestroyMenu(true);
            CreateMenu();
            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, Mathf.Max(0, Pages - 1));
        }

        public void DestroyMenu(bool refresh)
        {
            // If we aren't just refreshing, tell the system the menu is gone
            if (!refresh) isMenuCreated = false;

            if (menuObj != null)
            {
                if (ModConfig.instance.IsMenuRGB.Value && rgbCoroutine != null)
                    StopCoroutine(rgbCoroutine);

                if (!refresh)
                {
                    // CRITICAL: Unparent before destroying with a delay
                    menuObj.transform.SetParent(null, true);

                    Rigidbody rb = menuObj.GetComponent<Rigidbody>() ?? menuObj.AddComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.AddForce(Vector3.down * 0.5f, ForceMode.Impulse);

                    // Clear the reference so Update() doesn't try to scale it while it's falling
                    GameObject menuToDestroy = menuObj;
                    menuObj = null;
                    Destroy(menuToDestroy, 5f);
                }
                else
                {
                    // Refreshing: Destroy immediately
                    Destroy(menuObj);
                    menuObj = null;
                }
            }

            if (HandMenuCollider != null)
            {
                Destroy(HandMenuCollider);
                HandMenuCollider = null;
            }

            // Cleanup buttons
            foreach (var b in btnObjs)
            {
                if (b != null)
                {
                    if (!refresh)
                    {
                        // Stop following the hand
                        var follow = b.GetComponent<FollowMenu>();
                        if (follow != null) follow.target = null;

                        b.transform.SetParent(null, true);
                        Rigidbody rb = b.GetComponent<Rigidbody>() ?? b.AddComponent<Rigidbody>();
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        rb.AddForce(Vector3.down * 0.5f, ForceMode.Impulse);
                        Destroy(b, 5f);
                    }
                    else
                    {
                        Destroy(b);
                    }
                }
            }
            btnObjs.Clear();
            Config.Save();
        }

        void AddToggleButton(ref float z, float step, string name, ConfigEntry<bool> entry, Action onDisable = null, Action onEnable = null)
        {
            AddButton(z, 0f, 0.2f, entry.Value ? $"[ON] {name}" : name, () => {
                entry.Value = !entry.Value;
                if (entry.Value) onEnable?.Invoke();
                else onDisable?.Invoke();
                RefreshMenu();
            });
            z -= step;
        }

        void AddButton(float z, float y, float s, string name, Action act)
        {
            GameObject btn = null;
            if (menuBundle != null)
            {
                GameObject buttonPrefab = menuBundle.LoadAsset<GameObject>("Assets/Button.prefab");

                if (buttonPrefab != null)
                {
                    btn = Instantiate(buttonPrefab);
                }
                else
                {
                    Logger.LogError("prefab not found");
                }
            }

            if (btn == null)
            {
                btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                btn.transform.localScale = new Vector3(0.03f, s, 0.04f);
            }

            var f = btn.GetComponent<FollowMenu>() ?? btn.AddComponent<FollowMenu>();
            f.target = GTPlayer.Instance.LeftHand.controllerTransform;
            f.position = new Vector3(0.015f, y, z) + menuForwardOffset;
            f.rotationOffset = Quaternion.Euler(90f, 0f, 0f);

            var renderer = btn.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.shader = Shader.Find("GorillaTag/UberShader");
                if (!ModConfig.instance.IsMenuRGB.Value)
                    btn.GetComponent<Renderer>().material.color = ModConfig.Theme;
                else
                {
                    StartCoroutine(RGBTheme(btn.GetComponent<Renderer>()));
                }
            }

            var collider = btn.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            btn.layer = 2;

            var c = btn.GetComponent<ButtonCollider>() ?? btn.AddComponent<ButtonCollider>();
            c.OnPressed = act;

            TextMeshPro t = null;
            Transform textTransform = btn.transform.Find("Text");

            if (textTransform != null)
            {
                t = textTransform.GetComponent<TextMeshPro>();
            }

            if (t == null)
            {
                var tObj = new GameObject("Text");
                tObj.transform.SetParent(btn.transform);
                tObj.transform.localPosition = new Vector3(0.55f, 0f, 0f);
                tObj.transform.localRotation = Quaternion.Euler(0f, -90f, -90f);

                t = tObj.AddComponent<TextMeshPro>();
            }
            t.text = name;
            if (ModConfig.Theme == Color.black)
                t.color = Color.white;
            else if (ModConfig.Theme == Color.white)
            {
                t.color = Color.black;
            }
            else
            {
                t.color = Color.white;
            }


                btnObjs.Add(btn);
        }

        public void PlayClickSound() { if (soundReady && cachedClip != null) audioSource.PlayOneShot(cachedClip); }
        public IEnumerator RGBTheme(Renderer r) { while (true) { float t = Time.time * 2f; r.material.color = new Color(Mathf.Sin(t) * 0.5f + 0.5f, Mathf.Sin(t + 2f) * 0.5f + 0.5f, Mathf.Sin(t + 4f) * 0.5f + 0.5f); yield return null; } }

        public class FollowMenu : MonoBehaviour
        {
            public Transform target;
            public Vector3 position;
            public Quaternion rotationOffset = Quaternion.identity;

            void LateUpdate()
            {
                if (target)
                {
                    transform.position = target.TransformPoint(position);
                    transform.rotation = target.rotation * rotationOffset;
                }
            }
        }
        public class ButtonCollider : MonoBehaviour
        {
            public Action OnPressed;

            public void Press()
            {
                if (instance.globalClickCooldown > 0)
                    return;

                instance.globalClickCooldown = 0.4f;

                instance.PlayClickSound();

                OnPressed?.Invoke();
            }

            private void OnTriggerEnter(Collider other)
            {
                if (other.gameObject != instance.HandMenuCollider)
                    return;

                GorillaTagger.Instance.StartVibration(
                    false,
                    GorillaTagger.Instance.tagHapticStrength / 2f,
                    0.05f
                );

                Press();
            }
        }
    }
}