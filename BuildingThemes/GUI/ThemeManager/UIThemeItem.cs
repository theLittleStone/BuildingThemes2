using UnityEngine;
using ColossalFramework.UI;

namespace BuildingThemes.GUI
{
    public class UIThemeItem : UIPanel, IUIFastListRow
    {
        private UILabel m_name;
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
            if (m_name != null) m_name.width = Mathf.Max(10f, width - 10f);
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

            // Keep label width in sync with row width (row width may still be 0 before Start(), use parent as fallback)
            float rowWidth = width > 0 ? width : (parent != null ? parent.width : 0f);
            m_name.width = Mathf.Max(10f, rowWidth - 10f);

            m_theme = data as Configuration.Theme;

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
