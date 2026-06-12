using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildingThemes.GUI
{
    public enum Origin
    {
        All,
        Default,
        Custom,
        Cloned
    }

    public enum Status
    {
        All,
        Included,
        Excluded
    }

    /// <summary>Asset-loading status for a BuildingItem, independent of theme inclusion.</summary>
    public enum AssetStatus
    {
        All,
        Available,   // prefab != null (loaded)
        Missing,     // workshop/custom asset whose prefab couldn't be loaded
        DLCLocked,   // vanilla or DLC asset not available (DLC not owned, wrong env)
    }

    public enum Category
    {
        None = -1,
        ResidentialLow = 0,
        ResidentialHigh,
        ResidentialEco, // gc
        CommercialLow,
        CommercialHigh,
        CommercialLeisure,
        CommercialTourism,
        CommercialEco, // gc
        Industrial,
        Farming,
        Forestry,
        Oil,
        Ore,
        Office,
        OfficeHightech, // gc
        ResidentialWallToWall, //pp
        CommercialWallToWall, //pp
        OfficeWallToWall, //pp
        OfficeFinancial //fd
    }

    public class CategoryIcons
    {

        public static readonly string[] atlases = {
            "Thumbnails",
            "Thumbnails",
            "Thumbnails", // gc
            "Thumbnails",
            "Thumbnails",
            "Thumbnails",
            "Thumbnails",
            "Thumbnails", // gc
            "Thumbnails",
            "Ingame",
            "Ingame",
            "Ingame",
            "Ingame",
            "Thumbnails",
            "Thumbnails", //gc
            "Thumbnails",
            "Thumbnails",
            "Thumbnails",
            "Thumbnails"
        };

        public static readonly string[] spriteNames = {
            "ZoningResidentialLow",
            "ZoningResidentialHigh",
            "DistrictSpecializationSelfsufficient",
            "ZoningCommercialLow",
            "ZoningCommercialHigh",
            "DistrictSpecializationLeisure",
            "DistrictSpecializationTourist",
            "DistrictSpecializationOrganic",
            "ZoningIndustrial",
            "IconPolicyFarming",
            "IconPolicyForest",
            "IconPolicyOil",
            "IconPolicyOre",
            "ZoningOffice",
            "DistrictSpecializationHightech",
            "DistrictSpecializationResidentialWallToWall",
            "DistrictSpecializationCommercialWallToWall",
            "DistrictSpecializationOfficeWallToWall",
            "DistrictSpecializationFinancial"
        };

        public static string[] tooltips
        {
            get
            {
                return new string[] {
                    Localization.Get("CATEGORY_RESIDENTIAL_LOW"),
                    Localization.Get("CATEGORY_RESIDENTIAL_HIGH"),
                    Localization.Get("CATEGORY_RESIDENTIAL_ECO"),
                    Localization.Get("CATEGORY_COMMERCIAL_LOW"),
                    Localization.Get("CATEGORY_COMMERCIAL_HIGH"),
                    Localization.Get("CATEGORY_COMMERCIAL_LEISURE"),
                    Localization.Get("CATEGORY_COMMERCIAL_TOURISM"),
                    Localization.Get("CATEGORY_COMMERCIAL_ECO"),
                    Localization.Get("CATEGORY_INDUSTRIAL"),
                    Localization.Get("CATEGORY_FARMING"),
                    Localization.Get("CATEGORY_FORESTRY"),
                    Localization.Get("CATEGORY_OIL"),
                    Localization.Get("CATEGORY_ORE"),
                    Localization.Get("CATEGORY_OFFICE"),
                    Localization.Get("CATEGORY_OFFICE_HIGHTECH"),
                    Localization.Get("CATEGORY_RESIDENTIAL_W2W"),
                    Localization.Get("CATEGORY_COMMERCIAL_W2W"),
                    Localization.Get("CATEGORY_OFFICE_W2W"),
                    Localization.Get("CATEGORY_OFFICE_FINANCIAL")
                };
            }
        }
    }
}
