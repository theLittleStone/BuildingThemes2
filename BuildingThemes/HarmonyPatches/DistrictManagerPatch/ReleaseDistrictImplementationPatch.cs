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
            Debugger.Log("Building Themes: DistrictManager.ReleaseDistrictImplementation patched.");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(DistrictManager), "ReleaseDistrictImplementation"));

            deployed = false;
            Debugger.Log("Building Themes: DistrictManager.ReleaseDistrictImplementation unpatched.");
        }

        // Prefix: clear theme management before the district data is wiped by the original.
        public static void Prefix(byte district, ref District data)
        {
            if (data.m_flags == District.Flags.None) return;
            BuildingThemesManager.instance.ToggleThemeManagement(district, false);
        }
    }
}
