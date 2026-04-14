using System;
using BuildingThemes.GUI;
using BuildingThemes.HarmonyPatches.BuildingInfoPatch;
using ColossalFramework;
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
                Debugger.LogException(e);
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            Debugger.Log("ON_LEVEL_LOADED");
            Debugger.OnLevelLoaded();

            try
            {
                // Don't load if it's not a game
                if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame) return;

                BuildingThemesManager.instance.ImportThemes();

                PolicyPanelEnabler.UnlockPolicyToolbarButton();
                UIThemeManager.Initialize();
                UIStyleButtonReplacer.ReplaceStyleButton();
            }
            catch (Exception e)
            {
                Debugger.LogException(e);
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            Debugger.Log("ON_LEVEL_UNLOADING");
            Debugger.OnLevelUnloading();

            BuildingThemesManager.instance.Reset();
            UIThemeManager.Destroy();
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
