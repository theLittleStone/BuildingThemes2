using System;
using System.Reflection;
using CitiesHarmony.API;
using ColossalFramework.UI;
using BuildingThemes.GUI;
using UnityEngine;

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

            // RefreshPanel validates every policy button — our tab would fail that check,
            // so we remove it before the call and restore it after.
            // SetParentButton no longer needs a patch: we add TutorialUITag to our tab
            // in AddThemesTab() so it survives SetParentButton's component search.
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(PoliciesPanel), "RefreshPanel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
                prefix:  new PatchUtil.MethodDefinition(typeof(PoliciesPanelPatches), nameof(RefreshPanel_Prefix)),
                postfix: new PatchUtil.MethodDefinition(typeof(PoliciesPanelPatches), nameof(RefreshPanel_Postfix)));

            deployed = true;
            Debugger.Log("PoliciesPanel methods patched.");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(PoliciesPanel), "RefreshPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));

            deployed = false;
            Debugger.Log("PoliciesPanel methods unpatched.");
        }

        // RefreshPanel validates every policy button — our fake buttons would fail that check,
        // so we remove the Themes tab before the call and restore it after.
        public static void RefreshPanel_Prefix()
        {
            try { ThemePolicyTab.RemoveThemesTab(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        public static void RefreshPanel_Postfix()
        {
            try { ThemePolicyTab.AddThemesTab(); }
            catch (Exception e) { Debug.LogException(e); }
        }

    }
}
