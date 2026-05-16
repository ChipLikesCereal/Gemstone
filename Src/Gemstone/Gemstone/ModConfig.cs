using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using BepInEx;

namespace Gemstone.Gemstone
{
    public class ModConfig : MonoBehaviour
    {
        public static ModConfig instance;
        public ConfigEntry<bool> SpeedBoostEnabled, FlyEnabled, LongArmsEnabled, IsPlatformsEnabled, IsNoclipEnabled,
                         IsJoystickFly, IsGroundHelper, IsAmplifiedMonke, IsWebSlingers,
                         IsLockOntoRig, IsHoldRig, IsRigGun, IsFreezeRig, IsTPGun, IsGetPIDGun,
                         IsMuteGun, IsMuteEveryoneExceptGun, IsReportGun, IsSilKick, IsTwerkingCarti,
                         IsCoolSword, IsTravis, IsPhone, IsAdminGrab, IsKormakur,
                         IsAxe, IsBigAssets, IsTv, IsUpsideDownHead, IsBackwardsHead,
                         IsAntiReportEnabled, IsGhostMonke, IsMenuRGB, IsInvisPlat, IsFunnyRig, IsRecroomTorso, IsRecroomRig, IsRealisticLooking, ShowHandCollider, AdminLaser, IsBees, IsTagGun, IsTagAll, IsCopyRigGun,
                         IsBypassAutoMod, IsBoxEsp, PreviewGun, IsInvisMonke, IsBraceletSpam, IsSpazMonke, IsCherryBomb;

        public ConfigEntry<float> FlySpeedSave, WebSlingSpeedSave, R, G, B, GunSmoothness;
        public ConfigEntry<int> Language, GunType, MenuType;
        public static Color Theme;
        void Update()
        {
            Theme = new Color(R.Value / 10f, G.Value / 10f, B.Value / 10f);
        }
        void Awake()
        {
            instance = this;
            Language = Plugin.instance.Config.Bind("Settings", "Language", 1, "");
            Language = Plugin.instance.Config.Bind("Settings", "Menu Type", 0, "");
            R = Plugin.instance.Config.Bind("Settings", "Theme R", 5f, "");
            G = Plugin.instance.Config.Bind("Settings", "Theme G", 8f, "");
            B = Plugin.instance.Config.Bind("Settings", "Theme B", 10f, "");
            GunType = Plugin.instance.Config.Bind("Settings", "Gun Type", 1, "");
            FlySpeedSave = Plugin.instance.Config.Bind("Settings", "FlySpeedSave", 4f, "");
            WebSlingSpeedSave = Plugin.instance.Config.Bind("Settings", "WebSlingSpeedSave", 30f, "");
            GunSmoothness = Plugin.instance.Config.Bind("Settings", "Gun Smoothness", 0.2f, "");

            IsMenuRGB = Plugin.instance.Config.Bind("Settings", "Rgb Mode", false, "");
            IsInvisPlat = Plugin.instance.Config.Bind("Settings", "Invis Plats", false, "");
            ShowHandCollider = Plugin.instance.Config.Bind("Settings", "Show Hand Collider", true, "");

            PreviewGun = Plugin.instance.Config.Bind("Settings", "Preview Gun", false, "");
            SpeedBoostEnabled = Plugin.instance.Config.Bind("Movement", "Speed Boost", false, "");
            FlyEnabled = Plugin.instance.Config.Bind("Movement", "Fly", false, "");
            LongArmsEnabled = Plugin.instance.Config.Bind("Movement", "Long Arms", false, "");
            IsPlatformsEnabled = Plugin.instance.Config.Bind("Movement", "Platforms", false, "");
            IsNoclipEnabled = Plugin.instance.Config.Bind("Movement", "Noclip", false, "");
            IsJoystickFly =     Plugin.instance.Config.Bind("Movement", "Joystick Fly", false, "");
            IsGroundHelper = Plugin.instance.Config.Bind("Movement", "Ground Helper", false, "");
            IsAmplifiedMonke = Plugin.instance.Config.Bind("Movement", "Amplified Monke", false, "");
            IsWebSlingers = Plugin.instance.Config.Bind("Movement", "Web Slingers", false, "");
            IsTPGun = Plugin.instance.Config.Bind("Movement", "Teleport Gun", false, "");
            IsTagGun = Plugin.instance.Config.Bind("Movement", "Tag Gun", false, "");
            IsTagAll = Plugin.instance.Config.Bind("Movement", "Tag All", false, "");
            IsBoxEsp = Plugin.instance.Config.Bind("Movement", "Box Esp", false, "");

            IsGhostMonke = Plugin.instance.Config.Bind("Rig", "Ghost Monke", false, "");
            IsLockOntoRig = Plugin.instance.Config.Bind("Rig", "Lock Rig", false, "");
            IsHoldRig = Plugin.instance.Config.Bind("Rig", "Hold Rig", false, "");
            IsRigGun = Plugin.instance.Config.Bind("Rig", "Rig Gun", false, "");
            IsFreezeRig =   Plugin.instance.Config.Bind("Rig", "Freeze Rig", false, "");
            IsUpsideDownHead = Plugin.instance.Config.Bind("Rig", "Upside Down Head", false, "");
            IsBackwardsHead = Plugin.instance.Config.Bind("Rig", "Backwards Head", false, "");
            IsFunnyRig = Plugin.instance.Config.Bind("Rig", "Funny Rig", false, "");
            IsRecroomTorso = Plugin.instance.Config.Bind("Rig", "Recroom Torso", false, "");
            IsRecroomRig = Plugin.instance.Config.Bind("Rig", "Recroom Rig", false, "");
            IsRealisticLooking = Plugin.instance.Config.Bind("Rig", "Realistic Looking", false, "");
            IsBees = Plugin.instance.Config.Bind("Rig", "Bees", false, "");
            IsCopyRigGun = Plugin.instance.Config.Bind("Rig", "Copy Rig Gun", false, "");
            IsInvisMonke = Plugin.instance.Config.Bind("Rig", "Invis Monke", false, "");
            IsSpazMonke = Plugin.instance.Config.Bind("Rig", "Spaz Monke", false, "");


            IsBraceletSpam = Plugin.instance.Config.Bind("Fun", "Invi", false, "");

            IsGetPIDGun = Plugin.instance.Config.Bind("Utility", "Get PID Gun", false, "");
            IsMuteGun = Plugin.instance.Config.Bind("Utility", "Mute Gun", false, "");
            IsMuteEveryoneExceptGun = Plugin.instance.Config.Bind("Utility", "Mute Others", false, "");
            IsReportGun = Plugin.instance.Config.Bind("Utility", "Report Gun", false, "");
            IsAntiReportEnabled = Plugin.instance.Config.Bind("Important", "Anti Report", false, "");
            IsBypassAutoMod = Plugin.instance.Config.Bind("Important", "Bypass Automod", false, "");

            IsSilKick = Plugin.instance.Config.Bind("Admin", "SilKick", false, "");
            IsTwerkingCarti = Plugin.instance.Config.Bind("Admin", "Twerking Carti", false, "");
            IsCoolSword = Plugin.instance.Config.Bind("Admin", "Cool Sword", false, "");
            IsTravis = Plugin.instance.Config.Bind("Admin", "Travis Scott", false, "");
            IsPhone = Plugin.instance.Config.Bind("Admin", "Phone", false, "");
            IsAdminGrab = Plugin.instance.Config.Bind("Admin", "Grab All", false, "");
            IsKormakur = Plugin.instance.Config.Bind("Admin", "Kormakur", false, "");
            IsAxe = Plugin.instance.Config.Bind("Admin", "Axe", false, "");
            IsBigAssets = Plugin.instance.Config.Bind("Admin", "Big Assets", false, "");
            IsTv = Plugin.instance.Config.Bind("Admin", "TV", false, "");
            AdminLaser = Plugin.instance.Config.Bind("Admin", "Laser", false, "");
            IsCherryBomb = Plugin.instance.Config.Bind("Admin", "CherryBomb idfk im mad rn dont question ts", false, "");
        }
    }
}
