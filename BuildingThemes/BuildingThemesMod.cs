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


        // Passes the spawn position to GetRandomBuildingInfoPatch for the abandoned-building
        // replacement path (set by AddResourcePatch). positionIsValid is consumed (reset to false)
        // by GetRandomBuildingInfoPatch after each use so a stale position is never re-used.
        public static Vector3 position;
        public static bool positionIsValid;
        public static readonly string EIGHTY_ONE_MOD = "81 Tiles (Fixed for C:S 1.2+)";
        public static readonly string EIGHTY_ONE_2_ASSEMBLY = "EightyOne2";

        public string Name => "Building Themes 2";

        public string Description => Localization.Get("MOD_DESCRIPTION");

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase root = helper.AddGroup("Building Themes");
            try
            {
                // ── Behaviour ────────────────────────────────────────────────────────────
                // Global defaults applied when theme management is first enabled for a district.
                // Each district can override these in its own Themes tab (district policies panel).
                UIHelperBase behaviourGroup = root.AddGroup(Localization.Get("SETTINGS_GROUP_BEHAVIOUR"));

                var missingModeDropdown = behaviourGroup.AddDropdown(
                    Localization.Get("SETTINGS_MISSING_ASSETS_LABEL"),
                    new string[] {
                        Localization.Get("SETTINGS_MISSING_ASSETS_SKIP"),
                        Localization.Get("SETTINGS_MISSING_ASSETS_FILL"),
                        Localization.Get("SETTINGS_MISSING_ASSETS_FALLBACK")
                    },
                    (int)BuildingThemesManager.MissingAssetBehavior,
                    delegate (int idx) { BuildingThemesManager.MissingAssetBehavior = (MissingAssetMode)idx; }
                ) as ColossalFramework.UI.UIDropDown;

                var emptyLevelDropdown = behaviourGroup.AddDropdown(
                    Localization.Get("SETTINGS_EMPTY_LEVEL_LABEL"),
                    new string[] {
                        Localization.Get("SETTINGS_EMPTY_LEVEL_VANILLA"),
                        Localization.Get("SETTINGS_EMPTY_LEVEL_STRICT")
                    },
                    BuildingThemesManager.EmptyLevelBehavior == EmptyLevelBehavior.StrictThemeOnly ? 1 : 0,
                    delegate (int idx) {
                        BuildingThemesManager.EmptyLevelBehavior =
                            idx == 1 ? EmptyLevelBehavior.StrictThemeOnly : EmptyLevelBehavior.VanillaFallback;
                    }
                ) as ColossalFramework.UI.UIDropDown;

                var autoBulldozePaceDropdown = behaviourGroup.AddDropdown(
                    Localization.Get("SETTINGS_AUTO_BULLDOZE_PACE_LABEL"),
                    new string[] {
                        Localization.Get("SETTINGS_AUTO_BULLDOZE_PACE_GENTLE"),
                        Localization.Get("SETTINGS_AUTO_BULLDOZE_PACE_NORMAL"),
                        Localization.Get("SETTINGS_AUTO_BULLDOZE_PACE_AGGRESSIVE")
                    },
                    AutoBulldozeService.Pace,
                    delegate (int idx) { AutoBulldozeService.Pace = idx; }
                ) as ColossalFramework.UI.UIDropDown;

                var warningCheck = behaviourGroup.AddCheckbox(Localization.Get("SETTINGS_INVALID_THEME_WARNING"), UIThemePolicyItem.showWarning,
                    delegate (bool c) { UIThemePolicyItem.showWarning = c; }) as ColossalFramework.UI.UICheckBox;

                // ── Debugging ─────────────────────────────────────────────────────────────
                UIHelperBase debugGroup = root.AddGroup(Localization.Get("SETTINGS_GROUP_DEBUGGING"));

                var debugCheck = debugGroup.AddCheckbox(Localization.Get("SETTINGS_DEBUG_OUTPUT"), Debugger.Enabled,
                    delegate (bool c) { Debugger.Enabled = c; }) as ColossalFramework.UI.UICheckBox;

                // ── Maintenance ───────────────────────────────────────────────────────────
                UIHelperBase maintenanceGroup = root.AddGroup(Localization.Get("SETTINGS_GROUP_MAINTENANCE"));

                var unlockCheck = maintenanceGroup.AddCheckbox(Localization.Get("SETTINGS_UNLOCK_POLICIES"), PolicyPanelEnabler.Unlock,
                    delegate (bool c) { PolicyPanelEnabler.Unlock = c; }) as ColossalFramework.UI.UICheckBox;
                var cloningCheck = maintenanceGroup.AddCheckbox(Localization.Get("SETTINGS_PREFAB_CLONING"), BuildingVariationManager.Enabled,
                    delegate (bool c) { BuildingVariationManager.Enabled = c; }) as ColossalFramework.UI.UICheckBox;
                maintenanceGroup.AddGroup(Localization.Get("SETTINGS_PREFAB_CLONING_WARNING"));

                // ── District Styles Plus import ───────────────────────────────────────────
                // Shown always: DSP may be disabled but its style packages still exist on disk
                // and are loaded by the game into DistrictManager.m_Styles when a city is open.
                {
                    UIHelperBase dspGroup = root.AddGroup(Localization.Get("SETTINGS_GROUP_DSP"));

                    dspGroup.AddGroup(Localization.Get("SETTINGS_DSP_DESCRIPTION"));

                    dspGroup.AddButton(Localization.Get("SETTINGS_DSP_IMPORT_BUTTON"), () =>
                    {
                        try
                        {
                            var dm = DistrictManager.instance;
                            var manager = BuildingThemesManager.instance;

                            if (dm == null || dm.m_Styles == null || manager == null || manager.Configuration == null)
                            {
                                ExceptionPanel ep = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
                                ep.SetMessage(Localization.Get("SETTINGS_DSP_IMPORT_BUTTON"),
                                    Localization.Get("SETTINGS_DSP_NEEDS_GAME"), true);
                                return;
                            }

                            const string prefix = "[DSP] ";
                            int imported = 0;
                            int skipped = 0;

                            foreach (var style in dm.m_Styles)
                            {
                                if (style == null || style.BuiltIn) continue;
                                if (style.PackageName == "DSPTransient") continue;

                                string themeName = prefix + style.Name;

                                if (manager.GetThemeByName(themeName) != null)
                                {
                                    skipped++;
                                    continue;
                                }

                                var newTheme = new Configuration.Theme { name = themeName };
                                var buildingInfos = style.GetBuildingInfos();
                                if (buildingInfos != null)
                                {
                                    foreach (var bi in buildingInfos)
                                    {
                                        if (bi == null || bi.m_placementStyle != ItemClass.Placement.Automatic) continue;
                                        if (!newTheme.containsBuilding(bi.name))
                                            newTheme.buildings.Add(new Configuration.Building(bi.name) { include = true });
                                    }
                                }
                                manager.Configuration.themes.Add(newTheme);
                                imported++;
                            }

                            if (imported > 0)
                            {
                                manager.SaveConfig();
                                manager.RefreshDistrictThemeInfos();
                                if (UIThemeManager.instance != null)
                                    UIThemeManager.instance.RebuildThemeList();
                            }

                            string msg;
                            if (imported == 0 && skipped == 0)
                                msg = Localization.Get("SETTINGS_DSP_RESULT_NONE");
                            else if (imported == 0)
                                msg = Localization.Get("SETTINGS_DSP_RESULT_ALL_SKIPPED", skipped, prefix);
                            else if (skipped == 0)
                                msg = Localization.Get("SETTINGS_DSP_RESULT_IMPORTED", imported, prefix);
                            else
                                msg = Localization.Get("SETTINGS_DSP_RESULT_MIXED", imported, prefix, skipped);

                            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
                            panel.SetMessage(Localization.Get("SETTINGS_DSP_IMPORT_BUTTON"), msg, false);
                        }
                        catch (Exception e)
                        {
                            Debugger.LogException(e);
                        }
                    });
                }

                maintenanceGroup.AddButton(Localization.Get("SETTINGS_RESET_DEFAULTS"), () =>
                {
                    PolicyPanelEnabler.Unlock = true;
                    BuildingVariationManager.Enabled = false;
                    UIThemePolicyItem.showWarning = true;
                    Debugger.Enabled = false;
                    BuildingThemesManager.MissingAssetBehavior = MissingAssetMode.FillWithVanilla;
                    BuildingThemesManager.EmptyLevelBehavior = EmptyLevelBehavior.VanillaFallback;
                    AutoBulldozeService.Pace = 1;

                    if (unlockCheck != null)  unlockCheck.isChecked  = true;
                    if (cloningCheck != null) cloningCheck.isChecked = false;
                    if (warningCheck != null) warningCheck.isChecked = true;
                    if (debugCheck != null)   debugCheck.isChecked   = false;
                    if (missingModeDropdown != null) missingModeDropdown.selectedIndex = (int)MissingAssetMode.FillWithVanilla;
                    if (emptyLevelDropdown != null)  emptyLevelDropdown.selectedIndex  = (int)EmptyLevelBehavior.VanillaFallback;
                    if (autoBulldozePaceDropdown != null) autoBulldozePaceDropdown.selectedIndex = 1;
                });
            }
            catch
            {
                root.AddGroup(Localization.Get("SETTINGS_XML_CORRUPT") + "\n" +
                               System.IO.Path.GetFullPath("BuildingThemes.xml"));
            }
        }

        public void OnEnabled()
        {
            // Register the settings file before any SavedInt/SavedBool with this name are
            // first read, so the game does not log "not found or cannot be loaded" warnings.
            // OnEnabled can fire more than once per session (re-enable in mods panel, recovery
            // after a previous crash), so swallow the duplicate-key exception that
            // GameSettings.AddSettingsFile throws if the file is already registered.
            try
            {
                ColossalFramework.GameSettings.AddSettingsFile(
                    new ColossalFramework.SettingsFile { fileName = "BuildingThemes2" });
            }
            catch (Exception e)
            {
                Debugger.Log("BuildingThemes2 settings file already registered; skipping. (" + e.GetType().Name + ")");
            }

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
