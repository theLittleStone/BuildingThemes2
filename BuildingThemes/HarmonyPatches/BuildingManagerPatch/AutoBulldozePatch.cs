using CitiesHarmony.API;
using HarmonyLib;
using System;

namespace BuildingThemes.HarmonyPatches.BuildingManagerPatch
{
    /// <summary>
    /// Hooks into BuildingManager.SimulationStep to run the auto-bulldoze scan once per tick.
    /// SimulationStep is declared on SimulationManagerBase&lt;&gt;, not BuildingManager directly,
    /// so we use AccessTools.Method (which walks the inheritance chain) instead of PatchUtil.
    /// </summary>
    public static class AutoBulldozePatch
    {
        private static bool deployed;

        public static void Deploy()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || deployed) return;

            var original = AccessTools.Method(typeof(BuildingManager), "SimulationStep",
                new[] { typeof(int) });

            if (original == null)
            {
                Debugger.LogError("AutoBulldozePatch: could not find BuildingManager.SimulationStep(int).");
                return;
            }

            var harmony = new Harmony(BuildingThemesMod.HarmonyId);
            harmony.Patch(original,
                postfix: new HarmonyMethod(typeof(AutoBulldozePatch), nameof(Postfix)));

            deployed = true;
            Debugger.Log("BuildingManager.SimulationStep patched (AutoBulldoze).");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            var original = AccessTools.Method(typeof(BuildingManager), "SimulationStep",
                new[] { typeof(int) });

            if (original != null)
            {
                var harmony = new Harmony(BuildingThemesMod.HarmonyId);
                harmony.Unpatch(original, HarmonyPatchType.Postfix, BuildingThemesMod.HarmonyId);
            }

            deployed = false;
            Debugger.Log("BuildingManager.SimulationStep unpatched (AutoBulldoze).");
        }

        public static void Postfix(int subStep)
        {
            // subStep cycles 0–3; run only on 0 to keep frequency reasonable
            if (subStep != 0) return;
            try
            {
                AutoBulldozeService.Tick();
            }
            catch (Exception e)
            {
                Debugger.LogException(e);
            }
        }
    }
}
