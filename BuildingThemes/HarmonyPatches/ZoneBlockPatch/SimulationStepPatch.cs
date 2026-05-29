using System;
using System.Runtime.CompilerServices;
using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using UnityEngine;

namespace BuildingThemes.HarmonyPatches.ZoneBlockPatch
{
    public static partial class SimulationStepPatch
    {
        // Zone grid dimensions — updated in SetUp() if 81 Tiles mod is active.
        private static int _zoneGridResolution = ZoneManager.ZONEGRID_RESOLUTION;
        private static int _zoneGridHalfResolution = ZoneManager.ZONEGRID_RESOLUTION / 2;
        private static readonly int EIGHTY_ONE_ZONEGRID_RESOLUTION = 270;
        private static readonly int EIGHTY_ONE_HALF_ZONEGRID_RESOLUTION = 270 / 2;

        private static bool deployed;
        private static int debugCount;

        // Increase grid bounds when 81 Tiles mod is detected.
        public static void SetUp()
        {
            if (!Util.IsModActive(BuildingThemesMod.EIGHTY_ONE_MOD) &&
                !Util.IsModAssemblyActive(BuildingThemesMod.EIGHTY_ONE_2_ASSEMBLY))
            {
                return;
            }
            _zoneGridResolution = EIGHTY_ONE_ZONEGRID_RESOLUTION;
            _zoneGridHalfResolution = EIGHTY_ONE_HALF_ZONEGRID_RESOLUTION;
        }

        public static void Deploy()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || deployed) return;

            // Apply reverse patches so our stubs delegate back to the original private helpers.
            Harmony.ReversePatch(
                AccessTools.Method(typeof(ZoneBlock), "CheckBlock"),
                new HarmonyMethod(typeof(SimulationStepPatch), nameof(CheckBlock)));
            Harmony.ReversePatch(
                AccessTools.Method(typeof(ZoneBlock), "IsGoodPlace"),
                new HarmonyMethod(typeof(SimulationStepPatch), nameof(IsGoodPlace)));

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(ZoneBlock), "SimulationStep"),
                prefix: new PatchUtil.MethodDefinition(typeof(SimulationStepPatch), nameof(Prefix)));

            deployed = true;
            Debugger.Log("ZoneBlock.SimulationStep patched.");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(ZoneBlock), "SimulationStep"));

            deployed = false;
            debugCount = 0;
            Debugger.Log("ZoneBlock.SimulationStep unpatched.");
        }

        // Stubs replaced by Harmony ReversePatch with the original private implementations.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckBlock(ref ZoneBlock __instance, ushort blockID, ushort otherID,
            ref ZoneBlock other, int[] xBuffer, ItemClass.Zone zone, Vector2 startPos,
            Vector2 xDir, Vector2 zDir, Quad2 quad)
        {
            throw new NotImplementedException("CheckBlock stub was not replaced by Harmony reverse patch");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsGoodPlace(ref ZoneBlock __instance, Vector2 position)
        {
            throw new NotImplementedException("IsGoodPlace stub was not replaced by Harmony reverse patch");
        }
    }
}
