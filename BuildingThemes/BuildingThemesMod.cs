using System;
using ICities;
using BuildingThemes.GUI;
using CitiesHarmony.API;
using ColossalFramework.UI;
using UnityEngine;

namespace BuildingThemes
{
    public class BuildingThemesMod : IUserMod
    {
        public static bool xmlCorrupt = false;


        // we'll use this variable to pass the building position to GetRandomBuildingInfo method. It's here to make possible 81 Tiles compatibility
        public static Vector3 position;
        public static readonly string EIGHTY_ONE_MOD = "81 Tiles (Fixed for C:S 1.2+)";
        public static readonly string EIGHTY_ONE_2_ASSEMBLY = "EightyOne2";

        public string Name => "Building Themes 2";

        public string Description =>
            "Create building themes and apply them to cities and districts. " +
            "Community-maintained fork of Building Themes by boformer (Sebastian Schöner). " +
            "Harmony 2.x migration by roberto-naharro.";

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Building Themes");
            try
            {
                var unlockCheck = group.AddCheckbox("Unlock Policies Panel From Start", PolicyPanelEnabler.Unlock,
                    delegate (bool c) { PolicyPanelEnabler.Unlock = c; }) as ColossalFramework.UI.UICheckBox;
                var cloningCheck = group.AddCheckbox("Enable Prefab Cloning (experimental — read warning below)", BuildingVariationManager.Enabled,
                    delegate (bool c) { BuildingVariationManager.Enabled = c; }) as ColossalFramework.UI.UICheckBox;
                group.AddGroup(
                    "⚠ SAVE RISK: Cloned buildings are generated at load time and stored in your save. " +
                    "If you disable this option or unsubscribe the mod, all cloned buildings will vanish from your city on next load. " +
                    "The save file itself is not corrupted, but those buildings are gone permanently. " +
                    "Only enable this if you understand and accept that risk.");

                var warningCheck = group.AddCheckbox("Warning message when selecting an invalid theme", UIThemePolicyItem.showWarning,
                    delegate (bool c) { UIThemePolicyItem.showWarning = c; }) as ColossalFramework.UI.UICheckBox;
                var debugCheck = group.AddCheckbox("Generate Debug Output", Debugger.Enabled,
                    delegate (bool c) { Debugger.Enabled = c; }) as ColossalFramework.UI.UICheckBox;

                // Global defaults — applied to new districts when theme management is first enabled.
                // Each district can override these in its own Themes tab (district policies panel).
                var missingModeDropdown = group.AddDropdown(
                    "Default: missing workshop buildings",
                    new string[] {
                        "Skip (sparse slots, theme-only)",
                        "Fill with vanilla (supplement missing slots)",
                        "Fall back to vanilla (use vanilla if sparse)"
                    },
                    (int)BuildingThemesManager.MissingAssetBehavior,
                    delegate (int idx) { BuildingThemesManager.MissingAssetBehavior = (MissingAssetMode)idx; }
                ) as ColossalFramework.UI.UIDropDown;

                var emptyLevelDropdown = group.AddDropdown(
                    "Default: when theme has no buildings for a level",
                    new string[] {
                        "Vanilla fallback (spawn vanilla for uncovered levels)",
                        "Cascade from theme (reuse lower level buildings)",
                        "Strict (freeze levels, block upgrades past theme)"
                    },
                    (int)BuildingThemesManager.EmptyLevelBehavior,
                    delegate (int idx) { BuildingThemesManager.EmptyLevelBehavior = (EmptyLevelBehavior)idx; }
                ) as ColossalFramework.UI.UIDropDown;

                group.AddButton("Reset to Defaults", () =>
                {
                    PolicyPanelEnabler.Unlock = true;
                    BuildingVariationManager.Enabled = false;
                    UIThemePolicyItem.showWarning = true;
                    Debugger.Enabled = false;
                    BuildingThemesManager.MissingAssetBehavior = MissingAssetMode.FillWithVanilla;
                    BuildingThemesManager.EmptyLevelBehavior = EmptyLevelBehavior.VanillaFallback;

                    if (unlockCheck != null)  unlockCheck.isChecked  = true;
                    if (cloningCheck != null) cloningCheck.isChecked = false;
                    if (warningCheck != null) warningCheck.isChecked = true;
                    if (debugCheck != null)   debugCheck.isChecked   = false;
                    if (missingModeDropdown != null) missingModeDropdown.selectedIndex = (int)MissingAssetMode.FillWithVanilla;
                    if (emptyLevelDropdown != null)  emptyLevelDropdown.selectedIndex  = (int)EmptyLevelBehavior.VanillaFallback;
                });
            }
            catch
            {
                group.AddGroup("BuildingThemes is unable to read the BuildingThemes.xml file\n" +
                               "that stores your settings and themes!\n" +
                               "To fix it, delete this file and restart the game:\n" +
                               "{Steam folder}\\steamapps\\common\\Cities_Skylines\\BuildingThemes.xml");
            }
        }

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => HarmonyPatches.Patcher.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                HarmonyPatches.Patcher.UnpatchAll();
            }
        }
    }
}
