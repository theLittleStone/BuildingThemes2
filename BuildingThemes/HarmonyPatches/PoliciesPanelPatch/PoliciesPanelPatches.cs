using System.Reflection;
using CitiesHarmony.API;
using ColossalFramework.UI;
using BuildingThemes.GUI;

namespace BuildingThemes.HarmonyPatches.PoliciesPanelPatch
{
    // Hooks into PoliciesPanel to inject and maintain the custom Themes tab.
    // Both RefreshPanel and SetParentButton must remove the tab before the original
    // runs (which validates policy buttons) and re-add it afterwards.
    public static class PoliciesPanelPatches
    {
        private static bool deployed;

        public static void Deploy()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || deployed) return;

            // RefreshPanel is private; SetParentButton is public.
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(PoliciesPanel), "RefreshPanel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
                prefix:  new PatchUtil.MethodDefinition(typeof(PoliciesPanelPatches), nameof(RefreshPanel_Prefix)),
                postfix: new PatchUtil.MethodDefinition(typeof(PoliciesPanelPatches), nameof(RefreshPanel_Postfix)));

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(PoliciesPanel), "SetParentButton",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance),
                prefix:  new PatchUtil.MethodDefinition(typeof(PoliciesPanelPatches), nameof(SetParentButton_Prefix)),
                postfix: new PatchUtil.MethodDefinition(typeof(PoliciesPanelPatches), nameof(SetParentButton_Postfix)));

            deployed = true;
            Debugger.Log("Building Themes: PoliciesPanel methods patched.");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(PoliciesPanel), "RefreshPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(PoliciesPanel), "SetParentButton",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance));

            deployed = false;
            Debugger.Log("Building Themes: PoliciesPanel methods unpatched.");
        }

        // RefreshPanel validates every policy button — our fake buttons would fail that check,
        // so we remove the Themes tab before the call and restore it after.
        public static void RefreshPanel_Prefix()
        {
            ThemePolicyTab.RemoveThemesTab();
        }

        public static void RefreshPanel_Postfix()
        {
            ThemePolicyTab.AddThemesTab();
        }

        // SetParentButton searches for a TutorialUITag on each button — our tab doesn't have one,
        // so we remove it before the call and restore it after.
        public static void SetParentButton_Prefix(UIButton button)
        {
            if (button == null) return;
            ThemePolicyTab.RemoveThemesTab();
        }

        public static void SetParentButton_Postfix(UIButton button)
        {
            if (button == null) return;
            ThemePolicyTab.AddThemesTab();
        }
    }
}
