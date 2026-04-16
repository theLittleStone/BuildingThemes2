using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BuildingThemes.GUI
{
    public class UIThemeManager : UIPanel
    {
        private UITitleBar m_title;
        private UIBuildingFilter m_filter;
        private UIFastList m_themeSelection;
        private UIButton m_themeAdd;
        private UIButton m_themeRemove;
        private UIButton m_dependencies;
        private UIFastList m_buildingSelection;
        private UIButton m_includeAll;
        private UIButton m_includeNone;
        private UILabel m_includeLabel;
        private UIButton m_excludeMissing;
        private UIButton m_includeValid;
        private UILabel m_excludeLabel;
        private UILabel m_counterLabel;
        private UIBuildingPreview m_buildingPreview;
        private UIBuildingOptions m_buildingOptions;
        private UIButton m_cloneBuilding;
        private UIPanel m_leftPanel;
        private UIPanel m_middlePanel;
        private UIPanel m_rightPanel;
        private UISprite m_resizeHandle;

        private bool m_resizing = false;
        private Vector3 m_resizeStartMouse;
        private Vector2 m_resizeStartSize;

        private Dictionary<Configuration.Theme, List<BuildingItem>> m_themes = new Dictionary<Configuration.Theme, List<BuildingItem>>();
        private bool m_isDistrictThemesDirty = false;

        #region Constant values
        private const float LEFT_WIDTH = 250;
        private const float MIDDLE_WIDTH = 425;
        private const float RIGHT_WIDTH = 275;
        private const float HEIGHT = 550;
        private const float SPACING = 5;
        private const float TITLE_HEIGHT = 40;
        // Minimum window dimensions (= original fixed size)
        private const float MIN_WIDTH = SPACING + LEFT_WIDTH + SPACING + MIDDLE_WIDTH + SPACING + RIGHT_WIDTH + SPACING;
        private const float MIN_HEIGHT = TITLE_HEIGHT + HEIGHT + SPACING;
        #endregion

        private static GameObject _gameObject;
        private static UIThemeManager _instance;

        public static UIThemeManager instance
        {
            get { return _instance; }
        }

        public Configuration.Theme selectedTheme
        {
            get { return m_themeSelection.selectedItem as Configuration.Theme; }
        }

        public BuildingItem selectedBuilding
        {
            get { return m_buildingSelection.selectedItem as BuildingItem; }
        }

        public UIBuildingPreview buildingPreview
        {
            get { return m_buildingPreview; }
        }

        public static void Initialize()
        {
            try
            {
                // Destroy the UI if already exists
                _gameObject = GameObject.Find("BuildingThemes");
                Destroy();

                // Creating our own gameObect, helps finding the UI in ModTools
                _gameObject = new GameObject("BuildingThemes");
                _gameObject.transform.parent = UIView.GetAView().transform;
                _instance = _gameObject.AddComponent<GUI.UIThemeManager>();
            }
            catch (Exception e)
            {
                // Catching any exception to not block the loading process of other mods
                Debug.LogError("Building Themes: An error has happened during the UI creation.");
                Debug.LogException(e);
            }
        }

        public static void Destroy()
        {
            try
            {
                if (_gameObject != null)
                    GameObject.Destroy(_gameObject);
                _instance = null;
                _gameObject = null;
            }
            catch (Exception e)
            {
                // Catching any exception to not block the unloading process of other mods
                Debug.LogError("Building Themes: An error has happened during the UI destruction.");
                Debug.LogException(e);
            }
        }

        public void Toggle()
        {
            if (isVisible)
            {
                Debugger.LogFormat("ThemeManager closed.");
                Hide();
            }
            else
            {
                Debugger.LogFormat("ThemeManager opened.");
                Show(true);

                if (m_themeSelection.selectedIndex == -1) m_themeSelection.selectedIndex = 0;
            }
        }

        public void CreateTheme(string themeName)
        {
            if (BuildingThemesManager.instance.GetThemeByName(themeName) != null) return;
            var newTheme = new Configuration.Theme()
            {
                name = themeName
            };

            BuildingThemesManager.instance.Configuration.themes.Add(newTheme);
            m_isDistrictThemesDirty = true;

            InitBuildingLists();

            m_themeSelection.selectedIndex = -1;
            m_themeSelection.rowsData.m_buffer = m_themes.Keys.ToArray();
            m_themeSelection.rowsData.m_size = m_themeSelection.rowsData.m_buffer.Length;

            for (int i = 0; i < m_themeSelection.rowsData.m_buffer.Length; i++)
            {
                if (m_themeSelection.rowsData.m_buffer[i] == newTheme)
                {
                    m_themeSelection.DisplayAt(i);
                    m_themeSelection.selectedIndex = i;
                }
            }

            ThemePolicyTab.RefreshThemesContainer();
        }

        public void DeleteTheme(Configuration.Theme theme)
        {
            if (!theme.isBuiltIn)
            {
                BuildingThemesManager.instance.Configuration.themes.Remove(theme);
                m_isDistrictThemesDirty = true;

                InitBuildingLists();

                m_themeSelection.selectedIndex = -1;
                m_themeSelection.rowsData.m_buffer = m_themes.Keys.ToArray();
                m_themeSelection.rowsData.m_size = m_themeSelection.rowsData.m_buffer.Length;
                m_themeSelection.DisplayAt(0);
                m_themeSelection.selectedIndex = 0;

                ThemePolicyTab.RefreshThemesContainer();
            }
        }

        public void CloneBuilding(BuildingItem item, string cloneName, int level)
        {
            Configuration.Theme theme = selectedTheme;

            if (!theme.containsBuilding(cloneName))
            {
                Configuration.Building clone = new Configuration.Building(cloneName);
                clone.baseName = item.isCloned ? item.building.baseName : item.name;
                clone.level = level;

                selectedTheme.buildings.Add(clone);
                m_isDistrictThemesDirty = true;

                // Refresh building list
                List<BuildingItem> list = GetBuildingItemList(theme);
                m_themes[theme] = list;

                m_buildingSelection.selectedIndex = -1;
                m_buildingSelection.rowsData = Filter(list);

                // Select cloned item if displayed
                for (int i = 0; i < m_buildingSelection.rowsData.m_size; i++)
                {
                    BuildingItem buildingItem = m_buildingSelection.rowsData.m_buffer[i] as BuildingItem;
                    if (buildingItem.building == clone)
                    {
                        m_buildingSelection.selectedIndex = i;
                        m_buildingSelection.DisplayAt(i);
                        UpdateBuildingInfo(list[i]);
                        break;
                    }
                }
            }
        }

        public void ChangeBuildingStatus(BuildingItem item, bool include)
        {
            if (include == item.included) return;

            CreateBuilding(item);
            item.building.include = include;

            m_isDistrictThemesDirty = true;

            m_themeSelection.Refresh();
            m_buildingSelection.Refresh();
        }

        public void ChangeUpgradeBuilding(BuildingItem building)
        {
            CreateBuilding(selectedBuilding);
            if (building == null)
                selectedBuilding.building.upgradeName = null;
            else
                selectedBuilding.building.upgradeName = building.name;

            m_isDistrictThemesDirty = true;
        }

        public void ChangeSpawnRate(int spawnRate)
        {
            CreateBuilding(selectedBuilding);

            spawnRate = Mathf.Clamp(spawnRate, 0, 100);
            if (selectedBuilding.building.spawnRate != spawnRate)
            {
                selectedBuilding.building.spawnRate = spawnRate;
                m_isDistrictThemesDirty = true;
            }
        }

        private void CreateBuilding(BuildingItem item)
        {
            if (item.building != null) return;

            Configuration.Building building = new Configuration.Building(item.name);
            building.baseName = BuildingVariationManager.instance.GetBasePrefabName(item.name);
            building.include = false;

            if (!selectedTheme.containsBuilding(building.name))
            {
                selectedTheme.buildings.Add(building);
                item.building = building;
            }
        }

        public void UpdateBuildingInfo(BuildingItem item)
        {
            m_buildingPreview.Show(item);
            m_buildingOptions.Show(item);
        }

        public BuildingItem GetBuildingItem(string name)
        {
            List<BuildingItem> list = m_themes[selectedTheme];
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].name == name) return list[i];
            }

            return null;
        }

        /// <summary>Returns all BuildingItems for the given theme (used by UIWorkshopDependenciesModal).</summary>
        public List<BuildingItem> GetBuildingItems(Configuration.Theme theme)
        {
            if (m_themes.ContainsKey(theme)) return m_themes[theme];
            return new List<BuildingItem>();
        }

        private enum ThemeValidity
        {
            Valid = 0,
            Empty = 1,
            MissingL1 = 2,
            BuildingNotLoaded = 4
        }

        public struct ThemeStats
        {
            public int TotalBuildings;
            public int LoadedBuildings;
            public int MissingBuildings;
            public bool HasLevel1;
        }

        /// <summary>
        /// Returns counts of loaded vs. missing buildings for the given theme.
        /// Only counts buildings that are actually included and expected to be loaded
        /// (skips DLC-locked and environment-filtered entries).
        /// </summary>
        public ThemeStats GetThemeStats(Configuration.Theme theme)
        {
            var stats = new ThemeStats();
            if (!m_themes.ContainsKey(theme))
            {
                InitBuildingLists();
                if (!m_themes.ContainsKey(theme)) return stats;
            }

            List<BuildingItem> list = m_themes[theme];
            foreach (BuildingItem item in list)
            {
                if (!item.included) continue;
                stats.TotalBuildings++;

                if (item.prefab == null)
                {
                    // Don't count as "missing" if excluded by DLC or environment
                    if (item.building != null)
                    {
                        if (item.building.dlc != null && !PlatformService.IsDlcInstalled(Convert.ToUInt32(item.building.dlc))) continue;
                        if (item.building.environments != null
                            && (item.building.environments.Contains("-" + SimulationManager.instance.m_metaData.m_environment)
                                || !item.building.environments.Contains("+" + SimulationManager.instance.m_metaData.m_environment)))
                        {
                            continue;
                        }
                    }
                    stats.MissingBuildings++;
                }
                else
                {
                    stats.LoadedBuildings++;
                    if (item.level == 1) stats.HasLevel1 = true;
                }
            }
            return stats;
        }

        public string ThemeValidityError(Configuration.Theme theme)
        {
            if (!m_themes.ContainsKey(theme))
            {
                InitBuildingLists();
                if (!m_themes.ContainsKey(theme)) return "Theme not found";
            }

            var stats = GetThemeStats(theme);
            ThemeValidity validity = ThemeValidity.Valid;

            if (stats.TotalBuildings == 0) validity |= ThemeValidity.Empty;
            if (!stats.HasLevel1 && stats.TotalBuildings > 0) validity |= ThemeValidity.MissingL1;
            if (stats.MissingBuildings > 0) validity |= ThemeValidity.BuildingNotLoaded;

            if (validity == 0) return null;

            StringBuilder errorMessage = new StringBuilder();
            if ((validity & ThemeValidity.Empty) == ThemeValidity.Empty)
                errorMessage.Append("No building included.\n");
            else if ((validity & ThemeValidity.MissingL1) == ThemeValidity.MissingL1)
                errorMessage.Append("No level 1 building included.\n");
            if ((validity & ThemeValidity.BuildingNotLoaded) == ThemeValidity.BuildingNotLoaded)
                errorMessage.AppendFormat("{0}/{1} buildings loaded.\n",
                    stats.LoadedBuildings, stats.LoadedBuildings + stats.MissingBuildings);
            errorMessage.Length--;

            return errorMessage.ToString();
        }

        public override void Update()
        {
            base.Update();

            if (m_isDistrictThemesDirty)
            {
                ThemePolicyTab.RefreshThemesContainer();
                BuildingThemesManager.instance.RefreshDistrictThemeInfos();
                BuildingThemesManager.instance.SaveConfig();
                m_isDistrictThemesDirty = false;
            }
            if (BuildingVariationManager.Enabled != m_cloneBuilding.isVisible)
            {
                m_cloneBuilding.isVisible = BuildingVariationManager.Enabled;
            }
        }

        public override void Start()
        {
            base.Start();

            try
            {
                backgroundSprite = "UnlockingPanel2";
                isVisible = false;
                canFocus = true;
                isInteractive = true;
                width = SPACING + LEFT_WIDTH + SPACING + MIDDLE_WIDTH + SPACING + RIGHT_WIDTH + SPACING;
                height = TITLE_HEIGHT + HEIGHT + SPACING;
                relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

                InitBuildingLists();
                SetupControls();
            }
            catch (Exception e)
            {
                Debug.LogError("Building Themes: An error has happened during the UI start.");
                Debug.LogException(e);
                Destroy();
            }
        }

        private void SetupControls()
        {
            // Title Bar
            m_title = AddUIComponent<UITitleBar>();
            m_title.title = "Theme Manager";
            m_title.iconSprite = "ToolbarIconZoomOutCity";

            // Filter
            m_filter = AddUIComponent<UIBuildingFilter>();
            m_filter.width = width - SPACING * 2;
            m_filter.height = 105;
            m_filter.relativePosition = new Vector3(SPACING, TITLE_HEIGHT);

            m_filter.eventFilteringChanged += (c, i) =>
            {
                if (m_themeSelection != null && m_themeSelection.selectedIndex != -1)
                {
                    Configuration.Theme theme = m_themeSelection.selectedItem as Configuration.Theme;
                    m_buildingSelection.selectedIndex = -1;
                    m_buildingSelection.rowsData = Filter(m_themes[theme]);
                }
            };

            // Panels
            m_leftPanel = AddUIComponent<UIPanel>();
            m_leftPanel.width = LEFT_WIDTH;
            m_leftPanel.height = HEIGHT - m_filter.height;
            m_leftPanel.relativePosition = new Vector3(SPACING, TITLE_HEIGHT + m_filter.height + SPACING);
            UIPanel left = m_leftPanel;

            m_middlePanel = AddUIComponent<UIPanel>();
            m_middlePanel.width = MIDDLE_WIDTH;
            m_middlePanel.height = HEIGHT - m_filter.height;
            m_middlePanel.relativePosition = new Vector3(LEFT_WIDTH + SPACING * 2, TITLE_HEIGHT + m_filter.height + SPACING);
            UIPanel middle = m_middlePanel;

            m_rightPanel = AddUIComponent<UIPanel>();
            m_rightPanel.width = RIGHT_WIDTH;
            m_rightPanel.height = HEIGHT - m_filter.height;
            m_rightPanel.relativePosition = new Vector3(LEFT_WIDTH + MIDDLE_WIDTH + SPACING * 3, TITLE_HEIGHT + m_filter.height + SPACING);
            UIPanel right = m_rightPanel;

            // Theme selection
            m_themeSelection = UIFastList.Create<UIThemeItem>(left);

            m_themeSelection.backgroundSprite = "UnlockingPanel";
            m_themeSelection.width = left.width;
            m_themeSelection.height = left.height - 80; // 80 = 40 (New/Delete row) + 40 (Dependencies row)
            m_themeSelection.canSelect = true;
            m_themeSelection.rowHeight = 40;
            m_themeSelection.autoHideScrollbar = true;
            m_themeSelection.relativePosition = Vector3.zero;

            m_themeSelection.rowsData.m_buffer = m_themes.Keys.ToArray();
            m_themeSelection.rowsData.m_size = m_themeSelection.rowsData.m_buffer.Length;
            m_themeSelection.DisplayAt(0);

            m_themeSelection.eventSelectedIndexChanged += (c, i) =>
            {
                if (i == -1) return;

                int listCount = m_buildingSelection.rowsData.m_size;
                float pos = m_buildingSelection.listPosition;

                Configuration.Theme theme = m_themeSelection.selectedItem as Configuration.Theme;
                Debugger.LogFormat("ThemeManager: theme selected '{0}'.", theme != null ? theme.name : "null");
                m_buildingSelection.selectedIndex = -1;
                UpdateSourceFilter(m_themes[theme]);
                m_buildingSelection.rowsData = Filter(m_themes[theme]);

                if (m_filter.buildingStatus == Status.All && m_buildingSelection.rowsData.m_size == listCount)
                {
                    m_buildingSelection.DisplayAt(pos);
                }

                m_themeRemove.isEnabled = !((Configuration.Theme)m_themeSelection.selectedItem).isBuiltIn;
            };

            // Add theme
            m_themeAdd = UIUtils.CreateButton(left);
            m_themeAdd.width = (LEFT_WIDTH - SPACING) / 2;
            m_themeAdd.text = "New Theme";
            m_themeAdd.relativePosition = new Vector3(0, m_themeSelection.height + SPACING);

            m_themeAdd.eventClick += (c, p) =>
            {
                UIView.PushModal(UINewThemeModal.instance);
                UINewThemeModal.instance.Show(true);
            };

            // Remove theme
            m_themeRemove = UIUtils.CreateButton(left);
            m_themeRemove.width = (LEFT_WIDTH - SPACING) / 2;
            m_themeRemove.text = "Delete Theme";
            m_themeRemove.isEnabled = false;
            m_themeRemove.relativePosition = new Vector3(LEFT_WIDTH - m_themeRemove.width, m_themeSelection.height + SPACING);

            m_themeRemove.eventClick += (c, p) =>
            {
                ConfirmPanel.ShowModal("Delete Theme", "Are you sure you want to delete '" + selectedTheme.name + "' theme ?",
                    (d, i) => { if (i == 1) DeleteTheme(selectedTheme); });
            };

            // Workshop Dependencies button (full-width, second row below Add/Delete)
            m_dependencies = UIUtils.CreateButton(left);
            m_dependencies.width = LEFT_WIDTH;
            m_dependencies.text = "Workshop Dependencies";
            m_dependencies.tooltip = "List all workshop assets in this theme grouped by Loaded / Missing status";
            m_dependencies.relativePosition = new Vector3(0, m_themeSelection.height + 40 + SPACING);

            m_dependencies.eventClick += (c, p) =>
            {
                if (selectedTheme != null)
                    UIWorkshopDependenciesModal.instance.ShowFor(selectedTheme);
            };

            // Counter label — filtered item stats, shown above the building list
            m_counterLabel = middle.AddUIComponent<UILabel>();
            m_counterLabel.width = middle.width;
            m_counterLabel.height = 18;
            m_counterLabel.textScale = 0.72f;
            m_counterLabel.textColor = new Color32(150, 150, 150, 255);
            m_counterLabel.text = "";
            m_counterLabel.padding = new RectOffset(3, 0, 3, 0);
            m_counterLabel.relativePosition = Vector3.zero;

            // Building selection
            m_buildingSelection = UIFastList.Create<UIBuildingItem>(middle);

            m_buildingSelection.backgroundSprite = "UnlockingPanel";
            m_buildingSelection.width = middle.width;
            m_buildingSelection.height = middle.height - 40 - 18;
            m_buildingSelection.canSelect = true;
            m_buildingSelection.rowHeight = 40;
            m_buildingSelection.autoHideScrollbar = true;
            m_buildingSelection.relativePosition = new Vector3(0, 18);

            m_buildingSelection.rowsData = new FastList<object>();

            m_buildingSelection.eventSelectedIndexChanged += (c, i) =>
            {
                m_cloneBuilding.isEnabled = selectedBuilding != null && selectedBuilding.prefab != null;

                if (selectedBuilding != null && selectedBuilding.isCloned)
                {
                    BuildingItem item = GetBuildingItem(selectedBuilding.building.baseName);
                    m_cloneBuilding.isEnabled = item != null && item.prefab != null;
                }
            };

            m_buildingSelection.eventMouseLeave += (c, p) =>
            {
                UpdateBuildingInfo(selectedBuilding);
            };

            float initialListBottom = 18 + m_buildingSelection.height + SPACING;

            // Right-aligned: Include: [All] [None] [Valid]
            m_includeNone = UIUtils.CreateButton(middle);
            m_includeNone.width = 50;
            m_includeNone.text = "None";
            m_includeNone.tooltip = "Exclude all buildings in the current filtered view from this theme";
            m_includeNone.relativePosition = new Vector3(MIDDLE_WIDTH - m_includeNone.width, initialListBottom);

            m_includeAll = UIUtils.CreateButton(middle);
            m_includeAll.width = 45;
            m_includeAll.text = "All";
            m_includeAll.tooltip = "Include all buildings in the current filtered view in this theme";
            m_includeAll.relativePosition = new Vector3(m_includeNone.relativePosition.x - m_includeAll.width - SPACING, initialListBottom);

            m_includeValid = UIUtils.CreateButton(middle);
            m_includeValid.width = 50;
            m_includeValid.text = "Valid";
            m_includeValid.tooltip = "Include all spawnable buildings in the current filtered view\n(loaded + valid cell dimensions 1–4)";
            m_includeValid.relativePosition = new Vector3(m_includeAll.relativePosition.x - m_includeValid.width - SPACING, initialListBottom);

            m_includeLabel = middle.AddUIComponent<UILabel>();
            m_includeLabel.width = 65;
            m_includeLabel.padding = new RectOffset(0, 0, 8, 0);
            m_includeLabel.textScale = 0.8f;
            m_includeLabel.text = "Include:";
            m_includeLabel.relativePosition = new Vector3(m_includeValid.relativePosition.x - m_includeLabel.width - SPACING, initialListBottom);

            // Left-aligned: Exclude: [Missing]
            m_excludeLabel = middle.AddUIComponent<UILabel>();
            m_excludeLabel.width = 60;
            m_excludeLabel.padding = new RectOffset(0, 0, 8, 0);
            m_excludeLabel.textScale = 0.8f;
            m_excludeLabel.text = "Exclude:";
            m_excludeLabel.relativePosition = new Vector3(0, initialListBottom);

            m_excludeMissing = UIUtils.CreateButton(middle);
            m_excludeMissing.width = 70;
            m_excludeMissing.text = "Missing";
            m_excludeMissing.tooltip = "Exclude all missing/unloaded buildings in the current filtered view from this theme";
            m_excludeMissing.relativePosition = new Vector3(m_excludeLabel.width + SPACING, initialListBottom);

            m_includeAll.eventClick += (c, p) =>
            {
                for (int i = 0; i < m_buildingSelection.rowsData.m_size; i++)
                {
                    BuildingItem item = m_buildingSelection.rowsData[i] as BuildingItem;
                    if (item != null) ChangeBuildingStatus(item, true);
                }
                m_buildingSelection.Refresh();
            };

            m_includeNone.eventClick += (c, p) =>
            {
                for (int i = 0; i < m_buildingSelection.rowsData.m_size; i++)
                {
                    BuildingItem item = m_buildingSelection.rowsData[i] as BuildingItem;
                    if (item != null) ChangeBuildingStatus(item, false);
                }
                m_buildingSelection.Refresh();
            };

            m_excludeMissing.eventClick += (c, p) =>
            {
                int count = 0;
                for (int i = 0; i < m_buildingSelection.rowsData.m_size; i++)
                {
                    BuildingItem item = m_buildingSelection.rowsData[i] as BuildingItem;
                    if (item != null && item.prefab == null && item.included) count++;
                }
                if (count == 0) return;
                ConfirmPanel.ShowModal("Exclude Missing",
                    string.Format("Exclude {0} missing building(s) in the current filtered view from this theme?", count),
                    (d, result) =>
                    {
                        if (result != 1) return;
                        for (int i = 0; i < m_buildingSelection.rowsData.m_size; i++)
                        {
                            BuildingItem item = m_buildingSelection.rowsData[i] as BuildingItem;
                            if (item != null && item.prefab == null && item.included)
                                ChangeBuildingStatus(item, false);
                        }
                        m_buildingSelection.Refresh();
                    });
            };

            m_includeValid.eventClick += (c, p) =>
            {
                int count = 0;
                for (int i = 0; i < m_buildingSelection.rowsData.m_size; i++)
                {
                    BuildingItem item = m_buildingSelection.rowsData[i] as BuildingItem;
                    if (item != null && item.canSpawn && !item.included) count++;
                }
                if (count == 0) return;
                ConfirmPanel.ShowModal("Include Valid",
                    string.Format("Include {0} spawnable building(s) from the current filtered view in this theme?", count),
                    (d, result) =>
                    {
                        if (result != 1) return;
                        for (int i = 0; i < m_buildingSelection.rowsData.m_size; i++)
                        {
                            BuildingItem item = m_buildingSelection.rowsData[i] as BuildingItem;
                            if (item != null && item.canSpawn && !item.included)
                                ChangeBuildingStatus(item, true);
                        }
                        m_buildingSelection.Refresh();
                    });
            };

            // Preview
            m_buildingPreview = right.AddUIComponent<UIBuildingPreview>();
            m_buildingPreview.width = right.width;
            m_buildingPreview.height = (right.height - SPACING) / 2;
            m_buildingPreview.relativePosition = Vector3.zero;

            // Building Options
            m_buildingOptions = right.AddUIComponent<UIBuildingOptions>();
            m_buildingOptions.width = RIGHT_WIDTH;
            m_buildingOptions.height = (right.height - SPACING) / 2 - 40;
            m_buildingOptions.relativePosition = new Vector3(0, m_buildingPreview.height + SPACING);

            // Clone building
            m_cloneBuilding = UIUtils.CreateButton(right);
            m_cloneBuilding.width = RIGHT_WIDTH;
            m_cloneBuilding.height = 30;
            m_cloneBuilding.text = "Clone building";
            m_cloneBuilding.isEnabled = false;
            m_cloneBuilding.relativePosition = new Vector3(0, m_buildingOptions.relativePosition.y + m_buildingOptions.height + SPACING);

            m_cloneBuilding.eventClick += (c, p) =>
            {
                UIView.PushModal(UICloneBuildingModal.instance);
                UICloneBuildingModal.instance.Show(true);
            };

            // Pre-create the workshop dependencies modal so its Start() runs before the first user click
            UIWorkshopDependenciesModal.instance.Hide();

            // Resize handle — draggable corner at bottom-right
            m_resizeHandle = AddUIComponent<UISprite>();
            m_resizeHandle.size = new Vector2(16, 16);
            m_resizeHandle.spriteName = "buttonresize";
            m_resizeHandle.relativePosition = new Vector3(width - 16, height - 16);
            m_resizeHandle.canFocus = true;
            m_resizeHandle.isInteractive = true;
            m_resizeHandle.tooltip = "Drag to resize";
            m_resizeHandle.BringToFront();

            m_resizeHandle.eventMouseDown += (c, p) =>
            {
                m_resizing = true;
                m_resizeStartMouse = Input.mousePosition;
                m_resizeStartSize = new Vector2(width, height);
                p.Use();
            };

            m_resizeHandle.eventMouseMove += (c, p) =>
            {
                if (!m_resizing || p.buttons != UIMouseButton.Left) return;
                Vector3 delta = Input.mousePosition - m_resizeStartMouse;
                width = Mathf.Max(MIN_WIDTH, m_resizeStartSize.x + delta.x);
                height = Mathf.Max(MIN_HEIGHT, m_resizeStartSize.y - delta.y); // screen Y is inverted
                p.Use();
            };

            m_resizeHandle.eventMouseUp += (c, p) =>
            {
                m_resizing = false;
            };
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            if (m_leftPanel == null) return;
            ResizeUI();
        }

        private void ResizeUI()
        {
            float currentMiddleWidth = width - LEFT_WIDTH - RIGHT_WIDTH - SPACING * 4;
            float panelHeight = height - TITLE_HEIGHT - m_filter.height - SPACING;

            // Title bar and filter bar
            m_title.width = width;
            m_filter.width = width - SPACING * 2;

            // Left panel — width stays fixed, height grows
            m_leftPanel.height = panelHeight;
            m_themeSelection.height = panelHeight - 80;
            m_themeAdd.relativePosition = new Vector3(0, m_themeSelection.height + SPACING);
            m_themeRemove.relativePosition = new Vector3(LEFT_WIDTH - m_themeRemove.width, m_themeSelection.height + SPACING);
            m_dependencies.relativePosition = new Vector3(0, m_themeSelection.height + 40 + SPACING);

            // Middle panel — both width and height grow
            m_middlePanel.width = currentMiddleWidth;
            m_middlePanel.height = panelHeight;
            m_middlePanel.relativePosition = new Vector3(LEFT_WIDTH + SPACING * 2, TITLE_HEIGHT + m_filter.height + SPACING);
            m_counterLabel.width = currentMiddleWidth;
            m_counterLabel.relativePosition = Vector3.zero;
            m_buildingSelection.width = currentMiddleWidth;
            m_buildingSelection.height = panelHeight - 40 - 18;
            m_buildingSelection.relativePosition = new Vector3(0, 18);
            float listBottom = 18 + m_buildingSelection.height + SPACING;
            // Right-aligned: Include: [All] [None] [Valid]
            m_includeNone.relativePosition = new Vector3(currentMiddleWidth - m_includeNone.width, listBottom);
            m_includeAll.relativePosition = new Vector3(m_includeNone.relativePosition.x - m_includeAll.width - SPACING, listBottom);
            m_includeValid.relativePosition = new Vector3(m_includeAll.relativePosition.x - m_includeValid.width - SPACING, listBottom);
            m_includeLabel.relativePosition = new Vector3(m_includeValid.relativePosition.x - m_includeLabel.width - SPACING, listBottom);
            // Left-aligned: Exclude: [Missing]
            m_excludeLabel.relativePosition = new Vector3(0, listBottom);
            m_excludeMissing.relativePosition = new Vector3(m_excludeLabel.width + SPACING, listBottom);

            // Right panel — width stays fixed, height grows
            m_rightPanel.height = panelHeight;
            m_rightPanel.relativePosition = new Vector3(LEFT_WIDTH + currentMiddleWidth + SPACING * 3, TITLE_HEIGHT + m_filter.height + SPACING);
            m_buildingPreview.height = (panelHeight - SPACING) / 2;
            m_buildingOptions.height = (panelHeight - SPACING) / 2 - 40;
            m_buildingOptions.relativePosition = new Vector3(0, m_buildingPreview.height + SPACING);
            m_cloneBuilding.relativePosition = new Vector3(0, m_buildingOptions.relativePosition.y + m_buildingOptions.height + SPACING);

            // Resize handle stays at bottom-right corner
            m_resizeHandle.relativePosition = new Vector3(width - 16, height - 16);
        }

        private void InitBuildingLists()
        {
            Configuration.Theme[] themes = BuildingThemesManager.instance.GetAllThemes().ToArray();
            Array.Sort(themes, ThemeCompare);

            m_themes.Clear();
            for (int i = 0; i < themes.Length; i++)
            {
                m_themes.Add(themes[i], GetBuildingItemList(themes[i]));
            }
        }

        private List<BuildingItem> GetBuildingItemList(Configuration.Theme theme)
        {
            List<BuildingItem> list = new List<BuildingItem>();

            // List of all growables prefabs
            Dictionary<string, BuildingItem> buildingDictionary = new Dictionary<string, BuildingItem>();
            uint totalPrefabs = (uint)PrefabCollection<BuildingInfo>.PrefabCount();
            for (uint i = 0; i < totalPrefabs; i++)
            {
                BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetPrefab(i);
                if (prefab != null && prefab.m_placementStyle == ItemClass.Placement.Automatic)
                {
                    BuildingItem item = new BuildingItem();
                    item.prefab = PrefabCollection<BuildingInfo>.GetPrefab(i);
                    if (!buildingDictionary.ContainsKey(item.name)) buildingDictionary.Add(item.name, item);

                    if (!BuildingVariationManager.instance.IsVariation(item.name))
                        list.Add(item);
                }
            }

            // Combine growables with buildings in configuration
            Configuration.Building[] buildings = theme.buildings.ToArray();
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildingDictionary.ContainsKey(buildings[i].name))
                {
                    // Associate building with prefab by exact name
                    BuildingItem item = buildingDictionary[buildings[i].name];
                    item.building = buildings[i];

                    if (!list.Contains(item)) list.Add(item);
                }
                else
                {
                    // Exact name not found — try Steam prefix fallback (handles renamed workshop assets)
                    BuildingItem matched = null;
                    int dotIdx = buildings[i].name != null ? buildings[i].name.IndexOf('.') : -1;
                    if (dotIdx > 0)
                    {
                        string steamPrefix = buildings[i].name.Substring(0, dotIdx);
                        bool allDigits = true;
                        foreach (char ch in steamPrefix) if (!char.IsDigit(ch)) { allDigits = false; break; }
                        if (allDigits)
                        {
                            foreach (var kv in buildingDictionary)
                            {
                                int kDot = kv.Key.IndexOf('.');
                                if (kDot > 0 && kv.Key.Substring(0, kDot) == steamPrefix)
                                {
                                    matched = kv.Value;
                                    break;
                                }
                            }
                        }
                    }

                    if (matched != null)
                    {
                        matched.building = buildings[i];
                        if (!list.Contains(matched)) list.Add(matched);
                    }
                    else
                    {
                        // Prefab not found at all — show as not loaded

                        if (buildings[i].dlc != null && !PlatformService.IsDlcInstalled(Convert.ToUInt32(buildings[i].dlc))) continue;
                        if (buildings[i].environments != null
                            && (buildings[i].environments.Contains("-" + SimulationManager.instance.m_metaData.m_environment)
                            || !buildings[i].environments.Contains("+" + SimulationManager.instance.m_metaData.m_environment)))
                        {
                            continue;
                        }

                        BuildingItem item = new BuildingItem();
                        item.building = buildings[i];
                        list.Add(item);
                    }
                }
            }

            // Sorting
            try
            {
                list.Sort(BuildingCompare);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                // ignore this error. seems harmless
            }
            return list;
        }

        #region Filtering/Sorting
        public FastList<object> GetBuildingsFiltered(Category category, int preferredLevel, Vector2 size, string name)
        {
            List<BuildingItem> list = m_themes[selectedTheme];
            string lowerName = name == null ? "" : name.Trim().ToLower();

            var result = new List<BuildingItem>();
            for (int i = 0; i < list.Count; i++)
            {
                BuildingItem item = list[i];
                if (category != Category.None && item.category != category) continue;
                if (!lowerName.IsNullOrWhiteSpace() && !item.name.ToLower().Contains(lowerName)) continue;
                result.Add(item);
            }

            // Sort: level starting from preferredLevel ascending, then below; within level by size then name
            result.Sort((a, b) =>
            {
                int aLevelOrder = a.level >= preferredLevel ? a.level - preferredLevel : 1000 + (preferredLevel - a.level);
                int bLevelOrder = b.level >= preferredLevel ? b.level - preferredLevel : 1000 + (preferredLevel - b.level);
                int cmp = aLevelOrder.CompareTo(bLevelOrder);
                if (cmp != 0) return cmp;

                cmp = a.size.x.CompareTo(b.size.x);
                if (cmp != 0) return cmp;
                cmp = a.size.y.CompareTo(b.size.y);
                if (cmp != 0) return cmp;

                return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);
            });

            FastList<object> filtered = new FastList<object>();
            foreach (var item in result) filtered.Add(item);
            return filtered;
        }

        private FastList<object> Filter(List<BuildingItem> list)
        {
            List<BuildingItem> filtered = new List<BuildingItem>();
            for (int i = 0; i < list.Count; i++)
            {
                BuildingItem item = (BuildingItem)list[i];

                // DLC filter — uses BuildingInfo.m_requiredExpansion / m_requiredModderPack
                var filterExp  = m_filter.dlcExpansionFilter;
                var filterPack = m_filter.dlcModderPackFilter;
                if (filterExp != SteamHelper.ExpansionBitMask.None || filterPack != SteamHelper.ModderPackBitMask.None)
                {
                    if (item.prefab == null) continue; // unloaded buildings have no mask — skip when filter active
                    bool expMatch  = filterExp  == SteamHelper.ExpansionBitMask.None  || (item.prefab.m_requiredExpansion  & filterExp)  != SteamHelper.ExpansionBitMask.None;
                    bool packMatch = filterPack == SteamHelper.ModderPackBitMask.None || (item.prefab.m_requiredModderPack & filterPack) != SteamHelper.ModderPackBitMask.None;
                    if (!expMatch || !packMatch) continue;
                }

                // Origin
                if (m_filter.buildingOrigin == Origin.Default && item.isCustomAsset) continue;
                if (m_filter.buildingOrigin == Origin.Custom && !item.isCustomAsset) continue;
                if (m_filter.buildingOrigin == Origin.Cloned && !item.isCloned) continue;

                // Status (include/exclude)
                if (m_filter.buildingStatus == Status.Included && !item.included) continue;
                if (m_filter.buildingStatus == Status.Excluded && item.included) continue;

                // Asset loading status toggles
                AssetStatus aStatus = item.assetStatus;
                if (!m_filter.showLoaded && aStatus == AssetStatus.Available) continue;
                if (!m_filter.showMissing && aStatus == AssetStatus.Missing) continue;
                if (!m_filter.showDLCLocked && aStatus == AssetStatus.DLCLocked) continue;

                // Can spawn only
                if (m_filter.canSpawnOnly && !item.canSpawn) continue;

                // Level
                int level = (int)(m_filter.buildingLevel + 1);
                if (m_filter.buildingLevel != ItemClass.Level.None && item.level != level) continue;

                // Size
                Vector2 buildingSize = m_filter.buildingSize;
                if (buildingSize != Vector2.zero && item.size != buildingSize) continue;

                // Zone
                if (!m_filter.IsAllZoneSelected())
                {
                    Category category = item.category;
                    if (category == Category.None || !m_filter.IsZoneSelected(category)) continue;
                }

                // Name / Steam ID search
                if (!m_filter.buildingName.IsNullOrWhiteSpace())
                {
                    string search = m_filter.buildingName.ToLower();
                    bool nameMatch = item.name.ToLower().Contains(search)
                        || item.displayName.ToLower().Contains(search);
                    bool steamMatch = item.steamID != null && item.steamID.Contains(search);
                    if (!nameMatch && !steamMatch) continue;
                }

                filtered.Add(item);
            }

            // Update counter label
            UpdateCounterLabel(filtered);

            FastList<object> fastList = new FastList<object>();
            fastList.m_buffer = filtered.ToArray();
            fastList.m_size = filtered.Count;

            return fastList;
        }

        /// <summary>
        /// Rebuilds the DLC filter dropdown from the loaded prefab masks of the theme's buildings.
        /// </summary>
        private void UpdateSourceFilter(List<BuildingItem> buildings)
        {
            if (m_filter == null) return;
            m_filter.SetDlcOptions(buildings);
        }

        private void UpdateCounterLabel(List<BuildingItem> filtered)
        {
            if (m_counterLabel == null) return;

            int spawnable = 0, missing = 0, dlc = 0;
            for (int i = 0; i < filtered.Count; i++)
            {
                BuildingItem item = filtered[i];
                if (item.canSpawn) spawnable++;
                AssetStatus s = item.assetStatus;
                if (s == AssetStatus.Missing) missing++;
                else if (s == AssetStatus.DLCLocked) dlc++;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("{0} spawnable / {1} shown", spawnable, filtered.Count);
            if (missing > 0) sb.AppendFormat("  |  {0} missing", missing);
            if (dlc > 0) sb.AppendFormat("  |  {0} DLC/env", dlc);
            m_counterLabel.text = sb.ToString();
        }

        private static int ThemeCompare(Configuration.Theme a, Configuration.Theme b)
        {
            // Sort by name
            return a.name.CompareTo(b.name);
        }

        private static int BuildingCompare(BuildingItem a, BuildingItem b)
        {
            // Sort by category > displayName > level > size > name
            int compare = (int)a.category - (int)b.category;
            if (compare == 0) compare = a.displayName.CompareTo(b.displayName);
            if (compare == 0) compare = a.level.CompareTo(b.level);
            if (compare == 0) compare = a.sizeAsString.CompareTo(b.sizeAsString);
            if (compare == 0) compare = a.name.CompareTo(b.name);

            return compare;
        }
        #endregion

        public void Plop(BuildingItem mItem)
        {
            var buildingTool = ToolsModifierControl.SetTool<BuildingTool>();
            {
                buildingTool.m_prefab = mItem.prefab;
                buildingTool.m_relocate = 0;
            }
        }

        public void DestroyAll(BuildingItem m_item)
        {
            var simulationManager = SimulationManager.instance.AddAction(() =>
            {
                var buildings = BuildingManager.instance.m_buildings.m_buffer;
                for (ushort buildingId = 0; buildingId < buildings.Length; buildingId++)
                {
                    var building = buildings[buildingId];
                    if (building.Info == null)
                    {
                        continue;
                    }

                    if (building.Info.name != m_item.name)
                    {
                        continue;
                    }

                    Destroy(buildingId, building);
                }
            });
        }

        //similar to BulldozeTool.DeleteBuildingImpl
        public void Destroy(ushort buildingId, Building building)
        {
            BuildingManager instance = Singleton<BuildingManager>.instance;
            BuildingInfo info = building.Info;
            Vector3 position = building.m_position;
            float angle = building.m_angle;
            int width = building.Width;
            int length = building.Length;
            bool collapsed = (building.m_flags & Building.Flags.Collapsed) != Building.Flags.None;
            instance.ReleaseBuilding(buildingId);
            int publicServiceIndex = ItemClass.GetPublicServiceIndex(info.m_class.m_service);
            if (publicServiceIndex != -1)
            {
                Singleton<CoverageManager>.instance.CoverageUpdated(info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level);
            }
            BuildingTool.DispatchPlacementEffect(info, buildingId, position, angle, width, length, true, collapsed);
        }
    }
}

