using BuildingThemes.Diagnostics;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BuildingThemes.GUI
{
    /// <summary>
    /// Modal that shows the ThemeDiagnostics report for the currently selected district.
    /// Opened via the "Diagnostics" button in ThemePolicyTab.
    /// </summary>
    public class UIThemeDiagnosticsModal : UIPanel
    {
        private UITitleBar m_title;
        private UILabel m_text;
        private UIScrollablePanel m_scroll;
        private UIButton m_ok;
        private UIButton m_copy;

        private static UIThemeDiagnosticsModal _instance;

        public static UIThemeDiagnosticsModal instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UIView.GetAView().AddUIComponent(typeof(UIThemeDiagnosticsModal)) as UIThemeDiagnosticsModal;
                }
                return _instance;
            }
        }

        public override void Start()
        {
            base.Start();

            backgroundSprite = "UnlockingPanel2";
            isVisible = false;
            canFocus = true;
            isInteractive = true;
            clipChildren = true;
            width = 600;
            height = 560;
            relativePosition = new Vector3(
                Mathf.Floor((GetUIView().fixedWidth - width) / 2),
                Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            m_title = AddUIComponent<UITitleBar>();
            m_title.title = Localization.Get("DIAG_TITLE");
            m_title.iconSprite = "InfoPanelIconFreetime";
            m_title.isModal = true;

            // Scrollable panel that holds the text
            m_scroll = AddUIComponent<UIScrollablePanel>();
            m_scroll.width = width - 20;
            m_scroll.height = height - m_title.height - 50;
            m_scroll.relativePosition = new Vector3(10, m_title.height + 5);
            m_scroll.autoLayout = false;
            m_scroll.clipChildren = true;
            m_scroll.scrollWheelDirection = UIOrientation.Vertical;

            UIScrollbar scrollbar = AddUIComponent<UIScrollbar>();
            scrollbar.width = 10;
            scrollbar.height = m_scroll.height;
            scrollbar.relativePosition = new Vector3(width - 10, m_title.height + 5);
            scrollbar.orientation = UIOrientation.Vertical;
            scrollbar.pivot = UIPivotPoint.TopLeft;
            scrollbar.minValue = 0;
            scrollbar.value = 0;
            scrollbar.incrementAmount = 30;
            scrollbar.scrollEasingType = EasingType.BackEaseOut;
            m_scroll.verticalScrollbar = scrollbar;

            UISlicedSprite track = scrollbar.AddUIComponent<UISlicedSprite>();
            track.spriteName = "ScrollbarTrack";
            track.relativePosition = Vector2.zero;
            track.size = new Vector2(10, m_scroll.height);
            scrollbar.trackObject = track;

            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.spriteName = "ScrollbarThumb";
            thumb.size = new Vector2(10, 20);
            scrollbar.thumbObject = thumb;

            m_text = m_scroll.AddUIComponent<UILabel>();
            m_text.width = m_scroll.width - 5;
            m_text.autoHeight = true;
            m_text.wordWrap = true;
            m_text.textScale = 0.75f;
            m_text.relativePosition = Vector2.zero;

            m_copy = UIUtils.CreateButton(this);
            m_copy.text = Localization.Get("DIAG_COPY");
            m_copy.width = 160f;
            m_copy.relativePosition = new Vector3((width / 2) - m_copy.width - 4, height - m_copy.height - 5);
            m_copy.eventClick += (c, p) =>
            {
                GUIUtility.systemCopyBuffer = m_text.text;
                m_copy.text = Localization.Get("DIAG_COPIED");
                Invoke("ResetCopyButtonText", 2f);
            };

            m_ok = UIUtils.CreateButton(this);
            m_ok.text = Localization.Get("MODAL_CLOSE");
            m_ok.relativePosition = new Vector3((width / 2) + 4, height - m_ok.height - 5);
            m_ok.eventClick += (c, p) => { UIView.PopModal(); Hide(); };
        }

        public void ShowForDistrict(byte districtId)
        {
            if (m_text == null) return;

            var mgr = BuildingThemesManager.instance;
            bool managed = mgr != null && mgr.IsThemeManagementEnabled(districtId);
            string themeList = "(theme management off)";
            if (managed)
            {
                var themes = mgr.GetDistrictThemes(districtId, false);
                if (themes == null || themes.Count == 0)
                    themeList = "(no themes selected)";
                else
                {
                    var names = new System.Collections.Generic.List<string>();
                    foreach (var t in themes) names.Add(t.name);
                    themeList = string.Join(", ", names.ToArray());
                }
            }
            Debugger.LogFormat("[UserAction] DiagnosticsModal opened for district {0}. Enabled themes: {1}", districtId, themeList);
            string report = ThemeDiagnostics.FormatReport(districtId);

            // Append Skyve note if there are missing assets and Skyve is installed
            if (SkyveDetector.IsInstalled && report.Contains("Missing"))
            {
                report += "\n\n" + Localization.Get("DIAG_SKYVE_NOTE");
            }

            m_text.text = report;
            m_scroll.scrollPosition = Vector2.zero;

            UIView.PushModal(this);
            Show(true);
        }

        protected override void OnVisibilityChanged()
        {
            base.OnVisibilityChanged();

            UIComponent modalEffect = GetUIView().panelsLibraryModalEffect;
            if (isVisible)
            {
                Focus();
                if (modalEffect != null)
                {
                    modalEffect.Show(false);
                    ValueAnimator.Animate("DiagnosticsModalEffect", val => modalEffect.opacity = val,
                        new AnimatedFloat(0f, 1f, 0.4f, EasingType.CubicEaseOut));
                }
            }
            else if (modalEffect != null)
            {
                ValueAnimator.Animate("DiagnosticsModalEffect", val => modalEffect.opacity = val,
                    new AnimatedFloat(1f, 0f, 0.4f, EasingType.CubicEaseOut),
                    () => modalEffect.Hide());
            }
        }

        private void ResetCopyButtonText()
        {
            if (m_copy != null) m_copy.text = Localization.Get("DIAG_COPY");
        }

        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Return))
            {
                p.Use();
                UIView.PopModal();
                Hide();
            }
            base.OnKeyDown(p);
        }
    }
}
