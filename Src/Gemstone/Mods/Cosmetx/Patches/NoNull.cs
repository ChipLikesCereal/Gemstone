using GorillaNetworking;
using HarmonyLib;
using UnityEngine;

namespace Gemstone.Mods.Cosmetx.Patches
{
    [HarmonyPatch(typeof(CosmeticsController.CosmeticSet), "LoadFromPlayerPreferences")]
    internal class NoNull : MonoBehaviour
    {
        private static bool Prefix(CosmeticsController.CosmeticSet __instance, CosmeticsController controller)
        {
            int num = 16;
            for (int i = 0; i < num; i++)
            {
                CosmeticsController.CosmeticSlots slot = (CosmeticsController.CosmeticSlots)i;
                CosmeticsController.CosmeticItem item = controller.GetItemFromDict(PlayerPrefs.GetString(CosmeticsController.CosmeticSet.SlotPlayerPreferenceName(slot), "NOTHING"));
                __instance.items[i] = item;
            }
            return false;
        }
    }
}
