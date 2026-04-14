using CitiesHarmony.API;
using ColossalFramework.Math;

namespace BuildingThemes.HarmonyPatches.BuildingManagerPatch
{
    public static class GetRandomBuildingInfoPatch
    {
        private static bool deployed;

        public static void Deploy()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || deployed) return;

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(BuildingManager), "GetRandomBuildingInfo"),
                prefix: new PatchUtil.MethodDefinition(typeof(GetRandomBuildingInfoPatch), nameof(Prefix)));

            deployed = true;
            Debugger.Log("Building Themes: BuildingManager.GetRandomBuildingInfo patched.");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(BuildingManager), "GetRandomBuildingInfo"));

            deployed = false;
            Debugger.Log("Building Themes: BuildingManager.GetRandomBuildingInfo unpatched.");
        }

        // Prefix: select a themed building. If none found, fall through to the original.
        public static bool Prefix(ref BuildingInfo __result, ref Randomizer r,
            ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level,
            int width, int length, BuildingInfo.ZoningMode zoningMode, int style)
        {
            __result = RandomBuildings.GetRandomBuildingInfo_Spawn(
                BuildingThemesMod.position, ref r, service, subService, level,
                width, length, zoningMode, style);

            // Return false (skip original) only when we found a themed building.
            // Return true (run original) when result is null so vanilla fallback applies.
            return __result == null;
        }
    }
}
