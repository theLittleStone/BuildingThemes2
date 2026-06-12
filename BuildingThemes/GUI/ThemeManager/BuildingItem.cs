using System.Text.RegularExpressions;
using System.Globalization;

using UnityEngine;
using ColossalFramework;
using ColossalFramework.Globalization;

namespace BuildingThemes.GUI
{
    public class BuildingItem
    {
        private string m_name;
        private string m_displayName;
        private string m_steamID;
        private int m_level = -1;
        private Vector2 m_size;

        public BuildingInfo prefab;
        public Configuration.Building building;

        public bool included
        {
            get { return building != null && building.include; }
        }

        public string name
        {
            get
            {
                if (m_name == null)
                {
                    if (prefab != null) m_name = prefab.name;
                    else if (building != null) m_name = building.name;
                    else m_name = string.Empty;
                }
                return m_name;
            }
        }

        public string displayName
        {
            get
            {
                if (m_displayName != null) return m_displayName;

                m_displayName = Locale.GetUnchecked("BUILDING_TITLE", name);
                if (m_displayName.StartsWith("BUILDING_TITLE"))
                {
                    m_displayName = name.Substring(name.IndexOf('.') + 1).Replace("_Data", "");
                }
                m_displayName = CleanName(m_displayName, !name.Contains("."));

                return m_displayName;
            }
        }

        public Category category
        {
            get
            {
                if (prefab == null || prefab.m_class == null) return Category.None;

                ItemClass itemClass = prefab.m_class;
                if (itemClass.m_subService == ItemClass.SubService.ResidentialLow) return Category.ResidentialLow;
                if (itemClass.m_subService == ItemClass.SubService.ResidentialHigh) return Category.ResidentialHigh;
                if (itemClass.m_subService == ItemClass.SubService.ResidentialLowEco) return Category.ResidentialEco;
                if (itemClass.m_subService == ItemClass.SubService.ResidentialHighEco) return Category.ResidentialEco;
                if (itemClass.m_subService == ItemClass.SubService.CommercialLow) return Category.CommercialLow;
                if (itemClass.m_subService == ItemClass.SubService.CommercialHigh) return Category.CommercialHigh;
                if (itemClass.m_subService == ItemClass.SubService.CommercialLeisure) return Category.CommercialLeisure;
                if (itemClass.m_subService == ItemClass.SubService.CommercialTourist) return Category.CommercialTourism;
                if (itemClass.m_subService == ItemClass.SubService.CommercialEco) return Category.CommercialEco;
                if (itemClass.m_subService == ItemClass.SubService.IndustrialGeneric) return Category.Industrial;
                if (itemClass.m_subService == ItemClass.SubService.IndustrialFarming) return Category.Farming;
                if (itemClass.m_subService == ItemClass.SubService.IndustrialForestry) return Category.Forestry;
                if (itemClass.m_subService == ItemClass.SubService.IndustrialOil) return Category.Oil;
                if (itemClass.m_subService == ItemClass.SubService.IndustrialOre) return Category.Ore;
                if (itemClass.m_subService == ItemClass.SubService.OfficeGeneric) return Category.Office;
                if (itemClass.m_subService == ItemClass.SubService.OfficeHightech) return Category.OfficeHightech;
                if (itemClass.m_subService == ItemClass.SubService.ResidentialWallToWall) return Category.ResidentialWallToWall;
                if (itemClass.m_subService == ItemClass.SubService.CommercialWallToWall) return Category.CommercialWallToWall;
                if (itemClass.m_subService == ItemClass.SubService.OfficeWallToWall) return Category.OfficeWallToWall;
                if (itemClass.m_subService == ItemClass.SubService.OfficeFinancial) return Category.OfficeFinancial;
                return Category.None;
            }
        }

        public int level
        {
            get
            {
                if(m_level == -1)
                {
                    m_level = 0;
                    if (prefab != null && prefab.m_class != null)
                    {
                        m_level = (int)prefab.m_class.m_level + 1;
                    }
                    else
                    {
                        string cleanName = Regex.Replace(name, @"^{{.*?}}\.", "");
                        int.TryParse(Regex.Match(cleanName, @"(?<=[HL])\d").Value, out m_level);
                    }
                }
                return m_level;
            }
        }

        public int maxLevel
        {
            get
            {
                switch (category)
                {
                    case Category.None:
                    case Category.ResidentialHigh:
                    case Category.ResidentialLow:
                    case Category.ResidentialEco:
                    case Category.ResidentialWallToWall:
                        return 5;
                    case Category.Farming:
                    case Category.Forestry:
                    case Category.Oil:
                    case Category.Ore:
                    case Category.CommercialLeisure:
                    case Category.CommercialTourism:
                    case Category.OfficeHightech:
                    case Category.CommercialEco:
                        return 1;
                }

                return 3;
            }
        }

        public Vector2 size
        {
            get
            {
                if (m_size == Vector2.zero)
                {
                    if (prefab != null)
                    {
                        m_size = new Vector2(prefab.m_cellWidth, prefab.m_cellLength);
                    }
                    else
                    {
                        string cleanName = Regex.Replace(name, @"^{{.*?}}\.", "");
                        string size = Regex.Match(cleanName, @"\d[xX]\d").Value.ToLower();
                        if(!size.IsNullOrWhiteSpace())
                        {
                            string[] splitSize = size.Split('x');

                            int x, y;
                            int.TryParse(splitSize[0], out x);
                            int.TryParse(splitSize[0], out y);
                            m_size = new Vector2(x, y);
                        }
                    }
                }
                return m_size;
            }
        }

        // Returns the building's maximum height in metres, or -1 if unavailable (unloaded prefab).
        public float height
        {
            get
            {
                if (prefab == null) return -1f;
                var gen = prefab.m_generatedInfo;
                if (gen == null || gen.m_heights == null || gen.m_heights.Length == 0) return -1f;
                float max = 0f;
                for (int i = 0; i < gen.m_heights.Length; i++)
                    if (gen.m_heights[i] > max) max = gen.m_heights[i];
                return max;
            }
        }

        public string sizeAsString
        {
            get
            {
                if (size == Vector2.zero) return "";
                return size.x + "x" + size.y;
            }
        }

        public string steamID
        {
            get
            {
                if (m_steamID != null) return m_steamID;

                if (isCustomAsset)
                {
                    m_steamID = name.Substring(0, name.IndexOf("."));

                    ulong result;
                    if (!ulong.TryParse(m_steamID, out result) || result == 0)
                        m_steamID = null;
                }

                return m_steamID;
            }
        }

        public bool isCloned
        {
            get
            {
                if (building == null) return false;

                return building.baseName != null;
            }
        }

        /// <summary>Asset loading status, independent of theme inclusion.</summary>
        public AssetStatus assetStatus
        {
            get
            {
                if (prefab != null) return AssetStatus.Available;
                if (!isCustomAsset) return AssetStatus.DLCLocked;
                return AssetStatus.Missing;
            }
        }

        /// <summary>
        /// True when the building is included in the theme, is loaded, and has cell dimensions
        /// that map to a valid spawn area bucket (1–4 cells in both width and length).
        /// </summary>
        public bool canSpawn
        {
            get
            {
                if (prefab == null || !included) return false;
                return prefab.m_cellWidth >= 1 && prefab.m_cellWidth <= 4
                    && prefab.m_cellLength >= 1 && prefab.m_cellLength <= 4;
            }
        }

        // Cached result — computed once on first access, null until then.
        private bool? m_isWallToWall;

        /// <summary>
        /// True when the building is designed to be placed flush against its neighbours
        /// with no meaningful side gaps (wall-to-wall). Detected by three signals:
        ///   1. Official DLC WTW sub-services (ResidentialWallToWall etc.) — authoritative.
        ///   2. ZoningMode is CornerLeft or CornerRight — always wall-to-wall by design.
        ///   3. Mesh vertex analysis for legacy straight buildings: sample the zone
        ///      4–8 m (above typical garages and garden arches) and verify that every
        ///      occupied 1 m band has both left and right gaps ≤ 0.3 m.
        ///      In CS1 the lot is centred at origin:
        ///        left  lot edge = -(m_cellWidth × 4) m
        ///        right lot edge = +(m_cellWidth × 4) m
        ///        left  gap = wallMinX + halfCellW   (0 = flush, >0 = inset)
        ///        right gap = halfCellW − wallMaxX   (0 = flush, >0 = inset)
        /// Result is cached after first evaluation.
        /// </summary>
        public bool isWallToWall
        {
            get
            {
                if (!m_isWallToWall.HasValue)
                    m_isWallToWall = ComputeIsWallToWall();
                return m_isWallToWall.Value;
            }
        }

        // Wall-quality thresholds shared by the strict and lenient passes.
        //
        // A real wall-to-wall building has, on EACH side, vertices near the lot edge that:
        //   (a) span ≥ minCubeDepth metres in Z — a true side wall runs the building's depth;
        //       a pergola column, arch post, or half-cylinder's front-corner edge spans almost
        //       no Z and gets rejected.
        //   (b) span ≥ minWallHeight metres in Y — a "cube base" is enough. A pyramid or
        //       sloped prism whose sides immediately step inward as Y rises only has
        //       edge-proximate vertices for <2 m of height and gets rejected.
        //
        // The Y-range check lets us follow the user's rule: if the building has a cube base
        // tall enough, the roof can be anything (prism, dome, pyramid) — that's still WTW.
        // If the building is purely sloped from the ground up, the cube base is absent → not WTW.
        private const float WTW_WallYMin      = 1.5f;  // above base slab; includes garage level
        private const float WTW_WallYMax      = 8.0f;
        private const float WTW_BandSize      = 1.0f;
        private const float WTW_MinCubeDepth  = 3.0f;  // Z-extent required for a "real wall" vs thin arch/post
        private const float WTW_MinWallHeight = 2.0f;  // wall must be present across ≥2 m of Y range
        private const float WTW_StrictGap     = 0.3f;  // flush against lot edge
        private const float WTW_LenientGap    = 1.3f;  // thick masonry / modelling offset tolerance

        private bool ComputeIsWallToWall()
        {
            if (prefab == null) return false;

            // Official DLC wall-to-wall sub-services (Financial Districts+) are authoritative.
            var sub = prefab.m_class.m_subService;
            if (sub == ItemClass.SubService.ResidentialWallToWall
             || sub == ItemClass.SubService.CommercialWallToWall
             || sub == ItemClass.SubService.OfficeWallToWall)
                return true;

            if (prefab.m_zoningMode != BuildingInfo.ZoningMode.Straight) return true;
            var mesh = prefab.m_mesh;
            if (mesh == null) return false;

            float halfCellW = prefab.m_cellWidth * 4f;
            bool log = Debugger.Enabled;

            var verts = mesh.vertices;
            if (verts == null || verts.Length == 0)
            {
                float leftGap  = mesh.bounds.min.x + halfCellW;
                float rightGap = halfCellW - mesh.bounds.max.x;
                bool r         = leftGap <= WTW_StrictGap && rightGap <= WTW_StrictGap;
                if (log)
                    Debugger.LogFormat("WTW [{0}] no verts, BOUNDS leftGap={1:F2} rightGap={2:F2} -> {3}",
                        name, leftGap, rightGap, r ? "WTW" : "NOT-WTW");
                return r;
            }

            // Two-pass approach: try a strict gap (walls flush against the lot edge) first,
            // then fall back to a lenient gap (walls inset by up to ~1.3 m) for buildings with
            // thick masonry or modelling offset. Both passes apply identical Z-span and
            // Y-range guards — that's what rejects prisms, half-cylinders, pyramids, and
            // garden walls that the old bounds-only fallback used to wave through.
            if (DetectWallsAtGap(verts, halfCellW, WTW_StrictGap, "STRICT", log))
                return true;
            return DetectWallsAtGap(verts, halfCellW, WTW_LenientGap, "LENIENT", log);
        }

        // Per-band detection at a given lot-edge gap tolerance.
        // For each 1 m Y band in [wallYMin, wallYMax) on each side, finds vertices within
        // `gap` of the lot edge and measures their Z-extent. Tracks the maximum Z-span seen
        // across all bands (so L-shaped/stepped buildings with their left wall at one Y range
        // and right wall at another still count), plus the actual Y range of edge-proximate
        // vertices on each side (so a pyramid whose only edge vertices are at its base gets
        // rejected by the wall-height guard).
        private bool DetectWallsAtGap(Vector3[] verts, float halfCellW, float gap, string tag, bool log)
        {
            float maxLspan = 0f, maxRspan = 0f;
            float leftYMin  = float.MaxValue, leftYMax  = float.MinValue;
            float rightYMin = float.MaxValue, rightYMax = float.MinValue;
            bool  anyEdge  = false;

            for (float bandLo = WTW_WallYMin; bandLo < WTW_WallYMax; bandLo += WTW_BandSize)
            {
                float bandHi = Mathf.Min(bandLo + WTW_BandSize, WTW_WallYMax);

                float leftMinZ  = float.MaxValue, leftMaxZ  = float.MinValue;
                float rightMinZ = float.MaxValue, rightMaxZ = float.MinValue;
                bool  leftFound = false,          rightFound = false;

                for (int i = 0; i < verts.Length; i++)
                {
                    float y = verts[i].y;
                    if (y < bandLo || y >= bandHi) continue;
                    float x = verts[i].x;
                    float z = verts[i].z;

                    if (x >= -halfCellW - gap && x <= -halfCellW + gap)
                    {
                        leftFound = true;
                        if (z < leftMinZ) leftMinZ = z;
                        if (z > leftMaxZ) leftMaxZ = z;
                        if (y < leftYMin) leftYMin = y;
                        if (y > leftYMax) leftYMax = y;
                    }
                    if (x >= halfCellW - gap && x <= halfCellW + gap)
                    {
                        rightFound = true;
                        if (z < rightMinZ) rightMinZ = z;
                        if (z > rightMaxZ) rightMaxZ = z;
                        if (y < rightYMin) rightYMin = y;
                        if (y > rightYMax) rightYMax = y;
                    }
                }

                if (!leftFound && !rightFound) continue;
                anyEdge = true;

                float lSpan = leftFound  ? leftMaxZ  - leftMinZ  : 0f;
                float rSpan = rightFound ? rightMaxZ - rightMinZ : 0f;
                if (lSpan > maxLspan) maxLspan = lSpan;
                if (rSpan > maxRspan) maxRspan = rSpan;
            }

            if (!anyEdge)
            {
                if (log)
                    Debugger.LogFormat("WTW [{0}] {1} gap={2:F1}: no edge-proximate verts in y=[{3},{4}] -> NOT-WTW",
                        name, tag, gap, WTW_WallYMin, WTW_WallYMax);
                return false;
            }

            float leftYRange  = leftYMax  > leftYMin  ? leftYMax  - leftYMin  : 0f;
            float rightYRange = rightYMax > rightYMin ? rightYMax - rightYMin : 0f;
            bool leftOk  = maxLspan >= WTW_MinCubeDepth && leftYRange  >= WTW_MinWallHeight;
            bool rightOk = maxRspan >= WTW_MinCubeDepth && rightYRange >= WTW_MinWallHeight;
            bool result  = leftOk && rightOk;

            if (log)
                Debugger.LogFormat("WTW [{0}] {1} gap={2:F1}: maxL={3:F2} maxR={4:F2} minDepth={5} leftYRange={6:F2} rightYRange={7:F2} minHeight={8} -> {9}",
                    name, tag, gap, maxLspan, maxRspan, WTW_MinCubeDepth, leftYRange, rightYRange, WTW_MinWallHeight, result ? "WTW" : "NOT-WTW");
            return result;
        }

        public bool isCustomAsset
        {
            get
            {
                string cleanName = Regex.Replace(name, @"^{{.*?}}\.", "");
                return cleanName.Contains(".");
            }
        }

        public string GetOriginText()
        {
            return GetOriginTextForName(name);
        }

        public Color32 GetStatusColor()
        {
            if (prefab == null && building != null && !isCustomAsset)
                return new Color32(128, 128, 128, 255);
            if (prefab == null)
                return new Color32(255, 255, 0, 255);
            if (building != null && building.baseName != null)
                return new Color32(50, 230, 255, 255);


            return new Color32(255, 255, 255, 255);
        }

        /// <summary>
        /// Origin text resolved from a raw prefab name — "Vanilla asset", "Workshop", or
        /// "Included in &lt;localized DLC / DistrictStyle name&gt;". Shared by the preview panel
        /// and the diagnostics report so both surfaces show identical wording.
        /// </summary>
        public static string GetOriginTextForName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return "";

            string clean = Regex.Replace(prefabName, @"^\{\{.*?\}\}\.", "");
            if (clean.Contains(".")) return Localization.Get("ORIGIN_WORKSHOP");

            var prefab = PrefabCollection<BuildingInfo>.FindLoaded(prefabName);
            if (prefab == null) return Localization.Get("ORIGIN_VANILLA");

            var exp  = prefab.m_requiredExpansion;
            var pack = prefab.m_requiredModderPack;
            var parts = new System.Collections.Generic.List<string>(2);
            if (exp  != SteamHelper.ExpansionBitMask.None)  parts.Add(DlcNames.GetExpansionName(exp));
            if (pack != SteamHelper.ModderPackBitMask.None) parts.Add(DlcNames.GetModderPackName(pack));

            if (parts.Count == 0)
            {
                var themes = BuildingThemesManager.GetBuiltInThemesForPrefab(prefabName);
                if (themes != null)
                {
                    foreach (var t in themes)
                    {
                        if (t == null) continue;
                        string label = t.localizedName;
                        if (!string.IsNullOrEmpty(label) && !parts.Contains(label))
                            parts.Add(label);
                    }
                }
            }

            if (parts.Count == 0) return Localization.Get("ORIGIN_VANILLA");
            return Localization.Get("ORIGIN_INCLUDED_IN", string.Join(", ", parts.ToArray()));
        }

        public static string CleanName(string name, bool cleanNumbers = false)
        {
            name = Regex.Replace(name, @"^{{.*?}}\.", "");
            name = Regex.Replace(name, @"[_+\.]", " ");
            name = Regex.Replace(name, @"(\d[xX]\d)|([HL]\d)", "");
            // Insert a space between a letter and a digit so e.g. "Detached03" becomes "Detached 03"
            name = Regex.Replace(name, @"(?<=[a-zA-Z])(?=\d)", " ");
            name = Regex.Replace(name, @"\s+", " ").Trim();

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
        }
    }
}
