using ColossalFramework;
using ColossalFramework.UI;
using System;
using UnityEngine;

namespace BuildingThemes.GUI
{
    /// <summary>
    /// Floating panel with per-district theme options.
    /// Opened via "District Options" in ThemePolicyTab.
    /// Updates every frame to reflect the currently selected district.
    /// </summary>
    public class UIDistrictOptionsPanel : UIPanel
    {
        private UICheckBox m_blacklistCheck;
        private UIDropDown m_levelDropdown;
        private UIDropDown m_missingDropdown;
        private UICheckBox m_autoBulldozeCheck;

        // Prevents Update() sync from re-firing eventChanged callbacks
        private bool _updating;

        private static UIDistrictOptionsPanel _instance;

        public static UIDistrictOptionsPanel instance
        {
            get
            {
                if (_instance == null)
                    _instance = UIView.GetAView().AddUIComponent(typeof(UIDistrictOptionsPanel)) as UIDistrictOptionsPanel;
                return _instance;
            }
        }

        public void Toggle()
        {
            if (isVisible)
                Hide();
            else
            {
                Debugger.LogFormat("DistrictOptionsPanel opened.");
                Show(true);
                BringToFront();
            }
        }

        public override void Start()
        {
            base.Start();

            // Layout constants
            const float W = 390f;
            const float TITLE_H = 40f;
            const float X = 10f;
            const float CW = W - X * 2f; // 370 — full content width
            const float ROW_GAP = 12f;  // gap between a dropdown/control and the next section label
            const float LBL_GAP = 5f;   // gap between a section label and its own dropdown
            const float LBL_H = 36f;  // fixed label height (enough for 2 wrapped lines at scale 0.9)
            const float DROP_H = 28f;
            const float CHK_H = 20f;
            const float BTN_H = 30f;

            float y = TITLE_H + 8f;

            backgroundSprite = "UnlockingPanel2";
            isVisible = false;
            canFocus = true;
            isInteractive = true;
            clipChildren = true;
            width = W;

            relativePosition = new Vector3(
                Mathf.Floor((GetUIView().fixedWidth - W) / 2),
                Mathf.Floor((GetUIView().fixedHeight - 320f) / 2));

            var titleBar = AddUIComponent<UITitleBar>();
            titleBar.title = "District Theme Options";
            titleBar.iconSprite = "ToolbarIconZoomOutDistrict";
            titleBar.isModal = false;

            // ── Blacklist mode ────────────────────────────────────────
            m_blacklistCheck = ThemePolicyTab.CreateCheckBox(this);
            m_blacklistCheck.width = CW;
            m_blacklistCheck.relativePosition = new Vector3(X, y);
            m_blacklistCheck.text = "Allow buildings not in any theme";
            y += CHK_H + ROW_GAP;

            m_blacklistCheck.tooltip =
                "When enabled, all vanilla/theme buildings can spawn — the themes become a blacklist\n" +
                "(only explicitly excluded buildings are blocked).\n" +
                "When disabled, only buildings included in an active theme for this district can spawn.";

            m_blacklistCheck.eventCheckChanged += (c, val) =>
            {
                if (_updating) return;
                Singleton<BuildingThemesManager>.instance.ToggleBlacklistMode(GetDistrictId(), val);
            };

            // ── Level behavior label ──────────────────────────────────
            UILabel levelLabel = AddUIComponent<UILabel>();
            levelLabel.autoSize = false;
            levelLabel.width = CW;
            levelLabel.height = LBL_H;
            levelLabel.textScale = 0.9f;
            levelLabel.wordWrap = true;
            levelLabel.text = "When a building levels up and the theme has no buildings for that level:";
            levelLabel.relativePosition = new Vector3(X, y);
            y += LBL_H + LBL_GAP;

            // ── Level behavior dropdown ───────────────────────────────
            m_levelDropdown = UIUtils.CreateDropDown(this);
            m_levelDropdown.width = CW;
            m_levelDropdown.size = new Vector2(CW, DROP_H);
            m_levelDropdown.relativePosition = new Vector3(X, y);
            m_levelDropdown.items = new string[] {
                "Vanilla fallback — game picks a vanilla building",
                "Strict — freeze the upgrade; building stays at current level"
            };
            m_levelDropdown.tooltip =
                "Vanilla fallback: the game picks a vanilla building for the higher level.\n" +
                "The building changes appearance on upgrade (game default).\n\n" +
                "Strict: upgrades are blocked when the theme has no buildings at the target level.\n" +
                "The building stays at its current level indefinitely.\n\n" +
                "To control exactly which building a level-up produces, use the 'Upgrade to' field\n" +
                "in the Theme Manager options panel for that building.";
            y += DROP_H + ROW_GAP;

            m_levelDropdown.eventSelectedIndexChanged += (c, val) =>
            {
                if (_updating) return;
                byte districtId = GetDistrictId();
                if (!BuildingThemesManager.instance.IsThemeManagementEnabled(districtId)) return;
                var behavior = IndexToLevelBehavior(val);
                if (BuildingThemesManager.instance.GetDistrictEmptyLevelBehavior(districtId) == behavior) return;
                BuildingThemesManager.instance.SetDistrictEmptyLevelBehavior(districtId, behavior);
            };

            // ── Missing asset label ───────────────────────────────────
            UILabel missingLabel = AddUIComponent<UILabel>();
            missingLabel.autoSize = false;
            missingLabel.width = CW;
            missingLabel.height = LBL_H;
            missingLabel.textScale = 0.9f;
            missingLabel.wordWrap = true;
            missingLabel.text = "When a subscribed building is not loaded (unsubscribed or disabled):";
            missingLabel.relativePosition = new Vector3(X, y);
            y += LBL_H + LBL_GAP - 1f;

            // ── Missing asset dropdown ────────────────────────────────
            m_missingDropdown = UIUtils.CreateDropDown(this);
            m_missingDropdown.width = CW;
            m_missingDropdown.size = new Vector2(CW, DROP_H);
            m_missingDropdown.relativePosition = new Vector3(X, y);
            m_missingDropdown.listPosition = UIDropDown.PopupListPosition.Above;
            m_missingDropdown.items = new string[] {
                "Skip — theme-only, slots may be sparse",
                "Fill with vanilla — supplement missing slots",
                "Fall back to vanilla — use vanilla if sparse"
            };
            m_missingDropdown.tooltip =
                "Skip: missing buildings are quietly dropped. The theme still applies using\n" +
                "only loaded assets — slots may be sparse if many are missing.\n\n" +
                "Fill with vanilla: vanilla buildings supplement each missing slot.\n" +
                "Your loaded theme buildings still appear; vanilla fills the gaps.\n\n" +
                "Fall back to vanilla: if any building in a slot is missing, that entire\n" +
                "slot falls back to vanilla only. No sparse areas, but less theme coverage.";
            y += DROP_H + ROW_GAP;

            // ── Auto-bulldoze ─────────────────────────────────────────
            m_autoBulldozeCheck = ThemePolicyTab.CreateCheckBox(this);
            m_autoBulldozeCheck.width = CW;
            m_autoBulldozeCheck.relativePosition = new Vector3(X, y);
            m_autoBulldozeCheck.text = "Auto-remove non-theme buildings";
            m_autoBulldozeCheck.tooltip =
                "Gradually demolishes existing buildings in this district that are not\n" +
                "part of any active theme. Only affects growable residential, commercial,\n" +
                "industrial, and office buildings — service buildings are never touched.\n" +
                "Has no effect when 'Allow buildings not in any theme' is enabled.";
            y += CHK_H + ROW_GAP;

            m_autoBulldozeCheck.eventCheckChanged += (c, val) =>
            {
                if (_updating) return;
                BuildingThemesManager.instance.SetDistrictAutoBulldoze(GetDistrictId(), val);
            };

            // ── Spawn Diagnostics ─────────────────────────────────────
            UIButton diagnosticsBtn = UIUtils.CreateButton(this);
            diagnosticsBtn.width = CW;
            diagnosticsBtn.size = new Vector2(CW, BTN_H);
            diagnosticsBtn.relativePosition = new Vector3(X, y);
            diagnosticsBtn.text = "Spawn Diagnostics";
            diagnosticsBtn.tooltip =
                "Show why buildings are or are not spawning in this district.\n" +
                "Enable 'Generate Debug Output' in mod settings for detailed data.";
            diagnosticsBtn.eventClick += (c, p) =>
            {
                try { UIThemeDiagnosticsModal.instance.ShowForDistrict(GetDistrictId()); }
                catch (Exception e) { Debugger.LogException(e); }
            };
            y += BTN_H + 10f;

            // Pre-create diagnostics modal so its Start() runs before the first user click
            UIThemeDiagnosticsModal.instance.Hide();

            m_missingDropdown.eventSelectedIndexChanged += (c, val) =>
            {
                if (_updating) return;
                byte districtId = GetDistrictId();
                if (!BuildingThemesManager.instance.IsThemeManagementEnabled(districtId)) return;
                if ((int)BuildingThemesManager.instance.GetDistrictMissingAssetMode(districtId) == val) return;
                BuildingThemesManager.instance.SetDistrictMissingAssetMode(districtId, (MissingAssetMode)val);
            };

            height = y;
        }

        public override void Update()
        {
            base.Update();
            if (!isVisible) return;
            if (ToolsModifierControl.policiesPanel == null) return;

            _updating = true;
            try
            {
                byte districtId = GetDistrictId();
                bool managed = BuildingThemesManager.instance.IsThemeManagementEnabled(districtId);
                bool blacklist = BuildingThemesManager.instance.IsBlacklistModeEnabled(districtId);

                if (m_blacklistCheck != null && m_blacklistCheck.isChecked != blacklist)
                    m_blacklistCheck.isChecked = blacklist;

                if (m_levelDropdown != null)
                {
                    m_levelDropdown.isEnabled = managed;
                    m_levelDropdown.opacity = managed ? 1f : 0.5f;
                    if (managed)
                    {
                        int idx = LevelBehaviorToIndex(BuildingThemesManager.instance.GetDistrictEmptyLevelBehavior(districtId));
                        if (m_levelDropdown.selectedIndex != idx)
                            m_levelDropdown.selectedIndex = idx;
                    }
                }

                if (m_missingDropdown != null)
                {
                    m_missingDropdown.isEnabled = managed;
                    m_missingDropdown.opacity = managed ? 1f : 0.5f;
                    if (managed)
                    {
                        int val = (int)BuildingThemesManager.instance.GetDistrictMissingAssetMode(districtId);
                        if (m_missingDropdown.selectedIndex != val)
                            m_missingDropdown.selectedIndex = val;
                    }
                }

                if (m_autoBulldozeCheck != null)
                {
                    bool effectiveBlacklist = BuildingThemesManager.instance.IsBlacklistModeEnabled(districtId);
                    // Auto-bulldoze only makes sense when theme management is on and blacklist mode is off
                    m_autoBulldozeCheck.isEnabled = managed && !effectiveBlacklist;
                    m_autoBulldozeCheck.opacity = (managed && !effectiveBlacklist) ? 1f : 0.5f;
                    bool bulldoze = BuildingThemesManager.instance.GetDistrictAutoBulldoze(districtId);
                    if (m_autoBulldozeCheck.isChecked != bulldoze)
                        m_autoBulldozeCheck.isChecked = bulldoze;
                }
            }
            finally { _updating = false; }
        }

        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                p.Use();
                Hide();
            }
            base.OnKeyDown(p);
        }

        private static byte GetDistrictId()
        {
            return ToolsModifierControl.policiesPanel != null
                ? ToolsModifierControl.policiesPanel.targetDistrict
                : (byte)0;
        }

        // Maps between 2-item dropdown index (0,1) and the non-contiguous enum values
        // (VanillaFallback=0, StrictThemeOnly=2 — CascadeFromTheme=1 is removed).
        private static int LevelBehaviorToIndex(EmptyLevelBehavior b)
            => b == EmptyLevelBehavior.StrictThemeOnly ? 1 : 0;

        private static EmptyLevelBehavior IndexToLevelBehavior(int idx)
            => idx == 1 ? EmptyLevelBehavior.StrictThemeOnly : EmptyLevelBehavior.VanillaFallback;
    }
}
