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
            width = 480;
            height = 400;
            relativePosition = new Vector3(
                Mathf.Floor((GetUIView().fixedWidth - width) / 2),
                Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            m_title = AddUIComponent<UITitleBar>();
            m_title.title = "District Theme Diagnostics";
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

            m_ok = UIUtils.CreateButton(this);
            m_ok.text = "Close";
            m_ok.relativePosition = new Vector3((width - m_ok.width) / 2, height - m_ok.height - 5);
            m_ok.eventClick += (c, p) => { UIView.PopModal(); Hide(); };
        }

        public void ShowForDistrict(byte districtId)
        {
            if (m_text == null) return;

            string report = ThemeDiagnostics.FormatReport(districtId);

            // Append Skyve note if there are missing assets and Skyve is installed
            if (SkyveDetector.IsInstalled && report.Contains("Missing"))
            {
                report += "\n\nSkyve is installed — use 'Workshop Dependencies' in the Theme Manager\n" +
                          "to copy missing asset IDs and manage activation in Skyve.";
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
