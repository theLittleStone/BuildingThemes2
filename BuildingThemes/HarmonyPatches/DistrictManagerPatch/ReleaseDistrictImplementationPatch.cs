using CitiesHarmony.API;

namespace BuildingThemes.HarmonyPatches.DistrictManagerPatch
{
    public static class ReleaseDistrictImplementationPatch
    {
        private static bool deployed;

        public static void Deploy()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || deployed) return;

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(DistrictManager), "ReleaseDistrictImplementation"),
                prefix: new PatchUtil.MethodDefinition(typeof(ReleaseDistrictImplementationPatch), nameof(Prefix)));

            deployed = true;
            Debugger.Log("DistrictManager.ReleaseDistrictImplementation patched.");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(DistrictManager), "ReleaseDistrictImplementation"));

            deployed = false;
            Debugger.Log("DistrictManager.ReleaseDistrictImplementation unpatched.");
        }

        // Prefix: permanently clear theme data when a district is deleted.
        // Unlike ToggleThemeManagement(false), ClearDistrictData nulls the entry so that
        // a new district created in the same slot starts completely fresh.
        public static void Prefix(byte district, ref District data)
        {
            if (data.m_flags == District.Flags.None) return;
            BuildingThemesManager.instance.ClearDistrictData(district);
        }
    }
}
