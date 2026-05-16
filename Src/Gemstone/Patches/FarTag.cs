using Gemstone.Gemstone;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
namespace Gemstone.patches
{
    [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.SendReport))]
    public static class ExtremelyFarTagPatch
    {
        public static bool isDetected = false;
        private const float PlayerReportLogCooldown = 1f;
        private static readonly Dictionary<string, float> LastLoggedReport = new Dictionary<string, float>();

        private static bool Prefix(string susReason, string susId, string susNick)
        {
            bool isMe = (susId == PhotonNetwork.LocalPlayer.UserId);

            if (isMe)
            {
                if (susReason == "extremely far tag")
                {
                    isDetected = true;
                    NotiLib.SendNotification(Localization.Get("<color=red>[ANTICHEAT]</color> You have been reported for extremely far tag, is it recommended you stop using the tag mods and restart your game."), 5000);
                }

                return false;
            }

            if (LastLoggedReport.ContainsKey(susId) && LastLoggedReport[susId] > Time.time)
            {
                return true;
            }

            LastLoggedReport[susId] = Time.time + PlayerReportLogCooldown;

            return true;
        }
    }
}