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
        public UIDropDown themeFilter;
        public UITextField nameFilter;

        public float minHeight = 0f;
        public float maxHeight = 0f;

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

        // Hardcoded CCP names for all 29 packs (extracted from game DLL strings).
        // For packs that also have locale keys we try locale first for localization support.
        private static readonly Dictionary<SteamHelper.ModderPackBitMask, string> s_packNames =
            new Dictionary<SteamHelper.ModderPackBitMask, string>
        {
            { SteamHelper.ModderPackBitMask.Pack1,  "Art Deco"               },
            { SteamHelper.ModderPackBitMask.Pack2,  "High-Tech Buildings"    },
            { SteamHelper.ModderPackBitMask.Pack3,  "European Suburbia"      },
            { SteamHelper.ModderPackBitMask.Pack4,  "University City"        },
            { SteamHelper.ModderPackBitMask.Pack5,  "Modern City Center"     },
            { SteamHelper.ModderPackBitMask.Pack6,  "Modern Japan"           },
            { SteamHelper.ModderPackBitMask.Pack7,  "Train Stations"         },
            { SteamHelper.ModderPackBitMask.Pack8,  "Bridges & Piers"        },
            { SteamHelper.ModderPackBitMask.Pack9,  "Maps"                   },
            { SteamHelper.ModderPackBitMask.Pack10, "Vehicles of the World"  },
            { SteamHelper.ModderPackBitMask.Pack11, "Mid Century"            },
            { SteamHelper.ModderPackBitMask.Pack12, "Seaside Resorts"        },
            { SteamHelper.ModderPackBitMask.Pack13, "Skyscrapers"            },
            { SteamHelper.ModderPackBitMask.Pack14, "Heart of Korea"         },
            { SteamHelper.ModderPackBitMask.Pack15, "Maps 2"                 },
            { SteamHelper.ModderPackBitMask.Pack16, "Shopping Malls"         },
            { SteamHelper.ModderPackBitMask.Pack17, "Sports Venues"          },
            { SteamHelper.ModderPackBitMask.Pack18, "Africa in Miniature"    },
            { SteamHelper.ModderPackBitMask.Pack19, "Railroads of Japan"     },
            { SteamHelper.ModderPackBitMask.Pack20, "Industrial Evolution"   },
            { SteamHelper.ModderPackBitMask.Pack21, "Brooklyn and Queens"    },
            { SteamHelper.ModderPackBitMask.Pack22, "Mountain Village"       },
            { SteamHelper.ModderPackBitMask.Pack23, "Maps 3"                 },
            { SteamHelper.ModderPackBitMask.Pack24, "Countryside"            },
            { SteamHelper.ModderPackBitMask.Pack25, "Emerging Downtown"      },
            { SteamHelper.ModderPackBitMask.Pack26, "Shops of Shibuya"       },
            { SteamHelper.ModderPackBitMask.Pack27, "Maps 4"                 },
            { SteamHelper.ModderPackBitMask.Pack28, "Iconic Brutalism"       },
            { SteamHelper.ModderPackBitMask.Pack29, "Renewed History"        },
        };

        // Locale keys for packs that have them (allows in-game language override).
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
            // Try locale key first for language support.
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
            // Fall back to hardcoded English name.
            string hardcoded;
            if (s_packNames.TryGetValue(mask, out hardcoded))
                return hardcoded;
            return "CCP";
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

        // Each component is 0 when "All" is selected, or 1-4 for a specific size.
        public Vector2 buildingSize
        {
            get { return new Vector2(sizeFilterX.selectedIndex, sizeFilterY.selectedIndex); }
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

        // Canonical theme names parallel to themeFilter.items (index 0 = null for "In any theme").
        private readonly List<string> m_themeFilterNames = new List<string>();

        /// <summary>Returns the canonical theme name to filter by, or null when "In any theme" is selected.</summary>
        public string themeFilterName
        {
            get
            {
                int idx = themeFilter == null ? 0 : themeFilter.selectedIndex;
                return (idx > 0 && idx < m_themeFilterNames.Count) ? m_themeFilterNames[idx] : null;
            }
        }

        /// <summary>
        /// Rebuilds the theme filter dropdown showing displayName labels (with [DLC]/[Vanilla]/[Custom]
        /// prefixes). Excludes the currently-displayed theme. Canonical names are kept in a parallel
        /// list so GetThemeByName lookups still work.
        /// </summary>
        public void SetThemeOptions(List<Configuration.Theme> themes, string currentThemeName)
        {
            if (themeFilter == null) return;

            string prev = themeFilterName; // canonical name before rebuild

            m_themeFilterNames.Clear();
            m_themeFilterNames.Add(null); // index 0 = "In any theme"

            themeFilter.items = new string[0];
            themeFilter.AddItem("In any theme");
            foreach (var t in themes)
            {
                if (t.name == currentThemeName) continue;
                themeFilter.AddItem(t.displayName);
                m_themeFilterNames.Add(t.name);
            }

            // Restore previous selection by canonical name.
            int restored = 0;
            if (prev != null)
            {
                for (int i = 1; i < m_themeFilterNames.Count; i++)
                {
                    if (m_themeFilterNames[i] == prev) { restored = i; break; }
                }
            }
            themeFilter.selectedIndex = restored;
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
            sizeFilterX.width = 60;
            sizeFilterX.AddItem("All");
            sizeFilterX.AddItem("1");
            sizeFilterX.AddItem("2");
            sizeFilterX.AddItem("3");
            sizeFilterX.AddItem("4");
            sizeFilterX.selectedIndex = 0;
            sizeFilterX.relativePosition = new Vector3(sizeLabel.relativePosition.x + sizeLabel.width + 5, 40);

            UILabel XLabel = AddUIComponent<UILabel>();
            XLabel.textScale = 0.8f;
            XLabel.autoSize = true;
            XLabel.padding = new RectOffset(3, 3, 8, 0);
            XLabel.text = "X";
            XLabel.relativePosition = new Vector3(sizeFilterX.relativePosition.x + sizeFilterX.width, 40);

            sizeFilterY = UIUtils.CreateDropDown(this);
            sizeFilterY.width = 60;
            sizeFilterY.AddItem("All");
            sizeFilterY.AddItem("1");
            sizeFilterY.AddItem("2");
            sizeFilterY.AddItem("3");
            sizeFilterY.AddItem("4");
            sizeFilterY.selectedIndex = 0;
            sizeFilterY.relativePosition = new Vector3(XLabel.relativePosition.x + XLabel.width + 5, 40);

            sizeFilterX.eventSelectedIndexChanged += (c, i) => eventFilteringChanged(this, 4);
            sizeFilterY.eventSelectedIndexChanged += (c, i) => eventFilteringChanged(this, 4);

            // DLC filter — left half of the row; height filter occupies the right half.
            float halfWidth = (width - 5) / 2f;
            dlcFilter = UIUtils.CreateDropDown(this);
            dlcFilter.width = halfWidth;
            dlcFilter.tooltip = "Filter by DLC / Content Creator Pack.\nOnly DLCs that are installed and have buildings in this theme are shown.";
            dlcFilter.relativePosition = new Vector3(0, 74);

            m_dlcEntries.Add(new DlcEntry()); // index 0 = "All DLC"
            dlcFilter.AddItem("All DLC");
            dlcFilter.selectedIndex = 0;

            dlcFilter.eventSelectedIndexChanged += (c, i) => eventFilteringChanged(this, 8);

            // Height filter — right half of the DLC row.
            UILabel heightLabel = AddUIComponent<UILabel>();
            heightLabel.textScale = 0.8f;
            heightLabel.padding = new RectOffset(0, 0, 8, 0);
            heightLabel.text = "Height:";
            heightLabel.relativePosition = new Vector3(halfWidth + 8, 74);

            UITextField heightMin = UIUtils.CreateTextField(this);
            heightMin.width = 48;
            heightMin.height = 28;
            heightMin.padding = new RectOffset(4, 4, 6, 4);
            heightMin.tooltip = "Minimum building height in metres (leave blank for no limit)";
            heightMin.relativePosition = new Vector3(heightLabel.relativePosition.x + heightLabel.width + 3, 74);

            UILabel heightSep = AddUIComponent<UILabel>();
            heightSep.textScale = 0.8f;
            heightSep.padding = new RectOffset(0, 0, 8, 0);
            heightSep.text = "–";
            heightSep.relativePosition = new Vector3(heightMin.relativePosition.x + heightMin.width + 2, 74);

            UITextField heightMax = UIUtils.CreateTextField(this);
            heightMax.width = 48;
            heightMax.height = 28;
            heightMax.padding = new RectOffset(4, 4, 6, 4);
            heightMax.tooltip = "Maximum building height in metres (leave blank for no limit)";
            heightMax.relativePosition = new Vector3(heightSep.relativePosition.x + heightSep.width + 2, 74);

            heightMin.eventTextChanged += (c, s) =>
            {
                float v;
                minHeight = float.TryParse(s, out v) && v > 0 ? v : 0f;
                eventFilteringChanged(this, 9);
            };
            heightMax.eventTextChanged += (c, s) =>
            {
                float v;
                maxHeight = float.TryParse(s, out v) && v > 0 ? v : 0f;
                eventFilteringChanged(this, 9);
            };

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

            // Row 4 (y=108): theme filter — filter buildings by membership in another theme
            UILabel themeLabel = AddUIComponent<UILabel>();
            themeLabel.textScale = 0.8f;
            themeLabel.padding = new RectOffset(0, 0, 8, 0);
            themeLabel.text = "Also in:";
            themeLabel.relativePosition = new Vector3(0, 108);

            themeFilter = UIUtils.CreateDropDown(this);
            themeFilter.width = width - themeLabel.width - 8;
            themeFilter.tooltip = "Show only buildings that are also included in the selected theme.\nUseful for comparing or merging themes.";
            themeFilter.relativePosition = new Vector3(themeLabel.width + 8, 108);
            themeFilter.AddItem("In any theme");
            themeFilter.selectedIndex = 0;

            themeFilter.eventSelectedIndexChanged += (c, i) => eventFilteringChanged(this, 10);

            // Row 5 (y=142): four checkboxes + counter label
            // "Show loaded" — hides buildings whose prefab is available
            m_showLoadedCb = MakeFilterCheckbox("Show loaded", 0, 142, true);
            m_showLoadedCb.tooltip = "Show buildings whose prefab is loaded and available";
            m_showLoadedCb.eventCheckChanged += (c, v) => { showLoaded = v; eventFilteringChanged(this, 6); };

            // "Show missing" — hides workshop/custom assets that failed to load
            m_showMissingCb = MakeFilterCheckbox("Show missing", 150, 142, true);
            m_showMissingCb.tooltip = "Show workshop/custom assets that are not currently loaded\n(not subscribed, disabled by Skyve, or load error)";
            m_showMissingCb.eventCheckChanged += (c, v) => { showMissing = v; eventFilteringChanged(this, 6); };

            // "Show DLC/Env" — hides assets gated by unowned DLC or wrong environment
            m_showDLCCb = MakeFilterCheckbox("Show DLC/Env", 300, 142, true);
            m_showDLCCb.tooltip = "Show vanilla/DLC assets not available\n(DLC not owned, or asset excluded for this map environment)";
            m_showDLCCb.eventCheckChanged += (c, v) => { showDLCLocked = v; eventFilteringChanged(this, 6); };

            // "Spawnable only" — show only loaded + included + valid-dimension buildings
            m_canSpawnCb = MakeFilterCheckbox("Spawnable only", 480, 142, false);
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
