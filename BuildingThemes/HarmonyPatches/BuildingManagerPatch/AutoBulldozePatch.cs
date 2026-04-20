using CitiesHarmony.API;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace BuildingThemes.HarmonyPatches.BuildingManagerPatch
{
    /// <summary>
    /// Hooks into BuildingManager's ISimulationManager.SimulationStep to run the
    /// auto-bulldoze scan once per tick during normal gameplay.
    ///
    /// BuildingManager has TWO simulation-related methods:
    ///   - SimulationStepImpl(int)          — called via some internal / editor paths
    ///   - ISimulationManager.SimulationStep(int) — called by SimulationManager each tick
    ///
    /// We use GetInterfaceMap to reliably obtain the target of the interface dispatch,
    /// which is what SimulationManager actually calls during normal gameplay.
    /// </summary>
    public static class AutoBulldozePatch
    {
        private static bool deployed;
        private static MethodInfo s_patchedMethod;

        public static void Deploy()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || deployed) return;

            var original = FindSimulationStepMethod();
            if (original == null)
            {
                Debugger.LogError("AutoBulldozePatch: could not find BuildingManager simulation step method.");
                return;
            }

            var harmony = new Harmony(BuildingThemesMod.HarmonyId);
            harmony.Patch(original,
                postfix: new HarmonyMethod(typeof(AutoBulldozePatch), nameof(Postfix)));

            s_patchedMethod = original;
            deployed = true;
            Debugger.LogFormat("AutoBulldozePatch: patched '{0}' on BuildingManager.", original.Name);
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            if (s_patchedMethod != null)
            {
                var harmony = new Harmony(BuildingThemesMod.HarmonyId);
                harmony.Unpatch(s_patchedMethod, HarmonyPatchType.Postfix, BuildingThemesMod.HarmonyId);
            }

            s_patchedMethod = null;
            deployed = false;
            Debugger.Log("AutoBulldozePatch: unpatched.");
        }

        /// <summary>
        /// Finds the method that SimulationManager calls on BuildingManager each tick.
        /// Prefers the explicit ISimulationManager.SimulationStep implementation (what
        /// SimulationManager actually dispatches to); falls back to SimulationStepImpl.
        /// </summary>
        private static MethodInfo FindSimulationStepMethod()
        {
            // Primary: get the concrete method BuildingManager uses to implement
            // ISimulationManager.SimulationStep — this is what SimulationManager calls.
            try
            {
                Type ifaceType = typeof(BuildingManager).GetInterfaces()
                    .FirstOrDefault(t => t.Name == "ISimulationManager");

                if (ifaceType != null)
                {
                    var map = typeof(BuildingManager).GetInterfaceMap(ifaceType);
                    for (int i = 0; i < map.InterfaceMethods.Length; i++)
                    {
                        var im = map.InterfaceMethods[i];
                        if (im.Name == "SimulationStep")
                        {
                            var target = map.TargetMethods[i];
                            Debugger.LogFormat(
                                "AutoBulldozePatch: found ISimulationManager.SimulationStep → '{0}' (DeclaringType={1})",
                                target.Name, target.DeclaringType?.Name);
                            return target;
                        }
                    }

                    Debugger.LogError("AutoBulldozePatch: ISimulationManager found but SimulationStep not in interface map.");
                }
                else
                {
                    Debugger.LogError("AutoBulldozePatch: BuildingManager does not implement ISimulationManager.");
                }
            }
            catch (Exception e)
            {
                Debugger.LogFormat("AutoBulldozePatch: interface map lookup failed: {0}", e.Message);
            }

            // Fallback: SimulationStepImpl (may only fire on non-standard code paths)
            var fallback = AccessTools.Method(typeof(BuildingManager), "SimulationStepImpl", new[] { typeof(int) });
            if (fallback != null)
                Debugger.Log("AutoBulldozePatch: falling back to SimulationStepImpl.");
            return fallback;
        }

        public static void Postfix(int __0)
        {
            if (__0 != 0) return; // only subStep 0 — one tick per frame instead of 4×
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
