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
        private UICheckBox m_preferElectricityCheck;

        // Size-preference controls
        private UIDropDown m_resSizePref;
        private UIDropDown m_comSizePref;
        private UIDropDown m_indSizePref;
        private UIDropDown m_offSizePref;
        private UIDropDown m_strengthPref;

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

        public static void Cleanup()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
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

            // ── Layout constants ──────────────────────────────────────────────
            const float W       = 580f;
            const float TITLE_H = 40f;
            const float X       = 12f;
            const float GAP     = 12f;          // gap between two side-by-side columns
            const float CW      = W - X * 2f;  // 556 — full content width
            const float HW      = (CW - GAP) / 2f; // 272 — half-column width
            const float ROW_GAP = 10f;
            const float SEC_GAP = 5f;           // within a section (label → dropdown)
            const float DROP_H  = 28f;
            const float LBL_H   = 20f;
            const float CHK_H   = 20f;
            const float BTN_H   = 30f;
            const float TS      = 0.8f;         // label textScale

            float y = TITLE_H + 10f;

            backgroundSprite = "UnlockingPanel2";
            isVisible        = false;
            canFocus         = true;
            isInteractive    = true;
            clipChildren     = false; // allow dropdown popup lists to render outside panel bounds
            width            = W;

            relativePosition = new Vector3(
                Mathf.Floor((GetUIView().fixedWidth  - W)     / 2),
                Mathf.Floor((GetUIView().fixedHeight - 480f)  / 2));

            var titleBar = AddUIComponent<UITitleBar>();
            titleBar.title       = "District Theme Options";
            titleBar.iconSprite  = "ToolbarIconZoomOutDistrict";
            titleBar.isModal     = false;

            // ── Blacklist mode (full width) ───────────────────────────────────
            m_blacklistCheck = ThemePolicyTab.CreateCheckBox(this);
            m_blacklistCheck.width            = CW;
            m_blacklistCheck.relativePosition = new Vector3(X, y);
            m_blacklistCheck.text             = "Allow buildings not in any theme (blacklist mode)";
            m_blacklistCheck.tooltip =
                "When enabled, all vanilla/theme buildings can spawn — the themes become a blacklist\n" +
                "(only explicitly excluded buildings are blocked).\n" +
                "When disabled, only buildings included in an active theme for this district can spawn.";
            m_blacklistCheck.eventCheckChanged += (c, val) =>
            {
                if (_updating) return;
                Singleton<BuildingThemesManager>.instance.ToggleBlacklistMode(GetDistrictId(), val);
            };
            y += CHK_H + ROW_GAP;

            // ── Level behavior  |  Missing asset  (two columns) ─────────────
            AddLabel(X,          y, HW, LBL_H, TS, "Level up — no buildings for that level:");
            AddLabel(X + HW + GAP, y, HW, LBL_H, TS, "Subscribed building not loaded:");
            y += LBL_H + SEC_GAP;

            m_levelDropdown = UIUtils.CreateDropDown(this);
            m_levelDropdown.width            = HW;
            m_levelDropdown.size             = new Vector2(HW, DROP_H);
            m_levelDropdown.relativePosition = new Vector3(X, y);
            m_levelDropdown.items            = new string[] {
                "Vanilla fallback — game picks a vanilla building",
                "Strict — freeze upgrades at current level"
            };
            m_levelDropdown.tooltip =
                "Vanilla fallback: the game picks a vanilla building for the higher level.\n\n" +
                "Strict: upgrades are blocked when the theme has no buildings at the target level.\n" +
                "The building stays at its current level indefinitely.\n\n" +
                "To control exactly which building a level-up produces, use the 'Upgrade to' field\n" +
                "in the Theme Manager for that building.";
            m_levelDropdown.eventSelectedIndexChanged += (c, val) =>
            {
                if (_updating) return;
                byte districtId = GetDistrictId();
                if (!BuildingThemesManager.instance.IsThemeManagementEnabled(districtId)) return;
                var behavior = IndexToLevelBehavior(val);
                if (BuildingThemesManager.instance.GetDistrictEmptyLevelBehavior(districtId) == behavior) return;
                BuildingThemesManager.instance.SetDistrictEmptyLevelBehavior(districtId, behavior);
            };

            m_missingDropdown = UIUtils.CreateDropDown(this);
            m_missingDropdown.width            = HW;
            m_missingDropdown.size             = new Vector2(HW, DROP_H);
            m_missingDropdown.relativePosition = new Vector3(X + HW + GAP, y);
            m_missingDropdown.items            = new string[] {
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
            m_missingDropdown.eventSelectedIndexChanged += (c, val) =>
            {
                if (_updating) return;
                byte districtId = GetDistrictId();
                if (!BuildingThemesManager.instance.IsThemeManagementEnabled(districtId)) return;
                if ((int)BuildingThemesManager.instance.GetDistrictMissingAssetMode(districtId) == val) return;
                BuildingThemesManager.instance.SetDistrictMissingAssetMode(districtId, (MissingAssetMode)val);
            };
            y += DROP_H + ROW_GAP;

            // ── Auto-bulldoze (full width) ────────────────────────────────────
            m_autoBulldozeCheck = ThemePolicyTab.CreateCheckBox(this);
            m_autoBulldozeCheck.width            = CW;
            m_autoBulldozeCheck.relativePosition = new Vector3(X, y);
            m_autoBulldozeCheck.text             = "Auto-remove non-theme buildings";
            m_autoBulldozeCheck.tooltip =
                "Gradually demolishes existing buildings in this district that are not\n" +
                "part of any active theme. Only affects growable residential, commercial,\n" +
                "industrial, and office buildings — service buildings are never touched.\n" +
                "Has no effect when 'Allow buildings not in any theme' is enabled.";
            m_autoBulldozeCheck.eventCheckChanged += (c, val) =>
            {
                if (_updating) return;
                BuildingThemesManager.instance.SetDistrictAutoBulldoze(GetDistrictId(), val);
            };
            y += CHK_H + ROW_GAP;

            // ── Prefer electricity ────────────────────────────────────────────
            m_preferElectricityCheck = ThemePolicyTab.CreateCheckBox(this);
            m_preferElectricityCheck.width            = CW;
            m_preferElectricityCheck.relativePosition = new Vector3(X, y);
            m_preferElectricityCheck.text             = "Prefer zones with electricity";
            m_preferElectricityCheck.tooltip =
                "Only spawn new buildings in zone cells that are already connected to\n" +
                "the electricity grid. Cells without power are skipped and retried later.\n" +
                "\n" +
                "⚠ Spawning can be significantly slower while large parts of the district\n" +
                "are not yet electrified. After 40 consecutive skips the filter is\n" +
                "suspended automatically so growth is never blocked permanently — it\n" +
                "re-activates as soon as an electrified cell is found again.";
            m_preferElectricityCheck.eventCheckChanged += (c, val) =>
            {
                if (_updating) return;
                BuildingThemesManager.instance.SetDistrictPreferElectricity(GetDistrictId(), val);
            };
            y += CHK_H + ROW_GAP;

            // ── Size preference section ───────────────────────────────────────
            AddLabel(X, y, CW, LBL_H, TS, "Building size preference per zone type  (Default = original game behaviour):");
            y += LBL_H + SEC_GAP;

            string[] sizePrefItems = new string[] {
                "Default", "Biggest first", "Widest first",
                "Deepest first", "Random (weight only)", "Smallest first",
                "Tallest first", "Shortest first"
            };
            string sizePrefTooltip =
                "Default: original game behaviour — tries widest lot first, shrinks until a theme building fits.\n" +
                "Biggest first: prefers widest × deepest footprint.\n" +
                "Widest first: prefers widest buildings, tie-break by shallowest depth.\n" +
                "Deepest first: prefers deepest buildings, tie-break by narrowest width.\n" +
                "Random (weight only): no size bias — purely by spawn weight.\n" +
                "Smallest first: prefers narrowest × shallowest footprint.\n" +
                "Tallest first: prefers tallest buildings (by mesh height).\n" +
                "Shortest first: prefers shortest buildings (by mesh height).";

            // Row 1: Residential | Commercial
            AddLabel(X,            y, HW, LBL_H, TS, "Residential:");
            AddLabel(X + HW + GAP, y, HW, LBL_H, TS, "Commercial:");
            y += LBL_H + SEC_GAP;

            m_resSizePref = MakeSizePrefDrop(X,            y, HW, DROP_H, sizePrefItems, sizePrefTooltip);
            m_resSizePref.eventSelectedIndexChanged += (c, val) => {
                if (_updating) return;
                BuildingThemesManager.instance.SetDistrictSizePreference(GetDistrictId(), ItemClass.Service.Residential, (SizePreference)val);
            };
            m_comSizePref = MakeSizePrefDrop(X + HW + GAP, y, HW, DROP_H, sizePrefItems, sizePrefTooltip);
            m_comSizePref.eventSelectedIndexChanged += (c, val) => {
                if (_updating) return;
                BuildingThemesManager.instance.SetDistrictSizePreference(GetDistrictId(), ItemClass.Service.Commercial, (SizePreference)val);
            };
            y += DROP_H + SEC_GAP;

            // Row 2: Industrial | Office
            AddLabel(X,            y, HW, LBL_H, TS, "Industrial:");
            AddLabel(X + HW + GAP, y, HW, LBL_H, TS, "Office:");
            y += LBL_H + SEC_GAP;

            m_indSizePref = MakeSizePrefDrop(X,            y, HW, DROP_H, sizePrefItems, sizePrefTooltip);
            m_indSizePref.eventSelectedIndexChanged += (c, val) => {
                if (_updating) return;
                BuildingThemesManager.instance.SetDistrictSizePreference(GetDistrictId(), ItemClass.Service.Industrial, (SizePreference)val);
            };
            m_offSizePref = MakeSizePrefDrop(X + HW + GAP, y, HW, DROP_H, sizePrefItems, sizePrefTooltip);
            m_offSizePref.eventSelectedIndexChanged += (c, val) => {
                if (_updating) return;
                BuildingThemesManager.instance.SetDistrictSizePreference(GetDistrictId(), ItemClass.Service.Office, (SizePreference)val);
            };
            y += DROP_H + ROW_GAP;

            // ── Preference strength (half width, left) ────────────────────────
            AddLabel(X, y, HW, LBL_H, TS, "Preference strength:");
            y += LBL_H + SEC_GAP;

            m_strengthPref = UIUtils.CreateDropDown(this);
            m_strengthPref.width            = HW;
            m_strengthPref.size             = new Vector2(HW, DROP_H);
            m_strengthPref.relativePosition = new Vector3(X, y);
            m_strengthPref.items = new string[] {
                "Gentle",
                "Moderate",
                "Strong",
                "Absolute (always pick preferred size)"
            };
            m_strengthPref.tooltip =
                "Controls how strongly size preference overrides spawn weight.\n\n" +
                "Gentle (α=0.5): size gives a mild boost — spawn weight still matters a lot.\n" +
                "Moderate (α=1.0): balanced — size and weight both influence selection.\n" +
                "Strong (α=2.0): size strongly dominates — top-ranked sizes appear most of the time.\n" +
                "Absolute: only the highest-ranked size spawns; spawn weight breaks ties within that size.";
            m_strengthPref.eventSelectedIndexChanged += (c, val) => {
                if (_updating) return;
                BuildingThemesManager.instance.SetDistrictPreferenceStrength(GetDistrictId(), (PreferenceStrength)val);
            };
            y += DROP_H + ROW_GAP;

            // ── Spawn Diagnostics (bottom) ────────────────────────────────────
            UIButton diagnosticsBtn = UIUtils.CreateButton(this);
            diagnosticsBtn.width            = CW;
            diagnosticsBtn.size             = new Vector2(CW, BTN_H);
            diagnosticsBtn.relativePosition = new Vector3(X, y);
            diagnosticsBtn.text             = "Spawn Diagnostics";
            diagnosticsBtn.tooltip =
                "Show why buildings are or are not spawning in this district.\n" +
                "Enable 'Generate Debug Output' in mod settings for detailed data.";
            diagnosticsBtn.eventClick += (c, p) =>
            {
                try { UIThemeDiagnosticsModal.instance.ShowForDistrict(GetDistrictId()); }
                catch (Exception e) { Debugger.LogException(e); }
            };
            y += BTN_H + 10f;

            height = y;

            // Pre-create diagnostics modal so its Start() runs before the first user click
            UIThemeDiagnosticsModal.instance.Hide();
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
                bool managed    = BuildingThemesManager.instance.IsThemeManagementEnabled(districtId);
                bool blacklist  = BuildingThemesManager.instance.IsBlacklistModeEnabled(districtId);

                if (m_blacklistCheck != null && m_blacklistCheck.isChecked != blacklist)
                    m_blacklistCheck.isChecked = blacklist;

                if (m_levelDropdown != null)
                {
                    m_levelDropdown.isEnabled = managed;
                    m_levelDropdown.opacity   = managed ? 1f : 0.5f;
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
                    m_missingDropdown.opacity   = managed ? 1f : 0.5f;
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
                    m_autoBulldozeCheck.isEnabled = managed && !effectiveBlacklist;
                    m_autoBulldozeCheck.opacity   = (managed && !effectiveBlacklist) ? 1f : 0.5f;
                    bool bulldoze = BuildingThemesManager.instance.GetDistrictAutoBulldoze(districtId);
                    if (m_autoBulldozeCheck.isChecked != bulldoze)
                        m_autoBulldozeCheck.isChecked = bulldoze;
                }

                if (m_preferElectricityCheck != null)
                {
                    m_preferElectricityCheck.isEnabled = managed;
                    m_preferElectricityCheck.opacity   = managed ? 1f : 0.5f;
                    bool prefElec = BuildingThemesManager.instance.GetDistrictPreferElectricity(districtId);
                    if (m_preferElectricityCheck.isChecked != prefElec)
                        m_preferElectricityCheck.isChecked = prefElec;
                }

                SyncSizeDrop(m_resSizePref, managed, BuildingThemesManager.instance.GetDistrictSizePreference(districtId, ItemClass.Service.Residential));
                SyncSizeDrop(m_comSizePref, managed, BuildingThemesManager.instance.GetDistrictSizePreference(districtId, ItemClass.Service.Commercial));
                SyncSizeDrop(m_indSizePref, managed, BuildingThemesManager.instance.GetDistrictSizePreference(districtId, ItemClass.Service.Industrial));
                SyncSizeDrop(m_offSizePref, managed, BuildingThemesManager.instance.GetDistrictSizePreference(districtId, ItemClass.Service.Office));

                if (m_strengthPref != null)
                {
                    m_strengthPref.isEnabled = managed;
                    m_strengthPref.opacity   = managed ? 1f : 0.5f;
                    int si = (int)BuildingThemesManager.instance.GetDistrictPreferenceStrength(districtId);
                    if (m_strengthPref.selectedIndex != si) m_strengthPref.selectedIndex = si;
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

        private static int LevelBehaviorToIndex(EmptyLevelBehavior b)
            => b == EmptyLevelBehavior.StrictThemeOnly ? 1 : 0;

        private static EmptyLevelBehavior IndexToLevelBehavior(int idx)
            => idx == 1 ? EmptyLevelBehavior.StrictThemeOnly : EmptyLevelBehavior.VanillaFallback;

        private UILabel AddLabel(float x, float y, float w, float h, float textScale, string text)
        {
            UILabel lbl = AddUIComponent<UILabel>();
            lbl.autoSize  = false;
            lbl.width     = w;
            lbl.height    = h;
            lbl.textScale = textScale;
            lbl.wordWrap  = true;
            lbl.text      = text;
            lbl.relativePosition = new Vector3(x, y);
            return lbl;
        }

        private UIDropDown MakeSizePrefDrop(float x, float y, float w, float h, string[] items, string tooltip)
        {
            var dd = UIUtils.CreateDropDown(this);
            dd.width            = w;
            dd.size             = new Vector2(w, h);
            dd.relativePosition = new Vector3(x, y);
            dd.items            = items;
            dd.tooltip          = tooltip;
            return dd;
        }

        private static void SyncSizeDrop(UIDropDown dd, bool managed, SizePreference pref)
        {
            if (dd == null) return;
            dd.isEnabled = managed;
            dd.opacity   = managed ? 1f : 0.5f;
            int idx = (int)pref;
            if (dd.selectedIndex != idx) dd.selectedIndex = idx;
        }
    }
}
