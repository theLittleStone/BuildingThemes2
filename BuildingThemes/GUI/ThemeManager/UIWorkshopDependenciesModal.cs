using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using UnityEngine;

namespace BuildingThemes.GUI
{
    /// <summary>
    /// Modal that lists all workshop assets belonging to the selected theme,
    /// grouped into Missing / Inactive / Loaded sections.
    /// Provides a "Copy Missing IDs" button for quick re-subscription.
    /// </summary>
    public class UIWorkshopDependenciesModal : UIPanel
    {
        private enum RowState { Loaded, Missing }
        private UITitleBar m_title;
        private UILabel m_summaryLabel;
        private UIScrollbar m_scrollbar;
        private UIScrollablePanel m_scrollPanel;
        private UIButton m_copyMissing;
        private UIButton m_copyAll;
        private UIButton m_subscribeMissing;
        private UIButton m_close;

        private Configuration.Theme m_theme;
        private readonly List<string> m_missingIDs = new List<string>();
        private readonly List<string> m_allWorkshopIDs = new List<string>();

        private static UIWorkshopDependenciesModal _instance;

        public static UIWorkshopDependenciesModal instance
        {
            get
            {
                if (_instance == null)
                    _instance = UIView.GetAView().AddUIComponent(typeof(UIWorkshopDependenciesModal))
                        as UIWorkshopDependenciesModal;
                return _instance;
            }
        }

        public void ShowFor(Configuration.Theme theme)
        {
            m_theme = theme;
            PopulateList();
            Debugger.LogFormat("WorkshopDependenciesModal opened for theme '{0}'.", theme != null ? theme.name : "null");
            Show(true);
            Focus();
            BringToFront();
        }

        public override void Start()
        {
            base.Start();

            backgroundSprite = "UnlockingPanel2";
            isVisible = false;
            canFocus = true;
            isInteractive = true;
            clipChildren = true;
            width = 520;
            height = 450;
            relativePosition = new Vector3(
                Mathf.Floor((GetUIView().fixedWidth - width) / 2),
                Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            // Title
            m_title = AddUIComponent<UITitleBar>();
            m_title.title = "Workshop Dependencies";
            m_title.iconSprite = "SteamWorkshop";
            m_title.isModal = false; // not a blocking modal — X calls parent.Hide() directly

            // Summary label (below title)
            m_summaryLabel = AddUIComponent<UILabel>();
            m_summaryLabel.width = width - 20;
            m_summaryLabel.autoHeight = true;
            m_summaryLabel.textScale = 0.85f;
            m_summaryLabel.wordWrap = true;
            m_summaryLabel.relativePosition = new Vector3(10, m_title.height + 8);

            float listTop = m_title.height + 35;
            float listHeight = height - listTop - 50; // 50 = button row

            // Scrollbar
            m_scrollbar = AddUIComponent<UIScrollbar>();
            m_scrollbar.width = 10;
            m_scrollbar.height = listHeight;
            m_scrollbar.orientation = UIOrientation.Vertical;
            m_scrollbar.pivot = UIPivotPoint.TopLeft;
            m_scrollbar.minValue = 0;
            m_scrollbar.value = 0;
            m_scrollbar.incrementAmount = 40;
            m_scrollbar.relativePosition = new Vector3(width - 15, listTop);

            UISlicedSprite track = m_scrollbar.AddUIComponent<UISlicedSprite>();
            track.relativePosition = Vector2.zero;
            track.autoSize = true;
            track.anchor = UIAnchorStyle.All;
            track.size = track.parent.size;
            track.fillDirection = UIFillDirection.Vertical;
            track.spriteName = "ScrollbarTrack";
            m_scrollbar.trackObject = track;

            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.relativePosition = Vector2.zero;
            thumb.fillDirection = UIFillDirection.Vertical;
            thumb.autoSize = true;
            thumb.width = thumb.parent.width;
            thumb.spriteName = "ScrollbarThumb";
            m_scrollbar.thumbObject = thumb;

            // Scrollable list panel
            m_scrollPanel = AddUIComponent<UIScrollablePanel>();
            m_scrollPanel.width = width - 30;
            m_scrollPanel.height = listHeight;
            m_scrollPanel.relativePosition = new Vector3(8, listTop);
            m_scrollPanel.clipChildren = true;
            m_scrollPanel.autoLayout = true;
            m_scrollPanel.autoLayoutDirection = LayoutDirection.Vertical;
            m_scrollPanel.autoLayoutPadding = new RectOffset(0, 0, 1, 1);
            m_scrollPanel.verticalScrollbar = m_scrollbar;
            m_scrollPanel.scrollWheelDirection = UIOrientation.Vertical;
            // Wheel events that land on child rows bubble up to the modal; drive the scrollbar directly.
            eventMouseWheel += (c, p) =>
            {
                if (m_scrollbar == null) return;
                m_scrollbar.value += (p.wheelDelta > 0 ? -1f : 1f) * m_scrollbar.incrementAmount;
                p.Use();
            };

            // Buttons row
            m_copyMissing = UIUtils.CreateButton(this);
            m_copyMissing.width = 150;
            m_copyMissing.text = "Copy Missing IDs";
            m_copyMissing.tooltip = "Copy Steam Workshop IDs of all missing assets to clipboard (one per line)";
            m_copyMissing.relativePosition = new Vector3(10, height - 40);
            m_copyMissing.eventClick += (c, p) =>
            {
                if (m_missingIDs.Count > 0)
                    GUIUtility.systemCopyBuffer = string.Join("\n", m_missingIDs.ToArray());
            };

            m_copyAll = UIUtils.CreateButton(this);
            m_copyAll.width = 130;
            m_copyAll.text = "Copy All IDs";
            m_copyAll.tooltip = "Copy Steam Workshop IDs of all assets in this theme to clipboard (one per line)\nUseful for managing activation in Skyve";
            m_copyAll.relativePosition = new Vector3(10 + 150 + 5, height - 40);
            m_copyAll.eventClick += (c, p) =>
            {
                if (m_allWorkshopIDs.Count > 0)
                    GUIUtility.systemCopyBuffer = string.Join("\n", m_allWorkshopIDs.ToArray());
            };

            // Subscribe Missing — only shown when the Steam overlay is available
            if (PlatformService.IsOverlayEnabled())
            {
                m_subscribeMissing = UIUtils.CreateButton(this);
                m_subscribeMissing.width = 130;
                m_subscribeMissing.text = "Subscribe Missing";
                m_subscribeMissing.tooltip = "Subscribe to all missing workshop assets on Steam\nThe game must be restarted to load the newly subscribed assets";
                m_subscribeMissing.relativePosition = new Vector3(10 + 150 + 5 + 130 + 5, height - 40);
                m_subscribeMissing.eventClick += (c, p) =>
                {
                    int count = 0;
                    foreach (string idStr in m_missingIDs)
                    {
                        ulong id;
                        if (!ulong.TryParse(idStr, out id)) continue;
                        try
                        {
                            PlatformService.workshop.Subscribe(new PublishedFileId(id));
                            count++;
                        }
                        catch (Exception e)
                        {
                            Debugger.LogException(e);
                        }
                    }
                    Debugger.LogFormat("Subscribe Missing: requested {0} subscription(s).", count);
                    m_subscribeMissing.text = count > 0
                        ? string.Format("Requested ({0})", count)
                        : "Subscribe Missing";
                    m_subscribeMissing.isEnabled = false;
                };
            }

            m_close = UIUtils.CreateButton(this);
            m_close.width = 90;
            m_close.text = "Close";
            m_close.relativePosition = new Vector3(width - 100, height - 40);
            m_close.eventClick += (c, p) => Hide();
        }

        private void PopulateList()
        {
            // Clear previous rows
            var existing = new List<UIComponent>(m_scrollPanel.components);
            foreach (var child in existing)
                Destroy(child.gameObject);

            m_missingIDs.Clear();
            m_allWorkshopIDs.Clear();

            if (m_theme == null || UIThemeManager.instance == null)
            {
                SetSummary("No theme selected.");
                UpdateCopyButton();
                return;
            }

            List<BuildingItem> allItems = UIThemeManager.instance.GetBuildingItems(m_theme);

            var loaded  = new List<BuildingItem>();
            var missing = new List<BuildingItem>(); // not loaded (not subscribed, disabled, or load error)

            foreach (BuildingItem item in allItems)
            {
                if (item.steamID == null) continue; // not a workshop asset
                if (item.assetStatus == AssetStatus.Available)
                    loaded.Add(item);
                else
                    missing.Add(item);
            }

            foreach (BuildingItem item in missing)
                m_missingIDs.Add(item.steamID);

            foreach (BuildingItem item in loaded)
                m_allWorkshopIDs.Add(item.steamID);
            foreach (BuildingItem item in missing)
                m_allWorkshopIDs.Add(item.steamID);

            int total = loaded.Count + missing.Count;
            SetSummary(string.Format(
                "{0} workshop asset(s) — {1} loaded, {2} missing.",
                total, loaded.Count, missing.Count));

            // Missing section
            if (missing.Count > 0)
            {
                AddSectionHeader("Missing — not loaded (" + missing.Count + ")", new Color32(220, 70, 70, 255));
                foreach (BuildingItem item in missing)
                    AddRow(item, RowState.Missing);
            }

            // Loaded section
            if (loaded.Count > 0)
            {
                AddSectionHeader("Loaded (" + loaded.Count + ")", new Color32(100, 200, 100, 255));
                foreach (BuildingItem item in loaded)
                    AddRow(item, RowState.Loaded);
            }

            if (total == 0)
            {
                UILabel empty = m_scrollPanel.AddUIComponent<UILabel>();
                empty.text = "This theme has no workshop assets.";
                empty.textScale = 0.85f;
                empty.textColor = new Color32(160, 160, 160, 255);
                empty.width = m_scrollPanel.width - 5;
                empty.autoHeight = true;
                empty.padding = new RectOffset(4, 0, 4, 0);
            }

            UpdateCopyButton();
        }

        private void SetSummary(string text)
        {
            if (m_summaryLabel != null)
                m_summaryLabel.text = text;
        }

        private void AddSectionHeader(string text, Color32 color)
        {
            UILabel header = m_scrollPanel.AddUIComponent<UILabel>();
            header.text = text;
            header.textScale = 0.85f;
            header.textColor = color;
            header.width = m_scrollPanel.width - 5;
            header.autoHeight = true;
            header.padding = new RectOffset(4, 0, 6, 2);
        }

        private void AddRow(BuildingItem item, RowState state)
        {
            UIPanel row = m_scrollPanel.AddUIComponent<UIPanel>();
            row.width = m_scrollPanel.width - 5;
            row.height = 22;

            // Name label
            UILabel nameLabel = row.AddUIComponent<UILabel>();
            nameLabel.textScale = 0.78f;
            Color32 nameColor = state == RowState.Loaded
                ? new Color32(210, 210, 210, 255)
                : new Color32(200, 100, 100, 255);
            nameLabel.textColor = nameColor;
            nameLabel.text = item.displayName;
            nameLabel.autoSize = false;
            nameLabel.width = row.width - 145;
            nameLabel.height = 20;
            nameLabel.relativePosition = new Vector3(4, 3);

            // Steam ID label
            UILabel idLabel = row.AddUIComponent<UILabel>();
            idLabel.textScale = 0.72f;
            idLabel.textColor = new Color32(140, 140, 180, 255);
            idLabel.text = item.steamID;
            idLabel.width = 110;
            idLabel.height = 20;
            idLabel.textAlignment = UIHorizontalAlignment.Right;
            idLabel.relativePosition = new Vector3(row.width - 140, 3);
            idLabel.tooltip = "Steam Workshop ID: " + item.steamID;

            // "Open" button — only when Steam overlay is available
            if (PlatformService.IsOverlayEnabled())
            {
                UIButton openBtn = UIUtils.CreateButton(row);
                openBtn.width = 35;
                openBtn.height = 18;
                openBtn.text = "Open";
                openBtn.textScale = 0.65f;
                openBtn.relativePosition = new Vector3(row.width - 38, 2);
                openBtn.tooltip = "Open in Steam Workshop";

                string capturedID = item.steamID;
                openBtn.eventClick += (c, p) =>
                {
                    ulong id;
                    if (ulong.TryParse(capturedID, out id))
                        PlatformService.ActivateGameOverlayToWorkshopItem(new PublishedFileId(id));
                };
            }
        }

        private void UpdateCopyButton()
        {
            if (m_copyMissing != null)
                m_copyMissing.isEnabled = m_missingIDs.Count > 0;
            if (m_copyAll != null)
                m_copyAll.isEnabled = m_allWorkshopIDs.Count > 0;
            if (m_subscribeMissing != null)
            {
                m_subscribeMissing.isEnabled = m_missingIDs.Count > 0;
                m_subscribeMissing.text = "Subscribe Missing";
            }
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
                    ValueAnimator.Animate("DepModalEffect", delegate(float val)
                    {
                        modalEffect.opacity = val;
                    }, new AnimatedFloat(0f, 1f, 0.7f, EasingType.CubicEaseOut));
                }
            }
            else if (modalEffect != null)
            {
                ValueAnimator.Animate("DepModalEffect", delegate(float val)
                {
                    modalEffect.opacity = val;
                }, new AnimatedFloat(1f, 0f, 0.7f, EasingType.CubicEaseOut), delegate
                {
                    modalEffect.Hide();
                });
            }
        }
    }
}
