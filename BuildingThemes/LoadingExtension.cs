using System;
using System.Linq;
using BuildingThemes.GUI;
using BuildingThemes.HarmonyPatches.BuildingInfoPatch;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;

namespace BuildingThemes
{
    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            Debugger.Initialize();

            Debugger.Log("ON_CREATED");
            Debugger.Log("Building Themes: Initializing Mod...");

            try
            {
                PolicyPanelEnabler.Register();
                BuildingThemesManager.instance.Reset();
                BuildingVariationManager.instance.Reset();

                // Conditionally deploy the prefab-cloning patch based on saved config.
                UpdateConfig();

                Debugger.Log("Building Themes: Mod successfully initialized.");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Building Themes: Exception in OnCreated.");
                UnityEngine.Debug.LogException(e);
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            Debugger.Log("ON_LEVEL_LOADED");
            Debugger.OnLevelLoaded();

            try
            {
                UnityEngine.Debug.Log("Building Themes 2: OnLevelLoaded mode=" + mode + " (" + (int)mode + ")");

                // Accept all in-game modes (not map/asset/scenario editors)
                bool isGameMode = mode == LoadMode.NewGame
                               || mode == LoadMode.LoadGame
                               || mode == LoadMode.NewGameFromScenario
                               || mode == LoadMode.LoadScenario;
                if (!isGameMode) return;

                BuildingThemesManager.instance.ImportThemes();
                UnityEngine.Debug.Log("Building Themes 2: ImportThemes done.");

                // Mod compatibility check — logs warnings and shows in-game panel for critical conflicts
                var conflicts = ModCompatibilityChecker.Check();
                foreach (var c in conflicts)
                {
                    string msg = "Building Themes 2 [" + c.Level + "] Mod conflict — "
                                 + c.ModName + ": " + c.Reason;
                    if (c.Level == ModCompatibilityChecker.Severity.Critical)
                        UnityEngine.Debug.LogError(msg);
                    else
                        UnityEngine.Debug.LogWarning(msg);
                }
                if (conflicts.Count > 0 && conflicts.Exists(c => c.Level == ModCompatibilityChecker.Severity.Critical))
                {
                    var criticals = conflicts.FindAll(c => c.Level == ModCompatibilityChecker.Severity.Critical);
                    // Queue to main thread so Unity UI is ready (level may still be loading)
                    SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(() =>
                    {
                        try
                        {
                            string text = "Building Themes 2 detected a critical mod conflict:\n\n" +
                                string.Join("\n\n", criticals.ConvertAll(c => "• " + c.ModName + ": " + c.Reason).ToArray());
                            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
                            if (panel != null)
                                panel.SetMessage("Building Themes 2 — Mod Conflict", text, false);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogException(ex);
                        }
                    });
                }

                if (Debugger.Enabled)
                    BuildingThemesManager.instance.ValidateAllThemes();

                // Warn if themes have missing assets and Skyve is installed (Skyve may have disabled them)
                if (SkyveDetector.IsInstalled)
                {
                    var themes = BuildingThemesManager.instance.GetAllThemes();
                    int missingCount = 0;
                    foreach (var theme in themes)
                    {
                        foreach (var building in theme.buildings)
                        {
                            if (building.include && PrefabCollection<BuildingInfo>.FindLoaded(building.name) == null)
                                missingCount++;
                        }
                    }
                    if (missingCount > 0)
                    {
                        UnityEngine.Debug.LogWarning(string.Format(
                            "Building Themes 2: {0} theme building(s) are missing. " +
                            "Skyve is installed — some assets may be disabled. " +
                            "Use 'Workshop Dependencies' in the Theme Manager to identify missing assets.",
                            missingCount));
                    }
                }

                PolicyPanelEnabler.UnlockPolicyToolbarButton();
                UnityEngine.Debug.Log("Building Themes 2: PolicyPanelEnabler done.");

                UIThemeManager.Initialize();
                UnityEngine.Debug.Log("Building Themes 2: UIThemeManager.Initialize done. instance=" + (UIThemeManager.instance != null ? "OK" : "NULL"));

                UIStyleButtonReplacer.ReplaceStyleButton();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Building Themes 2: Exception in OnLevelLoaded.");
                UnityEngine.Debug.LogException(e);
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            Debugger.Log("ON_LEVEL_UNLOADING");
            Debugger.OnLevelUnloading();

            BuildingThemesManager.instance.Reset();
            UIThemeManager.Destroy();
            GUI.UIUtils.ClearAtlasCache();
        }

        public override void OnReleased()
        {
            base.OnReleased();
            Debugger.Log("ON_RELEASED");

            BuildingThemesManager.instance.Reset();
            BuildingVariationManager.instance.Reset();
            PolicyPanelEnabler.Unregister();

            // Revert the prefab-cloning patch (the other patches are unloaded via
            // BuildingThemesMod.OnDisabled which calls Patcher.UnpatchAll).
            try
            {
                InitializePrefabPatch.Revert();
            }
            catch (Exception e)
            {
                Debugger.LogException(e);
            }

            Debugger.Log("Building Themes: Done!");
            Debugger.Deinitialize();
        }

        private void UpdateConfig()
        {
            // If config version is 0, disable the cloning feature if it has never been used.
            if (BuildingVariationManager.Enabled)
            {
                bool cloneFeatureUsed = false;

                if (BuildingThemesManager.instance.Configuration.version == 0)
                {
                    foreach (var theme in BuildingThemesManager.instance.Configuration.themes)
                    {
                        foreach (var building in theme.buildings)
                        {
                            if (building.baseName != null)
                            {
                                cloneFeatureUsed = true;
                                break;
                            }
                        }
                        if (cloneFeatureUsed) break;
                    }
                }
                else cloneFeatureUsed = true;

                if (cloneFeatureUsed)
                {
                    try { InitializePrefabPatch.Deploy(); }
                    catch (Exception e) { Debugger.LogException(e); }
                }
                else
                {
                    BuildingVariationManager.Enabled = false;
                }
            }
            BuildingThemesManager.instance.Configuration.version = 1;
            BuildingThemesManager.instance.SaveConfig();
        }
    }
}
