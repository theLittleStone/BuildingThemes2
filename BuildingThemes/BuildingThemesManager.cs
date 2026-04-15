using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using System;
using ColossalFramework.Plugins;
using System.IO;
using System.Text;
using UnityEngine;
using BuildingThemes.Diagnostics;

namespace BuildingThemes
{
    public class BuildingThemesManager : Singleton<BuildingThemesManager>
    {
        // From BuildingManager.
        private const int AreaBuildingsLength = 4480;

        private const string ModConfigPath = "BuildingThemes.xml";
        private readonly DistrictThemeInfo[] districtThemeInfos = new DistrictThemeInfo[128];
        private readonly FastList<ushort>[] m_areaBuildings = new FastList<ushort>[AreaBuildingsLength];
        private bool m_areaBuildingsDirty = true;
        private bool importedModThemes = false;
        private bool importedStyles = false;

        private class DistrictThemeInfo
        {
            public bool blacklistMode = false;

            public readonly HashSet<Configuration.Theme> themes = new HashSet<Configuration.Theme>();

            // similar to BuildingManager.m_areaBuildings, but separate for every district
            public readonly FastList<ushort>[] areaBuildings = new FastList<ushort>[AreaBuildingsLength];

            // building upgrade mapping (prefabLevel1 --> prefabLevel2) for realistic building upgrades
            public readonly Dictionary<ushort, ushort> upgradeBuildings = new Dictionary<ushort, ushort>();
        }

        private const string userConfigPath = "BuildingThemes.xml";
        private Configuration _configuration;
        internal Configuration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    try
                    {
                        _configuration = Configuration.Deserialize(userConfigPath);

                        if (Debugger.Enabled)
                        {
                            Debugger.Log("Building Themes: User Configuration loaded.");
                        }

                        if (_configuration == null)
                        {
                            _configuration = new Configuration();
                            SaveConfig();
                        }

                        Debugger.xmlCorrupt = false;
                    }
                    catch
                    {
                        Debugger.xmlCorrupt = true;
                    }
                }

                return _configuration;
            }
        }

        internal void SaveConfig()
        {
            if (_configuration != null) Configuration.Serialize(userConfigPath, _configuration);
        }

        public void Reset()
        {
            for (int d = 0; d < districtThemeInfos.Length; d++)
            {
                districtThemeInfos[d] = null;
            }

            for (int i = 0; i < m_areaBuildings.Length; i++)
            {
                m_areaBuildings[i] = null;
            }

            _configuration = null;
            m_areaBuildingsDirty = true;
            importedModThemes = false;
            importedStyles = false;
            ThemeDiagnostics.Reset();
        }

        public void ImportThemes() 
        {
            if (!importedModThemes) ImportThemesFromThemeMods();
            if(!importedStyles) ImportStylesAsThemes();
        }


        public void ImportThemesFromThemeMods()
        {
            foreach (var pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo().Where(pluginInfo => pluginInfo.isEnabled))
            {
                try
                {
                    var config = Configuration.Deserialize(Path.Combine(pluginInfo.modPath, ModConfigPath));
                    if (config == null)
                    {
                        continue;
                    }
                    foreach (var theme in config.themes)
                    {
                        AddModTheme(theme, pluginInfo.name);
                    }
                }
                catch (Exception e)
                {
                    Debugger.Log("Error while parsing BuildingThemes.xml of mod " + pluginInfo.name);
                    Debugger.LogException(e);
                }
            }

            importedModThemes = true;
        }

        public void ImportStylesAsThemes()
        {
            var styles = DistrictManager.instance.m_Styles;
            if (styles == null)
            {
                return;
            }
            foreach (var style in styles)
            {
                try
                {
                    AddStyleTheme(style);
                }
                catch (Exception e)
                {
                    Debugger.Log("Error while importing style " + style.FullName);
                    Debugger.LogException(e);
                }
            }
            for (byte districtId = 0; districtId < DistrictManager.instance.m_districts.m_buffer.Length; ++districtId)
            {
                var district = DistrictManager.instance.m_districts.m_buffer[districtId];
                if (district.m_flags == District.Flags.None || district.m_Style <= 0)
                {
                    continue;
                }
                var style = DistrictManager.instance.m_Styles[district.m_Style - 1];
                var stylePackage = style.PackageName;
                Singleton<DistrictManager>.instance.m_districts.m_buffer[districtId].m_Style = 0;
                ToggleThemeManagement(districtId, true);
                var theme = GetThemeByStylePackage(stylePackage);
                EnableTheme(districtId, theme);
                Debugger.LogFormat("Theme \"{0}\" was enabled for districtId={1} instead of style \"{2}\" (packageName={3})",
                    theme.name, districtId, style.Name, style.PackageName);
            }

            importedStyles = true;
        }

        private void AddModTheme(Configuration.Theme modTheme, string modName)
        {
            if (modTheme == null)
            {
                return;
            }
            var theme = AddImportedTheme(modTheme.buildings, modTheme.name, null);

            Debugger.LogFormat(
                "Imported theme from mod \"{0}\" as theme \"{1}\". Buildings in mod: {2}. Buildings in theme: {3} ",
                modTheme.buildings.Count,
                modName, theme.name, theme.buildings.Count);
        }


        // Maps DistrictStyle.Name constants → locale keys used by the game for display.
        // Keys resolve to the style's official name (e.g. "Bridges & Piers", "European Suburbia").
        private static readonly System.Collections.Generic.Dictionary<string, string> s_styleLocaleKeys =
            new System.Collections.Generic.Dictionary<string, string>
        {
            { DistrictStyle.kEuropeanSuburbiaStyleName, "STYLES_EUROPEANSUBURBIA"    },
            { DistrictStyle.kModderPack5StyleName,      "STYLES_MODDERPACKFIVE"      },
            { DistrictStyle.kModderPack11StyleName,     "STYLES_MODDERPACKELEVEN"    },
            { DistrictStyle.kModderPack14StyleName,     "STYLES_MODDERPACKFOURTEEN"  },
            { DistrictStyle.kModderPack16StyleName,     "STYLES_MODDERPACKSIXTEEN"   },
            { DistrictStyle.kModderPack18StyleName,     "STYLES_MODDERPACKEIGHTEEN"  },
            { DistrictStyle.kModderPack20StyleName,     "STYLES_MODDERPACKTWENTY"    },
            { DistrictStyle.kModderPack21StyleName,     "STYLES_MODDERPACKTWENTYONE" },
            { DistrictStyle.kModderPack24StyleName,     "STYLES_MODDERPACKTWENTYFOUR"},
            { DistrictStyle.kModderPack25StyleName,     "STYLES_MODDERPACKTWENTYFIVE"},
            { DistrictStyle.kModderPack26StyleName,     "STYLES_MODDERPACKTWENTYSIX" },
        };

        private void AddStyleTheme(DistrictStyle style)
        {
            if (style.Name == DistrictStyle.kEuropeanStyleName) return; //skip builtin style

            var allBuildingInfos = style.GetBuildingInfos();

            // Derive DLC masks for this style by OR-ing the bits of all its buildings
            // (including non-growable ones, which reliably carry the correct DLC bits).
            var packMask = SteamHelper.ModderPackBitMask.None;
            var expansionMask = SteamHelper.ExpansionBitMask.None;
            if (allBuildingInfos != null)
                foreach (var b in allBuildingInfos)
                {
                    if (b == null) continue;
                    packMask      |= b.m_requiredModderPack;
                    expansionMask |= b.m_requiredExpansion;
                }

            // Collect growable zone buildings (Placement.Automatic) from the style.
            // Non-growable buildings (unique, service, parks, sub-buildings) are skipped:
            // they cannot spawn in zones and would show as "(Not Loaded)" in the Theme Manager.
            var styleNames = new System.Collections.Generic.HashSet<string>();
            var buildings = new System.Collections.Generic.List<Configuration.Building>();
            if (allBuildingInfos != null)
            {
                foreach (var b in allBuildingInfos)
                {
                    if (b == null || b.m_placementStyle != ItemClass.Placement.Automatic) continue;
                    if (styleNames.Add(b.name))
                        buildings.Add(new Configuration.Building { name = b.name, fromStyle = true });
                }
            }

            // If this is a paid DLC pack, skip it entirely when the player doesn't own it.
            bool isModderPackDlc = packMask != SteamHelper.ModderPackBitMask.None;
            bool isExpansionDlc  = expansionMask != SteamHelper.ExpansionBitMask.None;

            if (isModderPackDlc)
            {
                var ownedMask = SteamHelper.GetOwnedModderPackMask();
                if ((ownedMask & packMask) == SteamHelper.ModderPackBitMask.None)
                {
                    Debugger.LogFormat("Skipping style \"{0}\": modder pack DLC not owned (mask {1}).", style.FullName, packMask);
                    return;
                }
            }

            if (isExpansionDlc)
            {
                var ownedMask = SteamHelper.GetOwnedExpansionMask();
                if ((ownedMask & expansionMask) == SteamHelper.ExpansionBitMask.None)
                {
                    Debugger.LogFormat("Skipping style \"{0}\": expansion DLC not owned (mask {1}).", style.FullName, expansionMask);
                    return;
                }
            }

            // Some DLC packs tag their growable buildings with m_requiredModderPack but do not
            // register them all in the DistrictStyle (e.g. Heart of Korea KR R* residential).
            // Scan PrefabCollection for any Placement.Automatic building that carries this
            // style's pack bit and hasn't been added yet. Works for any current or future DLC.
            if (packMask != SteamHelper.ModderPackBitMask.None)
            {
                uint prefabCount = (uint)PrefabCollection<BuildingInfo>.PrefabCount();
                for (uint i = 0; i < prefabCount; i++)
                {
                    BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetPrefab(i);
                    if (prefab == null) continue;
                    if (prefab.m_placementStyle != ItemClass.Placement.Automatic) continue;
                    if ((prefab.m_requiredModderPack & packMask) == SteamHelper.ModderPackBitMask.None) continue;
                    if (!styleNames.Add(prefab.name)) continue;
                    buildings.Add(new Configuration.Building { name = prefab.name, fromStyle = true });
                }
            }

            var theme = AddImportedTheme(buildings, FormatStyleName(style), style.PackageName);
            theme.isDlc = isModderPackDlc || isExpansionDlc;

            // Wire up the locale key so the UI can show the official expansion name.
            string localeKey;
            if (s_styleLocaleKeys.TryGetValue(style.Name, out localeKey))
                theme.localeKey = localeKey;

            Debugger.LogFormat(
                "Imported style \"{0}\" as theme \"{1}\". isDlc={2}. Style buildings: {3}. Theme buildings: {4}.",
                style.FullName, theme.name, theme.isDlc,
                allBuildingInfos != null ? allBuildingInfos.Length : 0,
                theme.buildings.Count);
        }

        private static string FormatStyleName(DistrictStyle style)
        {
            if (style.BuiltIn)
            {
                if (style.Name == DistrictStyle.kEuropeanStyleName) return "European";
                if (style.Name == DistrictStyle.kEuropeanSuburbiaStyleName) return "European Suburbia";
            }

            return style.Name;
        }

        private Configuration.Theme AddImportedTheme(List<Configuration.Building> builtInBuildings, string themeName, string stylePackage)
        {
            var theme = Configuration.getTheme(themeName);
            if (theme == null)
            {
                theme = new Configuration.Theme
                {
                    name = themeName,
                    stylePackage = stylePackage
                };
                Configuration.themes.Add(theme);
            }
            theme.isBuiltIn = true;
            if (builtInBuildings != null)
            {
                foreach (var builtInBuilding in builtInBuildings)
                {
                    var building = theme.getBuilding(builtInBuilding.name);
                    if (building == null)
                    {
                        building = new Configuration.Building(builtInBuilding);
                        theme.buildings.Add(building);
                    }
                    else if (building.builtInBuilding == null)
                    {
                        building.builtInBuilding = builtInBuilding;
                    }
                    building.fromStyle = builtInBuilding.fromStyle;
                }
            }

            return theme;
        }

        public void EnableTheme(byte districtId, Configuration.Theme theme)
        {
            if (!IsThemeManagementEnabled(districtId)) return;

            if (Debugger.Enabled)
            {
                Debugger.LogFormat("Building Themes: BuildingThemesManager. Enabling theme {0} for district {1}.", theme.name, districtId);
            }
            var themes = GetDistrictThemes(districtId, true);

            if (!districtThemeInfos[districtId].themes.Add(theme))
            {
                if (Debugger.Enabled)
                {
                    Debugger.LogFormat("Building Themes: BuildingThemesManager. Theme {0} was already enabled for district {1}.", theme.name, districtId);
                }
                return;
            }

            CompileDistrictThemes(districtId);
        }

        public void DisableTheme(byte districtId, Configuration.Theme theme)
        {
            if (!IsThemeManagementEnabled(districtId)) return;

            if (Debugger.Enabled)
            {
                Debugger.LogFormat("Building Themes: BuildingThemesManager. Disabling theme {0} for district {1}.", theme.name, districtId);
            }

            if (!districtThemeInfos[districtId].themes.Remove(theme))
            {
                if (Debugger.Enabled)
                {
                    Debugger.LogFormat("Building Themes: BuildingThemesManager. Theme {0} was already disabled for district {1}.", theme.name, districtId);
                }
                return;
            }

            CompileDistrictThemes(districtId);
        }

        public void RefreshDistrictThemeInfos()
        {
            for (byte d = 0; d < districtThemeInfos.Length; d++)
            {
                var info = districtThemeInfos[d];
                if (info == null) continue;

                // Remove themes which are no longer listed in the configuration
                info.themes.RemoveWhere(theme => !Configuration.themes.Contains(theme));

                CompileDistrictThemes(d);
            }
        }

        public void setThemeInfo(byte districtId, HashSet<Configuration.Theme> themes, bool blacklistMode)
        {
            var info = districtThemeInfos[districtId];

            if (info == null)
            {
                info = new DistrictThemeInfo();
                districtThemeInfos[districtId] = info;
            }
            else
            {
                info.themes.Clear();
                info.upgradeBuildings.Clear();
            }

            info.blacklistMode = blacklistMode;

            // Add the themes to the district theme info
            info.themes.UnionWith(themes);

            CompileDistrictThemes(districtId);
        }

        //TODO move this method to DistrictThemeInfo class?
        public void CompileDistrictThemes(byte districtId)
        {
            var info = districtThemeInfos[districtId];

            if (info == null) return;

            HashSet<Configuration.Theme> enabledThemes = info.themes;
            HashSet<Configuration.Theme> blacklistedThemes = null;

            if (info.blacklistMode)
            {
                blacklistedThemes = new HashSet<Configuration.Theme>(GetAllThemes());
                blacklistedThemes.ExceptWith(info.themes);
            }

            if (Debugger.Enabled)
            {
                Debugger.LogFormat("Compiling theme data for district {0}. Enabled Themes: {1}, Blacklist Themes: {2}",
                    districtId, enabledThemes.Count, blacklistedThemes == null ? 0 : blacklistedThemes.Count);
                ThemeDiagnostics.BeginCompile(districtId);
            }

            // Create custom areaBuildings fastlist array for this district
            RefreshAreaBuildings(info.areaBuildings, enabledThemes, blacklistedThemes, true, districtId);

            // Create upgrade mapping
            info.upgradeBuildings.Clear();
            foreach (var theme in enabledThemes)
            {
                foreach (var building in theme.buildings)
                {
                    if (building.upgradeName == null) continue;

                    var fromPrefab = PrefabCollection<BuildingInfo>.FindLoaded(building.name);
                    var toPrefab = PrefabCollection<BuildingInfo>.FindLoaded(building.upgradeName);

                    if (fromPrefab != null && toPrefab != null && !info.upgradeBuildings.ContainsKey((ushort)fromPrefab.m_prefabDataIndex))
                    {
                        info.upgradeBuildings.Add((ushort)fromPrefab.m_prefabDataIndex, (ushort)toPrefab.m_prefabDataIndex);
                    }
                }
            }

            if (Debugger.Enabled)
            {
                Debugger.LogFormat("Upgrade Mappings in district {0}: {1}", districtId, info.upgradeBuildings.Count);
                ThemeDiagnostics.LogReport(districtId);
            }
        }

        public void ToggleThemeManagement(byte districtId, bool enabled)
        {
            if (enabled == IsThemeManagementEnabled(districtId)) return;

            if (enabled)
            {
                setThemeInfo(districtId, getDefaultThemes(districtId), IsBlacklistModeEnabled(0));
            }
            else
            {
                districtThemeInfos[districtId] = null;
            }
        }

        public bool IsThemeManagementEnabled(byte districtId)
        {
            return districtThemeInfos[districtId] != null;
        }

        public void ToggleBlacklistMode(byte districtId, bool enabled)
        {
            if (!IsThemeManagementEnabled(districtId) || enabled == IsBlacklistModeEnabled(districtId)) return;

            districtThemeInfos[districtId].blacklistMode = enabled;
            CompileDistrictThemes(districtId);
        }

        public bool IsBlacklistModeEnabled(byte districtId)
        {
            var info = districtThemeInfos[districtId];

            if (info != null)
            {
                return info.blacklistMode;
            }
            else if (districtId != 0)
            {
                return IsBlacklistModeEnabled(0);
            }
            else
            {
                return false;
            }
        }

        public BuildingInfo GetUpgradeBuildingInfo(ushort prefabIndex, byte districtId)
        {
            var info = districtThemeInfos[districtId];

            if (info == null)
            {
                return null;
            }

            ushort upgradePrefabIndex;
            if (!info.upgradeBuildings.TryGetValue(prefabIndex, out upgradePrefabIndex))
            {
                return null;
            }

            return PrefabCollection<BuildingInfo>.GetPrefab(upgradePrefabIndex);
        }

        private void RefreshAreaBuildings(FastList<ushort>[] m_areaBuildings, HashSet<Configuration.Theme> enabledThemes, HashSet<Configuration.Theme> blacklistedThemes, bool includeVariations, byte diagnosticsDistrictId = 255)
        {
            bool recordDiagnostics = Debugger.Enabled && diagnosticsDistrictId != 255;

            int areaBuildingsLength = m_areaBuildings.Length;
            for (int i = 0; i < areaBuildingsLength; i++)
            {
                m_areaBuildings[i] = null;
            }
            int prefabCount = PrefabCollection<BuildingInfo>.PrefabCount();
            for (int j = 0; j < prefabCount; j++)
            {
                BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetPrefab((uint)j);
                if (prefab != null && prefab.m_class.m_service != ItemClass.Service.None && prefab.m_placementStyle == ItemClass.Placement.Automatic && prefab.m_class.m_service <= ItemClass.Service.Office)
                {
                    int privateServiceIndex = ItemClass.GetPrivateServiceIndex(prefab.m_class.m_service);

                    if (privateServiceIndex != -1) {
                        if (prefab.m_cellWidth < 1 || prefab.m_cellWidth > 4 || prefab.m_cellLength < 1 || prefab.m_cellLength > 4)
                        {
                            continue;
                        }
                        else
                        {
                            // mod begin
                            if (!includeVariations && BuildingVariationManager.instance.IsVariation(prefab.name))
                            {
                                if (recordDiagnostics) ThemeDiagnostics.RecordBuilding(diagnosticsDistrictId, prefab.name, RejectionReason.Variation);
                                continue;
                            }

                            int spawnRateSum = 0;
                            int hits = 0;

                            if (enabledThemes != null && enabledThemes.Count > 0)
                            {
                                foreach (var theme in enabledThemes)
                                {
                                    var building = theme.getBuilding(prefab.name);

                                    if (building != null && building.include)
                                    {
                                        hits++;
                                        // limit spawn rate to 100
                                        spawnRateSum += Mathf.Clamp(building.spawnRate, 0, 100);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                spawnRateSum = 1;
                                hits = 1;
                            }

                            if (hits == 0 && blacklistedThemes != null)
                            {
                                bool onBlacklist = false;

                                foreach (var theme in blacklistedThemes)
                                {
                                    var building = theme.getBuilding(prefab.name);

                                    if (building != null && building.include)
                                    {
                                        onBlacklist = true;
                                        break;
                                    }
                                }

                                if (onBlacklist)
                                {
                                    if (recordDiagnostics) ThemeDiagnostics.RecordBuilding(diagnosticsDistrictId, prefab.name, RejectionReason.NotInTheme);
                                    continue;
                                }
                                else
                                {
                                    spawnRateSum = 10;
                                    hits = 1;
                                }
                            }

                            if (hits == 0)
                            {
                                if (recordDiagnostics) ThemeDiagnostics.RecordBuilding(diagnosticsDistrictId, prefab.name, RejectionReason.NotInTheme);
                                continue;
                            }

                            if (spawnRateSum == 0)
                            {
                                if (recordDiagnostics) ThemeDiagnostics.RecordBuilding(diagnosticsDistrictId, prefab.name, RejectionReason.ZeroSpawnRate);
                                continue;
                            }

                            if (recordDiagnostics) ThemeDiagnostics.RecordBuilding(diagnosticsDistrictId, prefab.name, RejectionReason.Accepted);

                            // mod end

                            int areaIndex = GetAreaIndex(prefab.m_class.m_service, prefab.m_class.m_subService, prefab.m_class.m_level, prefab.m_cellWidth, prefab.m_cellLength, prefab.m_zoningMode);
                            if (m_areaBuildings[areaIndex] == null)
                            {
                                m_areaBuildings[areaIndex] = new FastList<ushort>();
                            }

                            // mod begin
                            int spawnRate = spawnRateSum / hits;
                            for (uint s = 0; s < spawnRate; s++)
                            {
                                // mod end
                                m_areaBuildings[areaIndex].Add((ushort)j);
                                // mod begin
                            }
                            // mod end
                        }
                    }
                }
            }
            int num3 = 24;
            for (int k = 0; k < num3; k++)
            {
                for (int l = 0; l < 5; l++)
                {
                    for (int m = 0; m < 4; m++)
                    {
                        for (int n = 1; n < 4; n++)
                        {
                            int num4 = k;
                            num4 = num4 * 5 + l;
                            num4 = num4 * 4 + m;
                            num4 = num4 * 4 + n;
                            num4 *= 2;
                            FastList<ushort> fastList = m_areaBuildings[num4];
                            FastList<ushort> fastList2 = m_areaBuildings[num4 - 2];
                            if (fastList2 != null)
                            {
                                if (fastList == null)
                                {
                                    m_areaBuildings[num4] = fastList2;
                                }
                                else
                                {
                                    for (int num5 = 0; num5 < fastList2.m_size; num5++)
                                    {
                                        fastList.Add(fastList2.m_buffer[num5]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static int GetAreaIndex(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int width, int length, BuildingInfo.ZoningMode zoningMode)
        {
            int privateSubServiceIndex = ItemClass.GetPrivateSubServiceIndex(subService);
            int num = (int)((privateSubServiceIndex == -1 ? ItemClass.GetPrivateServiceIndex(service) : 8 + privateSubServiceIndex) * 5 + level);
            return zoningMode != BuildingInfo.ZoningMode.CornerRight ? (int)(((num * 4 + width - 1) * 4 + length - 1) * 2 + zoningMode) : ((num * 4 + length - 1) * 4 + width - 1) * 2 + 1;

        }

        public FastList<ushort> GetAreaBuildings(byte districtId, int areaIndex)
        {
            var info = districtThemeInfos[districtId];

            // Theme management enabled in district? return custom fastlist for district
            if (info != null)
            {
                return info.areaBuildings[areaIndex];
            }
            // Theme management not enabled in district? return fastlist for city-wide "district"
            else if (districtId != 0)
            {
                return GetAreaBuildings(0, areaIndex);
            }
            // Theme management not enabled in city-wide district? return fastlist of the game
            else
            {
                if (m_areaBuildingsDirty)
                {
                    RefreshAreaBuildings(m_areaBuildings, null, null, false);
                    m_areaBuildingsDirty = false;
                }
                return m_areaBuildings[areaIndex];
            }
        }

        private HashSet<Configuration.Theme> getDefaultThemes(uint districtIdx)
        {
            var theme = new HashSet<Configuration.Theme>();

            if (districtIdx == 0)
            {
                /*
                // city-wide default derived from environment (european, sunny, boreal, tropical)

                var env = Singleton<SimulationManager>.instance.m_metaData.m_environment;

                if (env == "Europe")
                {
                    theme.Add(GetThemeByName("European"));
                }
                else
                {
                    theme.Add(GetThemeByName("International"));
                }

                if (Debugger.Enabled)
                {
                    Debugger.LogFormat("Building Themes: Environment is {0}. Selected default builtin theme.", env);
                }
                */

                // By default no theme is enabled, so custom buildings grow
            }
            else
            {
                // district theme derived from city-wide theme

                theme.UnionWith(GetDistrictThemes(0, true));

                if (Debugger.Enabled)
                {
                    Debugger.LogFormat("Building Themes: Deriving theme for district {0} from city-wide theme.", districtIdx);
                }
            }

            return theme;
        }

        public HashSet<Configuration.Theme> GetDistrictThemes(byte districtId, bool initializeIfNull)
        {
            if (IsThemeManagementEnabled(districtId))
            {
                return districtThemeInfos[districtId].themes;
            }
            else
            {
                return initializeIfNull ? getDefaultThemes(districtId) : null;
            }
        }

        public List<Configuration.Theme> GetAllThemes()
        {
            return Configuration.themes;
        }

        /// <summary>
        /// Logs a validation summary for all themes to the debug log.
        /// Called after ImportThemes() on level load. Requires Debugger.Enabled.
        /// </summary>
        public void ValidateAllThemes()
        {
            var themes = GetAllThemes();
            var sb = new StringBuilder();
            sb.AppendLine("[BuildingThemes2] Theme validation summary:");

            int prefabCount = PrefabCollection<BuildingInfo>.PrefabCount();
            var loadedNames = new HashSet<string>();
            for (int i = 0; i < prefabCount; i++)
            {
                var prefab = PrefabCollection<BuildingInfo>.GetPrefab((uint)i);
                if (prefab != null) loadedNames.Add(prefab.name);
            }

            foreach (var theme in themes)
            {
                int total = 0, loaded = 0, missing = 0;
                foreach (var building in theme.buildings)
                {
                    if (!building.include) continue;
                    total++;
                    if (loadedNames.Contains(building.name)) loaded++;
                    else missing++;
                }

                if (total == 0)
                    sb.AppendFormat("  {0}: EMPTY (no buildings)\n", theme.name);
                else if (missing == 0)
                    sb.AppendFormat("  {0}: {1} buildings, all loaded\n", theme.name, total);
                else
                    sb.AppendFormat("  {0}: {1} buildings, {2} loaded, {3} MISSING\n",
                        theme.name, total, loaded, missing);
            }

            Debugger.Log(sb.ToString());
        }

        public Configuration.Theme GetThemeByName(string themeName)
        {
            return Configuration.themes.FirstOrDefault(theme => theme.name == themeName);
        }

        private Configuration.Theme GetThemeByStylePackage(string stylePackage)
        {
            return Configuration.themes.FirstOrDefault(theme => (theme.stylePackage == stylePackage || (stylePackage == DistrictStyle.kEuropeanStyleName && theme.name == "European")));
        }
    }
}
