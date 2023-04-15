using UnityEngine;

namespace ContractConfigurator
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class HarmonyPatcher : MonoBehaviour
    {
        internal void Start()
        {
            var harmony = new HarmonyLib.Harmony("ContractConfigurator.HarmonyPatcher");
            harmony.PatchAll();
        }
    }
}
