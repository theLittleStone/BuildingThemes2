using System;
using System.Collections.Generic;
using ColossalFramework.Globalization;
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
        public UIDropDown dlcFilter;
        public UITextField nameFilter;

        // Parallel list to dlcFilter.items.  Index 0 = "All DLC" (both masks None).
        private struct DlcEntry
        {
            public SteamHelper.ExpansionBitMask expansion;
            public SteamHelper.ModderPackBitMask modderPack;
        }
        private readonly List<DlcEntry> m_dlcEntries = new List<DlcEntry>();

        /// <summary>Active expansion DLC filter; None = no filter.</summary>
        public SteamHelper.ExpansionBitMask dlcExpansionFilter
        {
            get
            {
                int idx = dlcFilter == null ? 0 : dlcFilter.selectedIndex;
                return (idx > 0 && idx < m_dlcEntries.Count) ? m_dlcEntries[idx].expansion : SteamHelper.ExpansionBitMask.None;
            }
        }

        /// <summary>Active modder-pack DLC filter; None = no filter.</summary>
        public SteamHelper.ModderPackBitMask dlcModderPackFilter
        {
            get
            {
                int idx = dlcFilter == null ? 0 : dlcFilter.selectedIndex;
                return (idx > 0 && idx < m_dlcEntries.Count) ? m_dlcEntries[idx].modderPack : SteamHelper.ModderPackBitMask.None;
            }
        }

        /// <summary>
        /// Rebuild the DLC dropdown from the building items of the currently selected theme.
        /// Only DLCs that are (a) owned and (b) have at least one loaded building in the
        /// theme are shown.  Index 0 is always "All DLC".
        /// </summary>
        public void SetDlcOptions(List<BuildingItem> buildings)
        {
            if (dlcFilter == null) return;

            // Remember current selection by value so we can restore it after rebuild.
            int prevIdx = dlcFilter.selectedIndex;
            DlcEntry prevEntry = (prevIdx > 0 && prevIdx < m_dlcEntries.Count)
                ? m_dlcEntries[prevIdx]
                : new DlcEntry();

            var ownedExp  = SteamHelper.GetOwnedExpansionMask();
            var ownedPack = SteamHelper.GetOwnedModderPackMask();

            // Collect unique DLC entries that have buildings in this theme.
            var seen    = new HashSet<ulong>();
            var entries = new List<DlcEntry>();
            entries.Add(new DlcEntry()); // index 0 = "All DLC"
            seen.Add(0UL);

            foreach (BuildingItem item in buildings)
            {
                if (item.prefab == null) continue;

                var exp  = item.prefab.m_requiredExpansion;
                var pack = item.prefab.m_requiredModderPack;

                if (exp != SteamHelper.ExpansionBitMask.None && (ownedExp & exp) != SteamHelper.ExpansionBitMask.None)
                {
                    ulong key = (ulong)(int)exp;
                    if (seen.Add(key))
                        entries.Add(new DlcEntry { expansion = exp, modderPack = SteamHelper.ModderPackBitMask.None });
                }

                if (pack != SteamHelper.ModderPackBitMask.None && (ownedPack & pack) != SteamHelper.ModderPackBitMask.None)
                {
                    ulong key = 0x1_0000_0000UL + (ulong)(int)pack;
                    if (seen.Add(key))
                        entries.Add(new DlcEntry { expansion = SteamHelper.ExpansionBitMask.None, modderPack = pack });
                }
            }

            m_dlcEntries.Clear();
            m_dlcEntries.AddRange(entries);

            string[] items = new string[entries.Count];
            items[0] = "All DLC";
            for (int i = 1; i < entries.Count; i++)
                items[i] = entries[i].expansion != SteamHelper.ExpansionBitMask.None
                    ? GetExpansionName(entries[i].expansion)
                    : GetModderPackName(entries[i].modderPack);

            dlcFilter.items = items;

            // Restore previous selection if same DLC is still present.
            int restored = 0;
            for (int i = 1; i < entries.Count; i++)
            {
                if (entries[i].expansion == prevEntry.expansion && entries[i].modderPack == prevEntry.modderPack)
                { restored = i; break; }
            }
            dlcFilter.selectedIndex = restored;
        }

        private static string GetExpansionName(SteamHelper.ExpansionBitMask mask)
        {
            if (mask == SteamHelper.ExpansionBitMask.AfterDark)            return "After Dark";
            if (mask == SteamHelper.ExpansionBitMask.SnowFall)             return "Snowfall";
            if (mask == SteamHelper.ExpansionBitMask.NaturalDisasters)     return "Natural Disasters";
            if (mask == SteamHelper.ExpansionBitMask.InMotion)             return "Mass Transit";
            if (mask == SteamHelper.ExpansionBitMask.GreenCities)          return "Green Cities";
            if (mask == SteamHelper.ExpansionBitMask.Parks)                return "Parklife";
            if (mask == SteamHelper.ExpansionBitMask.Industry)             return "Industries";
            if (mask == SteamHelper.ExpansionBitMask.Campus)               return "Campus";
            if (mask == SteamHelper.ExpansionBitMask.SunsetHarbor)         return "Sunset Harbor";
            if (mask == SteamHelper.ExpansionBitMask.Airport)              return "Airports";
            if (mask == SteamHelper.ExpansionBitMask.PlazasAndPromenades)  return "Plazas & Promenades";
            if (mask == SteamHelper.ExpansionBitMask.FinancialDistricts)   return "Financial Districts";
            if (mask == SteamHelper.ExpansionBitMask.Hotel)                return "Hotels & Retreats";
            if (mask == SteamHelper.ExpansionBitMask.RacesAndParades)      return "Hubs & Transport";
            return "Expansion DLC";
        }

        // Maps ModderPackBitMask → locale key (mirrors BuildingThemesManager.s_styleLocaleKeys).
        private static readonly Dictionary<SteamHelper.ModderPackBitMask, string> s_packLocale =
            new Dictionary<SteamHelper.ModderPackBitMask, string>
        {
            { SteamHelper.ModderPackBitMask.Pack5,  "STYLES_MODDERPACKFIVE"      },
            { SteamHelper.ModderPackBitMask.Pack11, "STYLES_MODDERPACKELEVEN"    },
            { SteamHelper.ModderPackBitMask.Pack14, "STYLES_MODDERPACKFOURTEEN"  },
            { SteamHelper.ModderPackBitMask.Pack16, "STYLES_MODDERPACKSIXTEEN"   },
            { SteamHelper.ModderPackBitMask.Pack18, "STYLES_MODDERPACKEIGHTEEN"  },
            { SteamHelper.ModderPackBitMask.Pack20, "STYLES_MODDERPACKTWENTY"    },
            { SteamHelper.ModderPackBitMask.Pack21, "STYLES_MODDERPACKTWENTYONE" },
            { SteamHelper.ModderPackBitMask.Pack24, "STYLES_MODDERPACKTWENTYFOUR"},
            { SteamHelper.ModderPackBitMask.Pack25, "STYLES_MODDERPACKTWENTYFIVE"},
            { SteamHelper.ModderPackBitMask.Pack26, "STYLES_MODDERPACKTWENTYSIX" },
        };

        private static string GetModderPackName(SteamHelper.ModderPackBitMask mask)
        {
            string localeKey;
            if (s_packLocale.TryGetValue(mask, out localeKey))
            {
                try
                {
                    string name = Locale.Get(localeKey);
                    if (!string.IsNullOrEmpty(name)) return name;
                }
                catch { }
            }
            return "Content Creator Pack";
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

            // DLC filter — no label, positioned right after the size filters.
            // sizeFilterX is at ~390; allow 120px for both sizeFilterX+Y (45+10+45) + gap.
            dlcFilter = UIUtils.CreateDropDown(this);
            dlcFilter.width = 120;
            dlcFilter.tooltip = "Filter by DLC / Content Creator Pack.\nOnly DLCs installed and present in this theme are listed.";
            dlcFilter.relativePosition = new Vector3(sizeFilterX.relativePosition.x + 115 + 10, 40);

            m_dlcEntries.Add(new DlcEntry()); // index 0 = "All DLC"
            dlcFilter.AddItem("All DLC");
            dlcFilter.selectedIndex = 0;

            dlcFilter.eventSelectedIndexChanged += (c, i) => eventFilteringChanged(this, 8);

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
