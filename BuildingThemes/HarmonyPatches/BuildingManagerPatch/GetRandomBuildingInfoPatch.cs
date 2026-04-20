using CitiesHarmony.API;
using ColossalFramework;
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
            Debugger.Log("BuildingManager.GetRandomBuildingInfo patched.");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(BuildingManager), "GetRandomBuildingInfo"));

            deployed = false;
            Debugger.Log("BuildingManager.GetRandomBuildingInfo unpatched.");
        }

        // Prefix: select a themed building. If none found, fall through to the original.
        public static bool Prefix(ref BuildingInfo __result, ref Randomizer r,
            ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level,
            int width, int length, BuildingInfo.ZoningMode zoningMode, int style)
        {
            // Fast path: skip all theme logic for unthemed districts.
            var mgr = BuildingThemesManager.instance;
            if (mgr != null)
            {
                byte districtId = Singleton<DistrictManager>.instance.GetDistrict(BuildingThemesMod.position);
                if (!mgr.IsEffectivelyThemed(districtId))
                    return true;
            }

            __result = RandomBuildings.GetRandomBuildingInfo_Spawn(
                BuildingThemesMod.position, ref r, service, subService, level,
                width, length, zoningMode, style);

            // Consume the intentional-null flag set by strict mode.
            bool intentional = RandomBuildings.s_intentionalNull;
            RandomBuildings.s_intentionalNull = false;

            // Return false (skip original) when we found a themed building OR when strict mode
            // deliberately produced no result (intentional == true → do not run vanilla).
            // Return true (run original) only for a genuine null with no theme restriction.
            return __result == null && !intentional;
        }
    }
}
