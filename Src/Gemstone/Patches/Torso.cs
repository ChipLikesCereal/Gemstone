using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;
using System;
using Gemstone.Gemstone;

namespace Gemstone.patches
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
    public class TorsoPatch
    {
        private static Quaternion frozenRotation;
        private static bool hasFrozenRotation = false;
        public static event Action VRRigLateUpdate;
        public static bool enabled;
        public static int mode = 0;
        private static float storedTorsoYaw;
        private static bool hasStoredYaw = false;

        public static void Postfix(VRRig __instance)
        {
            if (__instance.isLocal)
            {
                if (enabled)
                {
                    Quaternion rotation = Quaternion.identity;
                    switch (mode)
                    {
                        case 0:
                            rotation = Quaternion.Euler(0f, Time.time * 180f % 360, 0f);
                            break;
                        case 1:
                            rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                            break;
                        case 2:
                            rotation = Quaternion.Euler(0f, GorillaTagger.Instance.headCollider.transform.rotation.eulerAngles.y + 180f, 0f);
                            break;
                        case 3:
                            rotation = Quaternion.Euler(0f, Mods.Mods.recBodyRotary.transform.rotation.eulerAngles.y, 0f);
                            break;
                        case 4:
                            if (!hasFrozenRotation)
                            {
                                frozenRotation = __instance.transform.rotation;
                                hasFrozenRotation = true;
                            }

                            rotation = frozenRotation;
                            break;
                        case 5:
                            {
                                Transform a = GTPlayer.Instance.LeftHand.controllerTransform;
                                Transform b = GTPlayer.Instance.RightHand.controllerTransform;

                                Vector3 pos = __instance.transform.position;

                                Vector3 dirA = a.position - pos;
                                Vector3 dirB = b.position - pos;

                                dirA.y = 0f;
                                dirB.y = 0f;

                                dirA.Normalize();
                                dirB.Normalize();

                                Vector3 currentForward = __instance.transform.forward;
                                currentForward.y = 0f;
                                currentForward.Normalize();


                                float dot = Vector3.Dot(dirA, dirB);

                                Vector3 blendedDir;

                                if (dot < -0.5f)
                                {
                                    float dotA = Vector3.Dot(currentForward, dirA);
                                    float dotB = Vector3.Dot(currentForward, dirB);

                                    blendedDir = dotA > dotB ? dirA : dirB;
                                }
                                else
                                {
                                    blendedDir = dirA + dirB;
                                }

                                if (blendedDir.sqrMagnitude > 0.0001f)
                                {
                                    blendedDir.Normalize();

                                    float angle = Vector3.SignedAngle(currentForward, blendedDir, Vector3.up);

                                    float maxAngle = 100f;
                                    float clampedAngle = Mathf.Clamp(angle, -maxAngle, maxAngle);

                                    Quaternion clampedRot = Quaternion.AngleAxis(clampedAngle, Vector3.up);
                                    Vector3 finalForward = clampedRot * currentForward;

                                    rotation = Quaternion.LookRotation(finalForward, Vector3.up);
                                }

                                break;
                            }
                        case 6:
                            {
                                float deadzone = 40f;
                                float baseSpeed = 60f;
                                float maxSpeed = 960f;

                                float headYaw = GorillaTagger.Instance.headCollider.transform.rotation.eulerAngles.y;

                                Transform lefthand = GTPlayer.Instance.LeftHand.controllerTransform;
                                Transform righthand = GTPlayer.Instance.RightHand.controllerTransform;
                                Vector3 pos = __instance.transform.position;

                                Vector3 dirA = lefthand.position - pos;
                                Vector3 dirB = righthand.position - pos;

                                dirA.y = 0f;
                                dirB.y = 0f;

                                float handYaw = headYaw;

                                if (dirA.sqrMagnitude > 0.001f && dirB.sqrMagnitude > 0.001f)
                                {
                                    dirA.Normalize();
                                    dirB.Normalize();

                                    Vector3 blended = dirA + dirB;

                                    if (blended.sqrMagnitude > 0.001f)
                                    {
                                        blended.Normalize();
                                        handYaw = Quaternion.LookRotation(blended, Vector3.up).eulerAngles.y;
                                    }
                                }

                                float handWeight = 0.15f;

                                float targetYaw = Mathf.LerpAngle(headYaw, handYaw, handWeight);

                                if (!hasStoredYaw)
                                {
                                    storedTorsoYaw = targetYaw;
                                    hasStoredYaw = true;
                                }

                                float delta = Mathf.DeltaAngle(storedTorsoYaw, targetYaw);
                                float absDelta = Mathf.Abs(delta);

                                if (absDelta > deadzone)
                                {
                                    float excess = absDelta - deadzone;

                                    float speed = Mathf.Min(maxSpeed, baseSpeed * (excess / 30f));

                                    storedTorsoYaw += Mathf.Sign(delta) * speed * Time.deltaTime;
                                }

                                rotation = Quaternion.Euler(0f, storedTorsoYaw, 0f);
                                break;
                            }
                    }
                    if (mode != 4)
                    {
                        hasFrozenRotation = false;
                    }
                    if (mode != 6)
                    {
                        hasStoredYaw = false;
                    }

                    __instance.transform.rotation = rotation;
                    __instance.head.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
                    __instance.leftHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
                    __instance.rightHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
                }

                VRRigLateUpdate?.Invoke();
            }
        }
    }
}