using CitiesHarmony.API;
using ColossalFramework.UI;

namespace BuildingThemes.HarmonyPatches.DistrictWorldInfoPanelPatch
{
    // After the policies panel opens, select the first tab so the Themes tab is visible.
    public static class OnPoliciesClickPatch
    {
        private static bool deployed;

        public static void Deploy()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || deployed) return;

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(DistrictWorldInfoPanel), "OnPoliciesClick"),
                postfix: new PatchUtil.MethodDefinition(typeof(OnPoliciesClickPatch), nameof(Postfix)));

            deployed = true;
            Debugger.Log("Building Themes: DistrictWorldInfoPanel.OnPoliciesClick patched.");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(DistrictWorldInfoPanel), "OnPoliciesClick"));

            deployed = false;
            Debugger.Log("Building Themes: DistrictWorldInfoPanel.OnPoliciesClick unpatched.");
        }

        // Postfix: select tab index 0 so the Themes policy tab is shown first.
        public static void Postfix()
        {
            var panel = UIView.Find<UIPanel>("PoliciesPanel");
            if (panel == null) return;
            var tabstrip = panel.Find<UITabstrip>("Tabstrip");
            if (tabstrip != null) tabstrip.selectedIndex = 0;
        }
    }
}
