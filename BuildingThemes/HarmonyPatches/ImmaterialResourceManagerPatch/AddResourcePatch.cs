using CitiesHarmony.API;
using UnityEngine;

namespace BuildingThemes.HarmonyPatches.ImmaterialResourceManagerPatch
{
    // Captures the world position of abandoned buildings so GetRandomBuildingInfo_Spawn
    // can use it when the game replaces them with fresh Level 1 buildings.
    // Skipped entirely when 81 Tiles is active (original detour behaviour preserved).
    public static class AddResourcePatch
    {
        private static bool deployed;
        private static int debugCounter;

        public static void Deploy()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || deployed) return;

            if (Util.IsModActive(BuildingThemesMod.EIGHTY_ONE_MOD))
            {
                Debugger.Log("Building Themes: ImmaterialResourceManager.AddResource will NOT be patched — 81 Tiles detected.");
                return;
            }

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(ImmaterialResourceManager), "AddResource",
                    argumentTypes: new[] { typeof(ImmaterialResourceManager.Resource), typeof(int), typeof(Vector3), typeof(float) }),
                prefix: new PatchUtil.MethodDefinition(typeof(AddResourcePatch), nameof(Prefix)));

            deployed = true;
            Debugger.Log("Building Themes: ImmaterialResourceManager.AddResource patched.");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(ImmaterialResourceManager), "AddResource",
                    argumentTypes: new[] { typeof(ImmaterialResourceManager.Resource), typeof(int), typeof(Vector3), typeof(float) }));

            deployed = false;
            Debugger.Log("Building Themes: ImmaterialResourceManager.AddResource unpatched.");
        }

        // Prefix: capture the abandoned-building position, then let the original run.
        public static void Prefix(ImmaterialResourceManager.Resource resource, int rate, Vector3 position)
        {
            if (Debugger.Enabled && debugCounter < 10)
            {
                debugCounter++;
                Debugger.Log("Building Themes: ImmaterialResourceManager.AddResource prefix called.");
            }

            if (resource == ImmaterialResourceManager.Resource.Abandonment)
            {
                BuildingThemesMod.position = position;
            }
        }
    }
}
