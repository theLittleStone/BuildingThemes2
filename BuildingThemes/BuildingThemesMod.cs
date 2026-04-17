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

        // Single canonical Harmony ID used by PatchUtil and ModCompatibilityChecker.
        public const string HarmonyId = "com.github.roberto-naharro.BuildingThemes2";


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
            UIHelperBase root = helper.AddGroup("Building Themes");
            try
            {
                // ── Behaviour ────────────────────────────────────────────────────────────
                // Global defaults applied when theme management is first enabled for a district.
                // Each district can override these in its own Themes tab (district policies panel).
                UIHelperBase behaviourGroup = root.AddGroup("Behaviour");

                var missingModeDropdown = behaviourGroup.AddDropdown(
                    "Default: missing workshop buildings",
                    new string[] {
                        "Skip (sparse slots, theme-only)",
                        "Fill with vanilla (supplement missing slots)",
                        "Fall back to vanilla (use vanilla if sparse)"
                    },
                    (int)BuildingThemesManager.MissingAssetBehavior,
                    delegate (int idx) { BuildingThemesManager.MissingAssetBehavior = (MissingAssetMode)idx; }
                ) as ColossalFramework.UI.UIDropDown;

                var emptyLevelDropdown = behaviourGroup.AddDropdown(
                    "Default: when theme has no buildings for a level",
                    new string[] {
                        "Vanilla fallback (game picks a vanilla building for that level)",
                        "Strict (freeze upgrades; building stays at current level)"
                    },
                    BuildingThemesManager.EmptyLevelBehavior == EmptyLevelBehavior.StrictThemeOnly ? 1 : 0,
                    delegate (int idx) {
                        BuildingThemesManager.EmptyLevelBehavior =
                            idx == 1 ? EmptyLevelBehavior.StrictThemeOnly : EmptyLevelBehavior.VanillaFallback;
                    }
                ) as ColossalFramework.UI.UIDropDown;

                var warningCheck = behaviourGroup.AddCheckbox("Warning message when selecting an invalid theme", UIThemePolicyItem.showWarning,
                    delegate (bool c) { UIThemePolicyItem.showWarning = c; }) as ColossalFramework.UI.UICheckBox;

                // ── Debugging ─────────────────────────────────────────────────────────────
                UIHelperBase debugGroup = root.AddGroup("Debugging");

                var debugCheck = debugGroup.AddCheckbox("Generate Debug Output", Debugger.Enabled,
                    delegate (bool c) { Debugger.Enabled = c; }) as ColossalFramework.UI.UICheckBox;

                // ── Maintenance ───────────────────────────────────────────────────────────
                UIHelperBase maintenanceGroup = root.AddGroup("Maintenance");

                var unlockCheck = maintenanceGroup.AddCheckbox("Unlock Policies Panel From Start", PolicyPanelEnabler.Unlock,
                    delegate (bool c) { PolicyPanelEnabler.Unlock = c; }) as ColossalFramework.UI.UICheckBox;
                var cloningCheck = maintenanceGroup.AddCheckbox("Enable Prefab Cloning (experimental — read warning below)", BuildingVariationManager.Enabled,
                    delegate (bool c) { BuildingVariationManager.Enabled = c; }) as ColossalFramework.UI.UICheckBox;
                maintenanceGroup.AddGroup(
                    "⚠ SAVE RISK: Cloned buildings are generated at load time and stored in your save. " +
                    "If you disable this option or unsubscribe the mod, all cloned buildings will vanish from your city on next load. " +
                    "The save file itself is not corrupted, but those buildings are gone permanently. " +
                    "Only enable this if you understand and accept that risk.");

                maintenanceGroup.AddButton("Reset to Defaults", () =>
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
                root.AddGroup("BuildingThemes is unable to read the BuildingThemes.xml file\n" +
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
                try { HarmonyPatches.Patcher.UnpatchAll(); }
                catch (Exception e) { Debugger.LogException(e); }
            }
        }
    }
}
