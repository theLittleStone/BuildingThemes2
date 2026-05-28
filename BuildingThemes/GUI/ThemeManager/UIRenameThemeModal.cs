using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BuildingThemes.GUI
{
    public class UIRenameThemeModal : UIPanel
    {
        private UITitleBar m_title;
        private UITextField m_name;
        private UIButton m_ok;
        private UIButton m_cancel;

        private Configuration.Theme m_theme;

        private static UIRenameThemeModal _instance;

        public static UIRenameThemeModal instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UIView.GetAView().AddUIComponent(typeof(UIRenameThemeModal)) as UIRenameThemeModal;
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
            width = 250;

            m_title = AddUIComponent<UITitleBar>();
            m_title.title = "Rename Theme";
            m_title.iconSprite = "ToolbarIconZoomOutCity";
            m_title.isModal = true;

            UILabel label = AddUIComponent<UILabel>();
            label.height = 30;
            label.text = "New name:";
            label.relativePosition = new Vector3(5, m_title.height);

            m_name = UIUtils.CreateTextField(this);
            m_name.width = width - 10;
            m_name.height = 30;
            m_name.padding = new RectOffset(6, 6, 6, 6);
            m_name.relativePosition = new Vector3(5, label.relativePosition.y + label.height + 5);

            m_name.eventTextChanged += (c, s) =>
            {
                m_ok.isEnabled = !s.IsNullOrWhiteSpace()
                    && s != (m_theme != null ? m_theme.name : "")
                    && BuildingThemesManager.instance.GetLocalThemeByName(s) == null;
            };

            m_name.eventTextSubmitted += (c, s) =>
            {
                if (m_ok.isEnabled && Input.GetKey(KeyCode.Return)) m_ok.SimulateClick();
            };

            m_ok = UIUtils.CreateButton(this);
            m_ok.text = "Rename";
            m_ok.isEnabled = false;
            m_ok.relativePosition = new Vector3(5, m_name.relativePosition.y + m_name.height + 5);

            m_ok.eventClick += (c, p) =>
            {
                UIThemeManager.instance.RenameTheme(m_theme, m_name.text);
                UIView.PopModal();
                Hide();
            };

            m_cancel = UIUtils.CreateButton(this);
            m_cancel.text = "Cancel";
            m_cancel.relativePosition = new Vector3(width - m_cancel.width - 5, m_ok.relativePosition.y);

            m_cancel.eventClick += (c, p) =>
            {
                UIView.PopModal();
                Hide();
            };

            height = m_cancel.relativePosition.y + m_cancel.height + 5;
            relativePosition = new Vector3(
                Mathf.Floor((GetUIView().fixedWidth - width) / 2),
                Mathf.Floor((GetUIView().fixedHeight - height) / 2));
        }

        public void ShowFor(Configuration.Theme theme)
        {
            m_theme = theme;
            m_name.text = theme.name;
            m_name.Focus();
            m_name.SelectAll();
            UIView.PushModal(this);
            Show(true);
        }

        protected override void OnVisibilityChanged()
        {
            base.OnVisibilityChanged();

            UIComponent modalEffect = GetUIView().panelsLibraryModalEffect;

            if (isVisible)
            {
                if (modalEffect != null)
                {
                    modalEffect.Show(false);
                    ValueAnimator.Animate("RenameThemeModalEffect", delegate(float val)
                    {
                        modalEffect.opacity = val;
                    }, new AnimatedFloat(0f, 1f, 0.7f, EasingType.CubicEaseOut));
                }
            }
            else if (modalEffect != null)
            {
                ValueAnimator.Animate("RenameThemeModalEffect", delegate(float val)
                {
                    modalEffect.opacity = val;
                }, new AnimatedFloat(1f, 0f, 0.7f, EasingType.CubicEaseOut), delegate
                {
                    modalEffect.Hide();
                });
            }
        }

        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                p.Use();
                UIView.PopModal();
                Hide();
            }

            base.OnKeyDown(p);
        }
    }
}
