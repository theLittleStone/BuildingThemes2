using UnityEngine;
using ColossalFramework.UI;
using ColossalFramework.PlatformServices;

namespace BuildingThemes.GUI
{
    public class UIBuildingItem : UIPanel, IUIFastListRow
    {
        private static readonly Color32 OriginTextColor = new Color32(160, 160, 160, 255);

        private UICheckBox m_name;
        private UISprite m_steamIcon;
        private UISprite m_category;
        private UILabel m_level;
        private UILabel m_size;
        private UILabel m_origin;
        private UIPanel m_background;

        private BuildingItem m_building;
        private bool _displaying;

        public UIPanel background
        {
            get
            {
                if (m_background == null)
                {
                    m_background = AddUIComponent<UIPanel>();
                    m_background.width = width;
                    m_background.height = 40;
                    m_background.relativePosition = Vector2.zero;

                    m_background.zOrder = 0;
                }

                return m_background;
            }
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (m_name == null) return;

            background.width = width;
            m_size.relativePosition = new Vector3(width - 35f, 15);
            m_level.relativePosition = new Vector3(width - 65f, 15);
            m_category.relativePosition = new Vector3(width - 95f, 10);
        }

        protected override void OnMouseEnter(UIMouseEventParameter p)
        {
            base.OnMouseEnter(p);
            if (enabled) UIThemeManager.instance.UpdateBuildingInfo(m_building);
        }

        protected override void OnMouseWheel(UIMouseEventParameter p)
        {
            base.OnMouseWheel(p);
            if (enabled) UIThemeManager.instance.UpdateBuildingInfo(m_building);
        }

        private void SetupControls()
        {
            if (m_name != null) return;

            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = parent.width;
            height = 40;

            m_name = UIUtils.CreateCheckBox(this);
            m_name.width = 20;
            m_name.clipChildren = false;
            m_name.relativePosition = new Vector3(5, 13);
            m_name.label.textScale = 0.8f;

            m_name.eventCheckChanged += (c, state) =>
            {
                if (m_building != null && !_displaying)
                {
                    Debugger.LogFormat("Building include toggled: '{0}' → {1}.", m_building.displayName, state);
                    UIThemeManager.instance.ChangeBuildingStatus(m_building, state);
                }
            };

            m_steamIcon = m_name.AddUIComponent<UISprite>();
            m_steamIcon.spriteName = "SteamWorkshop";
            m_steamIcon.isVisible = false;
            m_steamIcon.relativePosition = new Vector3(22, 0);

            UIUtils.ResizeIcon(m_steamIcon, new Vector2(25, 25));

            if (PlatformService.IsOverlayEnabled())
            {
                m_steamIcon.eventClick += (c, p) =>
                {
                    p.Use();
                    PlatformService.ActivateGameOverlayToWorkshopItem(new PublishedFileId(ulong.Parse(m_building.steamID)));
                };
            }

            m_size = AddUIComponent<UILabel>();
            m_size.width = 30;
            m_size.textAlignment = UIHorizontalAlignment.Center;
            m_size.textScale = 0.75f;

            m_level = AddUIComponent<UILabel>();
            m_level.width = 30;
            m_level.textAlignment = UIHorizontalAlignment.Center;
            m_level.textScale = 0.75f;

            m_category = AddUIComponent<UISprite>();
            m_category.size = new Vector2(20, 20);

            // Secondary line under the building name, in muted grey. Displays where the asset
            // comes from ("Vanilla asset", "Workshop", "Included in <DLC>"). Lower textScale and
            // dimmer colour keep it visually subordinate to the name.
            m_origin = AddUIComponent<UILabel>();
            m_origin.textScale = 0.65f;
            m_origin.textColor = OriginTextColor;
            m_origin.autoSize = false;
            m_origin.height = 14f;
            m_origin.relativePosition = new Vector3(27f, 23f);
            m_origin.isVisible = false;
        }

        #region IUIFastListRow implementation
        public void Display(object data, bool isRowOdd)
        {
            SetupControls();

            float maxLabelWidth = width - 120;

            m_building = data as BuildingItem;
            m_name.text = m_building.displayName;
            if (m_building.prefab == null) m_name.text += " (Not Loaded)";
            m_name.label.textColor = m_building.GetStatusColor();
            m_name.label.isInteractive = false;
            _displaying = true;
            m_name.isChecked = m_building.included;
            _displaying = false;

            bool builtIn = UIThemeManager.instance.selectedTheme?.isBuiltIn ?? false;
            m_name.isEnabled = !builtIn;
            m_name.tooltip = builtIn ? "Built-in themes are read-only.\nUse 'Copy Theme' to create an editable copy." : null;

            m_level.text = m_building.level == 0 ? null : "L" + m_building.level;
            m_size.text = m_building.sizeAsString;

            if (m_building.category != Category.None)
            {
                m_category.atlas = UIUtils.GetAtlas(CategoryIcons.atlases[(int)m_building.category]);
                m_category.spriteName = CategoryIcons.spriteNames[(int)m_building.category];
                m_category.tooltip = CategoryIcons.tooltips[(int)m_building.category];
                m_category.isVisible = true;
            }
            else
                m_category.isVisible = false;

            if(m_building.steamID != null)
            {
                m_steamIcon.tooltip = m_building.steamID;
                m_steamIcon.isVisible = true;

                maxLabelWidth -= 30;

                m_name.label.relativePosition = new Vector3(52, 2);
                m_origin.relativePosition = new Vector3(57f, 23f);
            }
            else
            {
                m_steamIcon.isVisible = false;

                m_name.label.relativePosition = new Vector3(22, 2);
                m_origin.relativePosition = new Vector3(27f, 23f);
            }

            string originText = m_building.GetOriginText();
            m_origin.text = originText;
            m_origin.width = Mathf.Max(10f, maxLabelWidth);
            m_origin.isVisible = !string.IsNullOrEmpty(originText);

            if (isRowOdd)
            {
                background.backgroundSprite = "UnlockingItemBackground";
                background.color = new Color32(0, 0, 0, 128);
            }
            else
            {
                background.backgroundSprite = null;
            }

            UIUtils.TruncateLabel(m_name.label, maxLabelWidth);
        }

        public void Select(bool isRowOdd)
        {
            background.backgroundSprite = "ListItemHighlight";
            background.color = new Color32(255, 255, 255, 255);
        }

        public void Deselect(bool isRowOdd)
        {
            if (m_building == null) return;

            if (isRowOdd)
            {
                background.backgroundSprite = "UnlockingItemBackground";
                background.color = new Color32(0, 0, 0, 128);
            }
            else
            {
                background.backgroundSprite = null;
            }
        }
        #endregion
    }
}
