using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BuildingThemes.GUI
{
    public class UIBuildingOptions : UIPanel
    {
        private UILabel m_noOption;

        public UIButton m_plop;
        public UIButton m_destroy;

        private UICheckBox m_include;
        private UITextField m_spawnRate;
        private UICheckBox m_markCorner;
        private bool m_updatingCorner;  // suppress the corner checkbox event during Show()

        private const string CornerHelpTooltip =
            "Also offer this building for corner lots (intersections).\n\n" +
            "Use this for straight buildings that should fill corners. The building keeps\n" +
            "spawning on normal lots too. Note: a straight building can't physically wrap the\n" +
            "corner like a purpose-built corner asset — it fronts the main street and its side\n" +
            "wall faces the cross street.\n\n" +
            "True corner assets (shown as 'Corner (left/right)' in the preview) already fill\n" +
            "corners, so this option has no effect on them.";

        private UILabel m_assetIdLabel;
        private UITextField m_assetName;

        private UITextField m_baseName;
        private UITextField m_upgradeName;

        private BuildingItem m_item;
        private BuildingItem m_upgradeBuilding;
        private BuildingItem m_baseBuilding;
        private UIFastList m_dropDownList;

        private static UIBuildingOptions _instance;

        public static UIBuildingOptions instance
        {
            get { return _instance; }
        }

        public override void Start()
        {
            base.Start();

            _instance = this;

            isVisible = true;
            canFocus = true;
            isInteractive = true;
            backgroundSprite = "UnlockingPanel";
            padding = new RectOffset(5, 5, 5, 0);

            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding.top = 5;

            SetupControls();
        }

        private void SetupControls()
        {
            if (m_noOption != null) return;

            // No option available
            m_noOption = AddUIComponent<UILabel>();
            m_noOption.textScale = 0.8f;
            m_noOption.text = "No option available";

            // Include
            m_include = UIUtils.CreateCheckBox(this);
            m_include.text = "Include";
            m_include.label.textScale = 0.8f;
            m_include.isVisible = false;

            m_include.eventCheckChanged += (c, state) =>
            {
                UIThemeManager.instance.ChangeBuildingStatus(m_item, state);
                Show(m_item);
            };

            // Spawn rate
            UIPanel spawnRatePanel = AddUIComponent<UIPanel>();
            spawnRatePanel.height = 25;
            spawnRatePanel.isVisible = false;

            UILabel spawnRateLabel = spawnRatePanel.AddUIComponent<UILabel>();
            spawnRateLabel.textScale = 0.8f;
            spawnRateLabel.text = "Spawn weight (1\u2013100):";
            spawnRateLabel.relativePosition = new Vector3(0, 5);

            m_spawnRate = UIUtils.CreateTextField(spawnRatePanel);
            m_spawnRate.size = new Vector2(60, 25);
            m_spawnRate.padding = new RectOffset(6, 6, 6, 0);
            m_spawnRate.tooltip =
                "Relative spawn weight compared to other buildings of the same zone, level, and size.\n\n" +
                "Example: two buildings at weight 10 each spawn 50 % of the time.\n" +
                "A building at weight 20 is twice as likely as one at weight 10 in the same slot.\n\n" +
                "A building alone in its slot always spawns (100 %), regardless of its weight.\n\n" +
                "Min: 1  |  Default: 10  |  Max: 100";
            m_spawnRate.relativePosition = new Vector3(width - 70, 0);

            m_spawnRate.eventTextSubmitted += (c, s) =>
            {
                if (m_item == null) return;
                int spawnRate;
                int.TryParse(m_spawnRate.text, out spawnRate); // 0 on empty/invalid → clamped to 1
                UIThemeManager.instance.ChangeSpawnRate(spawnRate); // also calls CreateBuilding
                m_spawnRate.text = m_item.building.spawnRate.ToString();
            };

            // Mark as corner — lets a straight-zoned building also fill corner lots
            m_markCorner = UIUtils.CreateCheckBox(this);
            m_markCorner.text = "Use on corner lots";
            m_markCorner.label.textScale = 0.8f;
            m_markCorner.isVisible = false;
            m_markCorner.tooltip = CornerHelpTooltip;
            m_markCorner.eventCheckChanged += (c, state) =>
            {
                if (m_item == null || m_updatingCorner) return;  // ignore programmatic sets in Show()
                UIThemeManager.instance.ChangeMarkAsCorner(state);
            };

            // Upgrade Name
            UIPanel upgradeNamePanel = AddUIComponent<UIPanel>();
            upgradeNamePanel.height = 50;
            upgradeNamePanel.isVisible = false;

            UILabel upgradeNameLabel = upgradeNamePanel.AddUIComponent<UILabel>();
            upgradeNameLabel.textScale = 0.8f;
            upgradeNameLabel.text = "Upgrade:";
            upgradeNameLabel.relativePosition = new Vector3(0, 5);

            m_upgradeName = UIUtils.CreateTextField(upgradeNamePanel);
            m_upgradeName.size = new Vector2(width - 10, 25);
            m_upgradeName.padding = new RectOffset(6, 6, 6, 0);
            m_upgradeName.tooltip = "Name of the building to spawn when upgraded.\nLeave empty for random spawn.";
            m_upgradeName.relativePosition = new Vector3(0, 25);

            m_upgradeName.eventMouseEnter += (c, p) =>
            {
                if (!m_upgradeName.hasFocus && m_upgradeBuilding != null)
                    UIThemeManager.instance.buildingPreview.Show(m_upgradeBuilding);
            };

            m_upgradeName.eventMouseLeave += (c, p) =>
            {
                UIThemeManager.instance.buildingPreview.Show(m_item);
            };

            m_upgradeName.eventEnterFocus += (c, p) =>
            {
                // Always show candidates when the field is focused, even if empty
                ShowDropDown();
            };

            m_upgradeName.eventTextChanged += (c, name) =>
            {
                if (m_upgradeName.hasFocus)
                    ShowDropDown();
            };

            m_upgradeName.eventTextSubmitted += (c, name) =>
            {
                if (m_dropDownList == null || !m_dropDownList.isVisible)
                    UIThemeManager.instance.ChangeUpgradeBuilding(null);
                else
                    HideDropDown();

                Show(m_item);
            };

            // Asset name (always shown, read-only)
            UIPanel assetNamePanel = AddUIComponent<UIPanel>();
            assetNamePanel.height = 50;
            assetNamePanel.isVisible = false;

            m_assetIdLabel = assetNamePanel.AddUIComponent<UILabel>();
            m_assetIdLabel.textScale = 0.8f;
            m_assetIdLabel.text = "Asset name:";
            m_assetIdLabel.relativePosition = new Vector3(0, 5);

            m_assetName = UIUtils.CreateTextField(assetNamePanel);
            m_assetName.size = new Vector2(width - 10, 25);
            m_assetName.padding = new RectOffset(6, 6, 6, 0);
            m_assetName.isEnabled = true;
            m_assetName.tooltip = "Full internal prefab name. For workshop assets this includes the Steam Workshop ID.\nClick to select and copy.";
            m_assetName.relativePosition = new Vector3(0, 25);

            // Keep field read-only: restore original name on any edit or focus loss
            m_assetName.eventTextChanged += (c, val) =>
            {
                if (m_item != null && val != m_item.name) m_assetName.text = m_item.name;
            };
            m_assetName.eventLostFocus += (c, p) =>
            {
                if (m_item != null) m_assetName.text = m_item.name;
            };

            var constructionPanel = AddUIComponent<UIPanel>();
            constructionPanel.height = 30;
            constructionPanel.isVisible = true;

            
            m_plop = UIUtils.CreateButton(constructionPanel);
            m_plop.width = 60;
            m_plop.text = "Plop";
            m_plop.relativePosition = new Vector3(0, 0); ;

            m_plop.eventClick += (c, p) =>
            {
                UIThemeManager.instance.Plop(m_item);
            };

            m_destroy = UIUtils.CreateButton(constructionPanel);
            m_destroy.width = 120;
            m_destroy.text = "Bulldoze All";
            m_destroy.relativePosition = new Vector3(m_plop.width + 10f, 0);
            m_destroy.eventClick += (c, p) =>
            {
                UIThemeManager.instance.DestroyAll(m_item);
            };

            // Base Name
            UIPanel baseNamePanel = AddUIComponent<UIPanel>();
            baseNamePanel.height = 50;
            baseNamePanel.isVisible = false;

            UILabel baseNameLabel = baseNamePanel.AddUIComponent<UILabel>();
            baseNameLabel.textScale = 0.8f;
            baseNameLabel.text = "Base:";
            baseNameLabel.relativePosition = new Vector3(0, 5);

            m_baseName = UIUtils.CreateTextField(baseNamePanel);
            m_baseName.size = new Vector2(width - 10, 25);
            m_baseName.padding = new RectOffset(6, 6, 6, 0);
            m_baseName.isEnabled = false;
            m_baseName.tooltip = "Name of the original building.";
            m_baseName.relativePosition = new Vector3(0, 25);

            m_baseName.eventMouseEnter += (c, p) => UIThemeManager.instance.buildingPreview.Show(m_baseBuilding);
            m_baseName.eventMouseLeave += (c, p) => UIThemeManager.instance.buildingPreview.Show(m_item);
        }

        public override void Update()
        {
            base.Update();

            if (m_dropDownList == null || !m_dropDownList.isVisible) return;

            if (!m_upgradeName.hasFocus)
                HideDropDown();

            if (Input.GetKeyUp(KeyCode.DownArrow))
            {
                m_dropDownList.selectedIndex = Mathf.Clamp(m_dropDownList.selectedIndex + 1, 0, m_dropDownList.rowsData.m_size);

                float max = m_dropDownList.listPosition + m_dropDownList.height / 30;

                if(m_dropDownList.selectedIndex >= max)
                {
                    m_dropDownList.DisplayAt(m_dropDownList.listPosition + 1f);
                }
            }
            if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                m_dropDownList.selectedIndex = Mathf.Clamp(m_dropDownList.selectedIndex - 1, 0, m_dropDownList.rowsData.m_size);

                float min = m_dropDownList.listPosition;

                if (m_dropDownList.selectedIndex < min)
                {
                    m_dropDownList.DisplayAt(m_dropDownList.listPosition - 1f);
                }
            }
            if (Input.GetKeyUp(KeyCode.Return))
            {
                UIThemeManager.instance.ChangeUpgradeBuilding(m_dropDownList.selectedItem as BuildingItem);
                HideDropDown();
                Show(m_item);
            }
            if (Input.GetKeyUp(KeyCode.Backspace) || Input.GetKeyUp(KeyCode.Delete))
            {
                if (m_upgradeName.text.IsNullOrWhiteSpace()) HideDropDown();
            }
        }

        public void Show(BuildingItem item)
        {
            m_item = item;

            m_noOption.isVisible = false;
            m_include.isVisible = false;
            m_spawnRate.parent.isVisible = false;
            m_markCorner.isVisible = false;
            m_upgradeName.parent.isVisible = false;
            m_baseName.parent.isVisible = false;
            m_assetName.parent.isVisible = false;
            m_plop.isVisible = false;
            m_destroy.isVisible = false;

            if (m_item == null)
            {
                m_noOption.isVisible = true;
                return;
            }

            // Always show the raw asset name
            m_assetName.text = m_item.name;
            if (m_item.steamID != null)
                m_assetIdLabel.text = "Asset name (Workshop " + m_item.steamID + "):";
            else
                m_assetIdLabel.text = "Asset name:";
            m_assetName.parent.isVisible = true;

            bool builtIn = UIThemeManager.instance.selectedTheme?.isBuiltIn ?? false;
            string readOnlyTip = builtIn ? "Built-in themes are read-only.\nUse 'Copy Theme' to create an editable copy." : null;

            m_include.isVisible = true;
            m_include.isChecked = m_item.included;
            m_include.isEnabled = !builtIn;
            m_include.tooltip = readOnlyTip;

            m_spawnRate.text = "10";
            m_spawnRate.parent.isVisible = true;
            m_spawnRate.isEnabled = !builtIn;
            m_spawnRate.tooltip = builtIn ? readOnlyTip : null;

            // "Use on corner lots" — only offered for buildings that could plausibly work as a
            // corner. Requirements, checked against the prefab/mesh:
            //   • loaded prefab (unloaded assets have no zoning/mesh info),
            //   • straight-zoned (true corner assets already fill corners — no checkbox needed),
            //   • wall-to-wall by mesh analysis (flat party walls on the sides). A detached
            //     building with garden setbacks can't sit flush in a corner block, so offering
            //     the option for it makes no sense.
            bool cornerEligible = m_item.prefab != null
                && m_item.prefab.m_zoningMode == BuildingInfo.ZoningMode.Straight
                && m_item.isWallToWall;
            m_markCorner.isVisible = cornerEligible;
            if (cornerEligible)
            {
                m_updatingCorner = true;
                m_markCorner.isChecked = m_item.building != null && m_item.building.markAsCorner;
                m_updatingCorner = false;
                m_markCorner.isEnabled = !builtIn;
                m_markCorner.tooltip = builtIn ? readOnlyTip : CornerHelpTooltip;
            }

            m_upgradeName.text = "";
            m_upgradeBuilding = null;
            m_upgradeName.parent.isVisible = m_item.level < m_item.maxLevel;
            m_upgradeName.isEnabled = !builtIn;
            m_upgradeName.tooltip = builtIn ? readOnlyTip : "Name of the building to spawn when upgraded.\nLeave empty for random spawn.";

            if (m_item.building != null)
            {
                m_spawnRate.text = m_item.building.spawnRate.ToString();

                if (m_item.building.upgradeName != null && m_item.level < m_item.maxLevel)
                {
                    m_upgradeBuilding = UIThemeManager.instance.GetBuildingItem(m_item.building.upgradeName);
                    if (m_upgradeBuilding != null) m_upgradeName.text = m_upgradeBuilding.displayName;
                }

                if (m_item.isCloned)
                {
                    m_baseBuilding = UIThemeManager.instance.GetBuildingItem(m_item.building.baseName);
                    if (m_baseBuilding != null) m_baseName.text = m_baseBuilding.displayName;
                    m_baseName.parent.isVisible = true;
                }
            }

            if (m_item.prefab != null)
            {
                m_plop.isVisible = true;
                m_destroy.isVisible = true;
            }
        }

        public void ShowDropDown()
        {
            Category category = m_item.category;
            if (category == Category.None && m_item.isCloned)
            {
                BuildingItem item = UIThemeManager.instance.GetBuildingItem(m_item.building.baseName);
                if (item != null) category = item.category;
            }

            FastList<object> list = UIThemeManager.instance.GetBuildingsFiltered(category, m_item.level + 1, m_item.size, m_upgradeName.text);

            if (m_dropDownList == null)
            {
                m_dropDownList = UIFastList.Create<UIDropDownItem>(GetRootContainer());
                m_dropDownList.width = m_upgradeName.width;
                m_dropDownList.rowHeight = 30;
                m_dropDownList.autoHideScrollbar = true;
                m_dropDownList.canSelect = true;
                m_dropDownList.selectOnMouseEnter = true;
                m_dropDownList.canFocus = true;
                m_dropDownList.backgroundSprite = "GenericPanelLight";
                m_dropDownList.backgroundColor = new Color32(45, 52, 61, 255);
                m_dropDownList.absolutePosition = m_upgradeName.absolutePosition + new Vector3(0, m_upgradeName.height);
            }

            m_dropDownList.height = Mathf.Min(list.m_size * 30, 150);
            m_dropDownList.rowsData = list;
            m_dropDownList.isVisible = list.m_size > 0;
            if (m_dropDownList.isVisible)
                m_dropDownList.selectedIndex = 0;
            else
                m_dropDownList.selectedIndex = -1;
        }

        public void HideDropDown()
        {
            if (m_dropDownList != null)
            {
                m_dropDownList.isVisible = false;
                m_dropDownList.selectedIndex = -1;
            }
        }
    }

    public class UIDropDownItem: UIPanel, IUIFastListRow
    {
        private UILabel m_name;
        private UILabel m_badge; // combined "L2 2x2"

        private BuildingItem m_building;

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (m_name == null) return;

            m_badge.relativePosition = new Vector3(width - 60f, 5);
        }

        private void SetupControls()
        {
            if (m_name != null) return;

            isVisible = true;
            isInteractive = true;
            width = parent.width;
            height = 30;

            m_name = AddUIComponent<UILabel>();
            m_name.relativePosition = new Vector3(5, 5);
            m_name.textScale = 0.8f;
            m_name.textColor = new Color32(170, 170, 170, 255);

            m_badge = AddUIComponent<UILabel>();
            m_badge.width = 55;
            m_badge.textAlignment = UIHorizontalAlignment.Right;
            m_badge.textScale = 0.75f;
            m_badge.textColor = new Color32(120, 120, 120, 255);
            m_badge.relativePosition = new Vector3(width - 60f, 5);
        }

        protected override void OnMouseDown(UIMouseEventParameter p)
        {
            p.Use();
            UIThemeManager.instance.ChangeUpgradeBuilding(m_building);

            base.OnMouseDown(p);
        }

        protected override void OnMouseEnter(UIMouseEventParameter p)
        {
            base.OnMouseEnter(p);
            UIThemeManager.instance.buildingPreview.Show(m_building);
        }


        protected override void OnMouseLeave(UIMouseEventParameter p)
        {
            base.OnMouseLeave(p);
            UIThemeManager.instance.buildingPreview.Show(UIThemeManager.instance.selectedBuilding);
        }

        #region IUIFastListRow implementation
        public void Display(object data, bool isRowOdd)
        {
            SetupControls();

            m_building = data as BuildingItem;
            m_name.text = m_building.name;

            // Level + size badge: "L2 2x2"
            string levelStr = m_building.level > 0 ? "L" + m_building.level : "";
            string sizeStr  = m_building.sizeAsString;
            m_badge.text = (levelStr + " " + sizeStr).Trim();

            // Tooltip: show workshop ID explicitly when available
            tooltip = m_building.steamID != null
                ? "Workshop ID: " + m_building.steamID
                : null;

            UIUtils.TruncateLabel(m_name, width - 65);

            backgroundSprite = null;
        }

        public void Select(bool isRowOdd)
        {
            backgroundSprite = "ListItemHighlight";
            color = new Color32(255, 255, 255, 255);
        }

        public void Deselect(bool isRowOdd)
        {
            backgroundSprite = null;
        }
        #endregion
    }

}
