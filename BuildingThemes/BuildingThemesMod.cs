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
                var cloningCheck = group.AddCheckbox("Enable Prefab Cloning (experimental, not stable!)", BuildingVariationManager.Enabled,
                    delegate (bool c) { BuildingVariationManager.Enabled = c; }) as ColossalFramework.UI.UICheckBox;
                group.AddGroup("Warning: When you disable this option, spawned clones will disappear!");

                var warningCheck = group.AddCheckbox("Warning message when selecting an invalid theme", UIThemePolicyItem.showWarning,
                    delegate (bool c) { UIThemePolicyItem.showWarning = c; }) as ColossalFramework.UI.UICheckBox;
                var debugCheck = group.AddCheckbox("Generate Debug Output", Debugger.Enabled,
                    delegate (bool c) { Debugger.Enabled = c; }) as ColossalFramework.UI.UICheckBox;

                group.AddButton("Reset to Defaults", () =>
                {
                    PolicyPanelEnabler.Unlock = true;
                    BuildingVariationManager.Enabled = false;
                    UIThemePolicyItem.showWarning = true;
                    Debugger.Enabled = false;

                    if (unlockCheck != null)  unlockCheck.isChecked  = true;
                    if (cloningCheck != null) cloningCheck.isChecked = false;
                    if (warningCheck != null) warningCheck.isChecked = true;
                    if (debugCheck != null)   debugCheck.isChecked   = false;
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
