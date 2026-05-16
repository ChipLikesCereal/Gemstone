using BepInEx;
using GorillaNetworking;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Gemstone.Mods.Cosmetx
{
    public class Cosmetx : MonoBehaviour
    {
        public static Cosmetx instance;
        public bool hasActivated = false;
        void Awake()
        {
            instance = this;
        }
        public void ActivateCosmetx()
        {
            if (!hasActivated)
            HarmonyPatches.ApplyHarmonyPatches();

            UnlockCosmetics();
            hasActivated = true;
        }
        public void UnlockCosmetics()
        {
            MethodInfo UnlockItem = typeof(CosmeticsController).GetMethod("UnlockItem", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (CosmeticsController.CosmeticItem cosmeticItem in CosmeticsController.instance.allCosmetics)
            {
                if (!CosmeticsController.instance.concatStringCosmeticsAllowed.Contains(cosmeticItem.itemName))
                {
                    try
                    {
                        UnlockItem.Invoke(CosmeticsController.instance, new object[] { cosmeticItem.itemName, false });
                    }
                    catch { }
                }
            }

            CosmeticsController.instance.OnCosmeticsUpdated.Invoke();
        }
    }
}