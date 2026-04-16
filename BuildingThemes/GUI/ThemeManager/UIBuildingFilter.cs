using System;
using System.Collections.Generic;
using ColossalFramework.PlatformServices;
using UnityEngine;
using ColossalFramework.UI;

namespace BuildingThemes.GUI
{
    public class UIBuildingFilter : UIPanel
    {
        private const int NumOfCategories = 19;
        public UICheckBox[] zoningToggles;
        public UIButton allZones;
        public UIButton noZones;
        public UIDropDown origin;
        public UIDropDown status;
        public UIDropDown levelFilter;
        public UIDropDown sizeFilterX;
        public UIDropDown sizeFilterY;
        public UIDropDown sourceFilter;
        public UITextField nameFilter;

        // Sentinel values used as source filter keys (parallel to sourceFilter.items).
        // null = "All", "workshop" = Workshop only, "vanilla" = no DLC/workshop.
        // Any other string = a DLC app-ID string (matches building.dlc).
        private readonly List<string> m_sourceKeys = new List<string>();

        /// <summary>
        /// Returns the current source filter key: null = All, "workshop" = Workshop,
        /// "vanilla" = Vanilla/no-DLC, or a DLC app-ID string for a specific DLC.
        /// </summary>
        public string buildingSourceKey
        {
            get
            {
                if (sourceFilter == null || m_sourceKeys.Count == 0) return null;
                int idx = sourceFilter.selectedIndex;
                return (idx >= 0 && idx < m_sourceKeys.Count) ? m_sourceKeys[idx] : null;
            }
        }

        /// <summary>
        /// Rebuild the "Source" dropdown based on which DLC IDs are actually present
        /// in the currently selected theme.  Call this whenever the theme changes.
        /// <paramref name="dlcIds"/> is the set of distinct non-null building.dlc values
        /// found in the theme. Pass an empty list if the theme has no DLC buildings.
        /// <paramref name="hasVanilla"/> is true when the theme contains at least one
        /// building with no steamID and no dlc requirement.
        /// </summary>
        public void SetSourceOptions(IList<string> dlcIds, bool hasVanilla)
        {
            if (sourceFilter == null) return;

            // Preserve the current selection if the key still exists after rebuild.
            string prevKey = buildingSourceKey;

            m_sourceKeys.Clear();
            m_sourceKeys.Add(null);           // "All"
            m_sourceKeys.Add("workshop");     // "Workshop (Steam)"
            foreach (string id in dlcIds)
                m_sourceKeys.Add(id);
            if (hasVanilla)
                m_sourceKeys.Add("vanilla");

            // Build display names.
            string[] items = new string[m_sourceKeys.Count];
            items[0] = "All";
            items[1] = "Workshop (Steam)";
            for (int i = 2; i < m_sourceKeys.Count; i++)
            {
                string key = m_sourceKeys[i];
                if (key == "vanilla") { items[i] = "Vanilla"; continue; }
                uint appId;
                items[i] = uint.TryParse(key, out appId) ? GetDlcName(appId) : "DLC " + key;
            }

            sourceFilter.items = items;

            // Restore previous selection if it still exists; else reset to "All".
            int restored = m_sourceKeys.IndexOf(prevKey);
            sourceFilter.selectedIndex = restored >= 0 ? restored : 0;
        }

        private static string GetDlcName(uint appId)
        {
            if (appId == SteamHelper.kAfterDLCAppID)               return "After Dark";
            if (appId == SteamHelper.kWinterDLCAppID)              return "Snowfall";
            if (appId == SteamHelper.kNaturalDisastersDLCAppID)    return "Natural Disasters";
            if (appId == SteamHelper.kMotionDLCAppID)              return "Mass Transit";
            if (appId == SteamHelper.kGreenDLCAppID)               return "Green Cities";
            if (appId == SteamHelper.kParksDLCAppID)               return "Parklife";
            if (appId == SteamHelper.kIndustryDLCAppID)            return "Industries";
            if (appId == SteamHelper.kCampusDLCAppID)              return "Campus";
            if (appId == SteamHelper.kUrbanDLCAppID)               return "Sunset Harbor";
            if (appId == SteamHelper.kAirportDLCAppID)             return "Airports";
            if (appId == SteamHelper.kPlazasAndPromenadesDLCAppID) return "Plazas & Promenades";
            if (appId == SteamHelper.kFinancialDistrictsDLCAppID)  return "Financial Districts";
            if (appId == SteamHelper.kHotelsAppID)                 return "Hotels & Retreats";
            if (appId == SteamHelper.kRacesAndParadesDLCAppID)     return "Hubs & Transport";
            return "DLC " + appId;
        }

        // Row 3 — asset loading status toggles
        public bool showLoaded = true;
        public bool showMissing = true;
        public bool showDLCLocked = true;
        public bool canSpawnOnly = false;
        private UICheckBox m_showLoadedCb;
        private UICheckBox m_showMissingCb;
        private UICheckBox m_showDLCCb;
        private UICheckBox m_canSpawnCb;

        public bool IsZoneSelected(Category zone)
        {
            return zoningToggles[(int)zone].isChecked;
        }

        public bool IsAllZoneSelected()
        {
            return zoningToggles[(int)Category.ResidentialLow].isChecked &&
                   zoningToggles[(int)Category.ResidentialHigh].isChecked &&
                   zoningToggles[(int)Category.ResidentialEco].isChecked &&
                   zoningToggles[(int)Category.CommercialLow].isChecked &&
                   zoningToggles[(int)Category.CommercialHigh].isChecked &&
                   zoningToggles[(int)Category.CommercialLeisure].isChecked &&
                   zoningToggles[(int)Category.CommercialTourism].isChecked &&
                   zoningToggles[(int)Category.CommercialEco].isChecked &&
                   zoningToggles[(int)Category.Industrial].isChecked &&
                   zoningToggles[(int)Category.Farming].isChecked &&
                   zoningToggles[(int)Category.Forestry].isChecked &&
                   zoningToggles[(int)Category.Oil].isChecked &&
                   zoningToggles[(int)Category.Ore].isChecked &&
                   zoningToggles[(int)Category.Office].isChecked &&
                   zoningToggles[(int)Category.OfficeHightech].isChecked &&
                   zoningToggles[(int)Category.ResidentialWallToWall].isChecked &&
                   zoningToggles[(int)Category.CommercialWallToWall].isChecked &&
                   zoningToggles[(int)Category.OfficeWallToWall].isChecked &&
                   zoningToggles[(int)Category.OfficeFinancial].isChecked;
        }

        public ItemClass.Level buildingLevel
        {
            get { return (ItemClass.Level)(levelFilter.selectedIndex - 1); }
        }

        public Vector2 buildingSize
        {
            get
            {
                if (sizeFilterX.selectedIndex == 0) return Vector2.zero;
                return new Vector2(sizeFilterX.selectedIndex, sizeFilterY.selectedIndex + 1);
            }
        }

        public string buildingName
        {
            get { return nameFilter.text.Trim(); }
        }

        public Origin buildingOrigin
        {
            get { return (Origin)origin.selectedIndex; }
        }

        public Status buildingStatus
        {
            get { return (Status)status.selectedIndex; }
        }

        public event PropertyChangedEventHandler<int> eventFilteringChanged;

        public override void Start()
        {
            base.Start();

            // Zoning
            zoningToggles = new UICheckBox[NumOfCategories];
            for (int i = 0; i < NumOfCategories; i++)
            {
                zoningToggles[i] = UIUtils.CreateIconToggle(this, CategoryIcons.atlases[i], CategoryIcons.spriteNames[i], CategoryIcons.spriteNames[i] + "Disabled");
                zoningToggles[i].tooltip = CategoryIcons.tooltips[i];
                zoningToggles[i].relativePosition = new Vector3(40 * i, 0);
                zoningToggles[i].isChecked = true;
                zoningToggles[i].readOnly = true;
                zoningToggles[i].checkedBoxObject.isInteractive = false; // Don't eat my double click event please

                zoningToggles[i].eventClick += (c, p) =>
                {
                    ((UICheckBox)c).isChecked = !((UICheckBox)c).isChecked;
                    eventFilteringChanged(this, 0);
                };

                zoningToggles[i].eventDoubleClick += (c, p) =>
                {
                    for (int j = 0; j < NumOfCategories; j++)
                        zoningToggles[j].isChecked = false;
                    ((UICheckBox)c).isChecked = true;

                    eventFilteringChanged(this, 0);
                };
            }

            if (!PlatformService.IsDlcInstalled(SteamHelper.kAfterDLCAppID))
            {
                zoningToggles[(int) Category.CommercialLeisure].isVisible = false;
                zoningToggles[(int) Category.CommercialTourism].isVisible = false;
            }

            if (!PlatformService.IsDlcInstalled(SteamHelper.kGreenDLCAppID))
            {
                zoningToggles[(int)Category.ResidentialEco].isVisible = false;
                zoningToggles[(int)Category.CommercialEco].isVisible = false;
                zoningToggles[(int)Category.OfficeHightech].isVisible = false;
            }
            
            if (!PlatformService.IsDlcInstalled(SteamHelper.kPlazasAndPromenadesDLCAppID))
            {
                zoningToggles[(int)Category.ResidentialWallToWall].isVisible = false;
                zoningToggles[(int)Category.CommercialWallToWall].isVisible = false;
                zoningToggles[(int)Category.OfficeWallToWall].isVisible = false;
            }
            
            if (!PlatformService.IsDlcInstalled(SteamHelper.kFinancialDistrictsDLCAppID))
            {
                zoningToggles[(int)Category.OfficeFinancial].isVisible = false;
            }

            allZones = UIUtils.CreateButton(this);
            allZones.width = 40;
            allZones.text = "All";
            allZones.relativePosition = new Vector3(760, 5);

            allZones.eventClick += (c, p) =>
            {
                for (int i = 0; i < NumOfCategories; i++)
                {
                    zoningToggles[i].isChecked = true;
                }
                eventFilteringChanged(this, 0);
            };

            noZones = UIUtils.CreateButton(this);
            noZones.width = 55;
            noZones.text = "None";
            noZones.relativePosition = new Vector3(800, 5);

            noZones.eventClick += (c, p) =>
            {
                for (int i = 0; i < NumOfCategories; i++)
                {
                    zoningToggles[i].isChecked = false;
                }
                eventFilteringChanged(this, 0);
            };

            // Display
            UILabel display = AddUIComponent<UILabel>();
            display.textScale = 0.8f;
            display.padding = new RectOffset(0, 0, 8, 0);
            display.text = "Display: ";
            display.relativePosition = new Vector3(0, 40);

            origin = UIUtils.CreateDropDown(this);
            origin.width = 90;
            origin.AddItem("All");
            origin.AddItem("Default");
            origin.AddItem("Custom");
            origin.AddItem("Cloned");
            origin.selectedIndex = 0;
            origin.relativePosition = new Vector3(display.relativePosition.x + display.width + 5, 40);

            origin.eventSelectedIndexChanged += (c, i) => eventFilteringChanged(this, 1);

            status = UIUtils.CreateDropDown(this);
            status.width = 90;
            status.AddItem("All");
            status.AddItem("Included");
            status.AddItem("Excluded");
            status.selectedIndex = 0;
            status.relativePosition = new Vector3(origin.relativePosition.x + origin.width + 5, 40);

            status.eventSelectedIndexChanged += (c, i) => eventFilteringChanged(this, 2);

            // Level
            UILabel levelLabel = AddUIComponent<UILabel>();
            levelLabel.textScale = 0.8f;
            levelLabel.padding = new RectOffset(0, 0, 8, 0);
            levelLabel.text = "Level: ";
            levelLabel.relativePosition = new Vector3(status.relativePosition.x + status.width + 10, 40);

            levelFilter = UIUtils.CreateDropDown(this);
            levelFilter.width = 55;
            levelFilter.AddItem("All");
            levelFilter.AddItem("1");
            levelFilter.AddItem("2");
            levelFilter.AddItem("3");
            levelFilter.AddItem("4");
            levelFilter.AddItem("5");
            levelFilter.selectedIndex = 0;
            levelFilter.relativePosition = new Vector3(levelLabel.relativePosition.x + levelLabel.width + 5, 40);

            levelFilter.eventSelectedIndexChanged += (c, i) => eventFilteringChanged(this, 3);

            // Size
            UILabel sizeLabel = AddUIComponent<UILabel>();
            sizeLabel.textScale = 0.8f;
            sizeLabel.padding = new RectOffset(0, 0, 8, 0);
            sizeLabel.text = "Size: ";
            sizeLabel.relativePosition = new Vector3(levelFilter.relativePosition.x + levelFilter.width + 10, 40);

            sizeFilterX = UIUtils.CreateDropDown(this);
            sizeFilterX.width = 55;
            sizeFilterX.AddItem("All");
            sizeFilterX.AddItem("1");
            sizeFilterX.AddItem("2");
            sizeFilterX.AddItem("3");
            sizeFilterX.AddItem("4");
            sizeFilterX.selectedIndex = 0;
            sizeFilterX.relativePosition = new Vector3(sizeLabel.relativePosition.x + sizeLabel.width + 5, 40);

            UILabel XLabel = AddUIComponent<UILabel>();
            XLabel.textScale = 0.8f;
            XLabel.padding = new RectOffset(0, 0, 8, 0);
            XLabel.text = "X";
            XLabel.isVisible = false;
            XLabel.relativePosition = new Vector3(sizeFilterX.relativePosition.x + sizeFilterX.width - 5, 40);

            sizeFilterY = UIUtils.CreateDropDown(this);
            sizeFilterY.width = 45;
            sizeFilterY.AddItem("1");
            sizeFilterY.AddItem("2");
            sizeFilterY.AddItem("3");
            sizeFilterY.AddItem("4");
            sizeFilterY.selectedIndex = 0;
            sizeFilterY.isVisible = false;
            sizeFilterY.relativePosition = new Vector3(XLabel.relativePosition.x + XLabel.width + 5, 40);

            sizeFilterX.eventSelectedIndexChanged += (c, i) =>
            {
                if (i == 0)
                {
                    sizeFilterX.width = 55;
                    XLabel.isVisible = false;
                    sizeFilterY.isVisible = false;
                }
                else
                {
                    sizeFilterX.width = 45;
                    XLabel.isVisible = true;
                    sizeFilterY.isVisible = true;
                }

                eventFilteringChanged(this, 4);
            };

            sizeFilterY.eventSelectedIndexChanged += (c, i) => eventFilteringChanged(this, 4);

            // Source filter (DLC / Workshop / All)
            UILabel sourceLabel = AddUIComponent<UILabel>();
            sourceLabel.textScale = 0.8f;
            sourceLabel.padding = new RectOffset(0, 0, 8, 0);
            sourceLabel.text = "Source: ";
            sourceLabel.relativePosition = new Vector3(sizeFilterY.relativePosition.x + sizeFilterY.width + 10, 40);

            sourceFilter = UIUtils.CreateDropDown(this);
            sourceFilter.width = 130;
            sourceFilter.tooltip = "Filter buildings by origin: All, Workshop (Steam), a specific DLC, or Vanilla";
            sourceFilter.relativePosition = new Vector3(sourceLabel.relativePosition.x + sourceLabel.width + 5, 40);

            // Populate with just "All" initially; SetSourceOptions() fills it once a theme is selected.
            m_sourceKeys.Add(null);
            sourceFilter.AddItem("All");
            sourceFilter.selectedIndex = 0;

            sourceFilter.eventSelectedIndexChanged += (c, i) => eventFilteringChanged(this, 8);

            // Name filter
            UILabel nameLabel = AddUIComponent<UILabel>();
            nameLabel.textScale = 0.8f;
            nameLabel.padding = new RectOffset(0, 0, 8, 0);
            nameLabel.relativePosition = new Vector3(width - 250, 40);
            nameLabel.text = "Name: ";

            nameFilter = UIUtils.CreateTextField(this);
            nameFilter.width = 200;
            nameFilter.height = 30;
            nameFilter.padding = new RectOffset(6, 6, 6, 6);
            nameFilter.relativePosition = new Vector3(width - nameFilter.width, 40);

            nameFilter.eventTextChanged += (c, s) => eventFilteringChanged(this, 5);
            nameFilter.eventTextSubmitted += (c, s) => eventFilteringChanged(this, 5);

            // Row 3 (y=78): four checkboxes + counter label
            // "Show loaded" — hides buildings whose prefab is available
            m_showLoadedCb = MakeFilterCheckbox("Show loaded", 0, 78, true);
            m_showLoadedCb.tooltip = "Show buildings whose prefab is loaded and available";
            m_showLoadedCb.eventCheckChanged += (c, v) => { showLoaded = v; eventFilteringChanged(this, 6); };

            // "Show missing" — hides workshop/custom assets that failed to load
            m_showMissingCb = MakeFilterCheckbox("Show missing", 150, 78, true);
            m_showMissingCb.tooltip = "Show workshop/custom assets that are not currently loaded\n(not subscribed, disabled by Skyve, or load error)";
            m_showMissingCb.eventCheckChanged += (c, v) => { showMissing = v; eventFilteringChanged(this, 6); };

            // "Show DLC/Env" — hides assets gated by unowned DLC or wrong environment
            m_showDLCCb = MakeFilterCheckbox("Show DLC/Env", 300, 78, true);
            m_showDLCCb.tooltip = "Show vanilla/DLC assets not available\n(DLC not owned, or asset excluded for this map environment)";
            m_showDLCCb.eventCheckChanged += (c, v) => { showDLCLocked = v; eventFilteringChanged(this, 6); };

            // "Spawnable only" — show only loaded + included + valid-dimension buildings
            m_canSpawnCb = MakeFilterCheckbox("Spawnable only", 480, 78, false);
            m_canSpawnCb.tooltip = "Show only buildings that are loaded, included in the theme,\nand have cell dimensions (1–4) valid for zone spawning";
            m_canSpawnCb.eventCheckChanged += (c, v) => { canSpawnOnly = v; eventFilteringChanged(this, 7); };

        }

        private UICheckBox MakeFilterCheckbox(string label, float x, float y, bool checkedByDefault)
        {
            UICheckBox cb = UIUtils.CreateCheckBox(this);
            cb.width = 115;
            cb.height = 20;
            cb.clipChildren = false;
            cb.relativePosition = new Vector3(x, y);
            cb.label.text = label;
            cb.label.textScale = 0.8f;
            cb.isChecked = checkedByDefault;
            return cb;
        }
    }
}
