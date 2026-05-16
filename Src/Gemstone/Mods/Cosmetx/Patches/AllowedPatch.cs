using GorillaNetworking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gemstone.Mods.Cosmetx.Patches
{
    [HarmonyPatch(typeof(VRRig))]
    [HarmonyPatch("IsItemAllowed", MethodType.Normal)]
    internal class SlidePatch
    {
        private static void Postfix(VRRig __instance, ref bool __result) =>
            __result = true;
    }
}
