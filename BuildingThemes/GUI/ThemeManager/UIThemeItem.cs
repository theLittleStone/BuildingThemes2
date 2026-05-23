using UnityEngine;
using ColossalFramework.UI;

namespace BuildingThemes.GUI
{
    public class UIThemeItem : UIPanel, IUIFastListRow
    {
        // Single badge whose colour reflects DLC dependency:
        //   green  - vanilla-only (no DLC, no workshop)
        //   red    - no workshop, but requires DLC
        //   hidden - has at least one workshop asset
        private static readonly Color32 BadgeColorVanilla = new Color32(100, 220, 100, 255);
        private static readonly Color32 BadgeColorDlc     = new Color32(220, 100, 100, 255);
        private const string BadgeTooltipVanilla = "Vanilla-only theme: no DLC or workshop assets required.";
        private const string BadgeTooltipDlc     = "Needs DLC. No workshop subscriptions required.";

        private UILabel m_name;
        private UILabel m_dlcBadge;
        private UIPanel m_background;

        private Configuration.Theme m_theme;

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

        public override void Start()
        {
            base.Start();

            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = parent.width;
            height = 40;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (m_background != null) m_background.width = width;
            if (m_dlcBadge != null) m_dlcBadge.relativePosition = new Vector3(width - 20f, 13f);
            if (m_name != null) m_name.width = Mathf.Max(10f, width - (m_dlcBadge != null && m_dlcBadge.isVisible ? 25f : 10f));
        }

        #region IUIFastListRow implementation
        public void Display(object data, bool isRowOdd)
        {
            if (m_name == null)
            {
                m_name = AddUIComponent<UILabel>();
                m_name.textScale = 0.8f;
                m_name.autoSize = false;
                m_name.height = 20f;
                m_name.relativePosition = new Vector3(5, 13);
            }

            if (m_dlcBadge == null)
            {
                m_dlcBadge = AddUIComponent<UILabel>();
                m_dlcBadge.textScale = 0.8f;
                m_dlcBadge.text = "♦";
                m_dlcBadge.autoSize = true;
                m_dlcBadge.relativePosition = new Vector3(width - 20f, 13f);
            }

            m_theme = data as Configuration.Theme;

            bool isVanilla = m_theme != null && !m_theme.isDlc && m_theme.isVanillaOnly;
            bool needsDlc  = !isVanilla && m_theme != null
                && (m_theme.isDlc || m_theme.hasNoWorkshopAssets);
            if (isVanilla)
            {
                m_dlcBadge.textColor = BadgeColorVanilla;
                m_dlcBadge.tooltip = BadgeTooltipVanilla;
                m_dlcBadge.isVisible = true;
            }
            else if (needsDlc)
            {
                m_dlcBadge.textColor = BadgeColorDlc;
                m_dlcBadge.tooltip = BadgeTooltipDlc;
                m_dlcBadge.isVisible = true;
            }
            else
            {
                m_dlcBadge.isVisible = false;
            }

            // Keep label width in sync with row width (row width may still be 0 before Start(), use parent as fallback)
            float rowWidth = width > 0 ? width : (parent != null ? parent.width : 0f);
            m_name.width = Mathf.Max(10f, rowWidth - (m_dlcBadge.isVisible ? 25f : 10f));

            string validityError = UIThemeManager.instance != null
                ? UIThemeManager.instance.ThemeValidityError(m_theme)
                : null;
            UIThemeManager.ThemeStats stats = default(UIThemeManager.ThemeStats);

            if (validityError != null)
            {
                // Append X/Y count badge to the displayed name
                stats = UIThemeManager.instance.GetThemeStats(m_theme);
                m_name.text = string.Format("{0} ({1}/{2})", m_theme.displayName,
                    stats.LoadedBuildings, stats.LoadedBuildings + stats.MissingBuildings);
            }
            else
            {
                m_name.text = m_theme.displayName;
            }

            m_name.textColor = (validityError == null) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 0, 255);
            tooltip = validityError;

            // Build tooltip — append mode note when there are missing buildings and mode ≠ Skip.
            string modeNote = null;
            if (validityError != null && stats.MissingBuildings > 0
                && BuildingThemesManager.MissingAssetBehavior != MissingAssetMode.Skip)
            {
                modeNote = BuildingThemesManager.MissingAssetBehavior == MissingAssetMode.FillWithVanilla
                    ? stats.MissingBuildings + " missing: supplemented with vanilla."
                    : stats.MissingBuildings + " missing: affected buckets fall back to vanilla.";
            }
            m_name.tooltip = validityError == null
                ? m_theme.displayName
                : m_theme.displayName + "\n" + validityError + (modeNote != null ? "\n" + modeNote : "");

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

        public void Select(bool isRowOdd)
        {
            background.backgroundSprite = "ListItemHighlight";
            background.color = new Color32(255, 255, 255, 255);
        }

        public void Deselect(bool isRowOdd)
        {
            if (m_theme == null) return;

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
