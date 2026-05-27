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
            const float maxGap       = 0.3f;  // X proximity to lot edge
            const float wallYMin     = 1.5f;  // above base slab; includes garage level
            const float wallYMax     = 8.0f;
            const float bandSize     = 1.0f;
            const float minCubeDepth = 3.0f;  // Z-extent required for a "real wall" vs thin arch/post

            bool log = Debugger.Enabled;

            var verts = mesh.vertices;
            if (verts == null || verts.Length == 0)
            {
                float leftGap  = mesh.bounds.min.x + halfCellW;
                float rightGap = halfCellW - mesh.bounds.max.x;
                bool r         = leftGap <= maxGap && rightGap <= maxGap;
                if (log)
                    Debugger.LogFormat("WTW [{0}] no verts, BOUNDS leftGap={1:F2} rightGap={2:F2} -> {3}",
                        name, leftGap, rightGap, r ? "WTW" : "NOT-WTW");
                return r;
            }

            if (log)
            {
                // Height profile: log minX/maxX in 1 m bands from 0 to meshTop.
                float meshTop = mesh.bounds.max.y;
                int bands = Mathf.CeilToInt(meshTop);
                var sb = new System.Text.StringBuilder();
                sb.AppendFormat("WTW-PROFILE [{0}] cellW={1} halfCellW={2} meshTop={3:F1}\n",
                    name, prefab.m_cellWidth, halfCellW, meshTop);
                for (int b = 0; b < bands; b++)
                {
                    float yLo = b, yHi = b + 1f;
                    float bMinX = float.MaxValue, bMaxX = float.MinValue;
                    bool bFound = false;
                    for (int i = 0; i < verts.Length; i++)
                    {
                        float y = verts[i].y;
                        if (y < yLo || y >= yHi) continue;
                        float x = verts[i].x;
                        if (x < bMinX) bMinX = x;
                        if (x > bMaxX) bMaxX = x;
                        bFound = true;
                    }
                    if (bFound)
                        sb.AppendFormat("  y=[{0},{1}): minX={2:F2} maxX={3:F2}  leftGap={4:F2} rightGap={5:F2}\n",
                            yLo, yHi, bMinX, bMaxX, bMinX + halfCellW, halfCellW - bMaxX);
                    else
                        sb.AppendFormat("  y=[{0},{1}): (no verts)\n", yLo, yHi);
                }
                Debugger.Log(sb.ToString());
            }

            // Per-band "cube wall" detection.
            // For each 1 m band in [wallYMin, wallYMax] find vertices within maxGap of the
            // left / right lot edges and measure their Z-extent (depth into the lot).
            // A structural wall — garage side wall, party wall — spans the full building
            // depth (≥ minCubeDepth). A thin decorative feature — arch post, pergola
            // column, boundary gate — has a small Z-extent and is rejected.
            // Track maximum Z-span seen for each side across ALL bands independently.
            // L-shaped or stepped buildings have their left wall at one height range and
            // right wall at a different range — we never require them to be in the same band.
            // Also track the Y-restricted overall X-extent for the fallback below.
            float maxLspan = 0f, maxRspan = 0f;
            float restrictedMinX = float.MaxValue, restrictedMaxX = float.MinValue;
            bool  anyBand  = false;
            var   bandLog  = log ? new System.Text.StringBuilder() : null;

            for (float bandLo = wallYMin; bandLo < wallYMax; bandLo += bandSize)
            {
                float bandHi = Mathf.Min(bandLo + bandSize, wallYMax);

                float leftMinZ  = float.MaxValue, leftMaxZ  = float.MinValue;
                float rightMinZ = float.MaxValue, rightMaxZ = float.MinValue;
                bool  leftFound = false,          rightFound = false;

                for (int i = 0; i < verts.Length; i++)
                {
                    float y = verts[i].y;
                    if (y < bandLo || y >= bandHi) continue;
                    float x = verts[i].x;
                    float z = verts[i].z;

                    // Track Y-restricted overall X-extent for the fallback.
                    if (x < restrictedMinX) restrictedMinX = x;
                    if (x > restrictedMaxX) restrictedMaxX = x;

                    if (x >= -halfCellW - maxGap && x <= -halfCellW + maxGap)
                    {
                        leftFound = true;
                        if (z < leftMinZ) leftMinZ = z;
                        if (z > leftMaxZ) leftMaxZ = z;
                    }
                    if (x >= halfCellW - maxGap && x <= halfCellW + maxGap)
                    {
                        rightFound = true;
                        if (z < rightMinZ) rightMinZ = z;
                        if (z > rightMaxZ) rightMaxZ = z;
                    }
                }

                if (!leftFound && !rightFound) continue;
                anyBand = true;

                float lSpan = leftFound  ? leftMaxZ  - leftMinZ  : 0f;
                float rSpan = rightFound ? rightMaxZ - rightMinZ : 0f;
                if (lSpan > maxLspan) maxLspan = lSpan;
                if (rSpan > maxRspan) maxRspan = rSpan;

                if (log)
                    bandLog.AppendFormat(
                        "  band y=[{0:F1},{1:F1}): L-span={2:F2}({3}) R-span={4:F2}({5})\n",
                        bandLo, bandHi,
                        lSpan, leftFound  ? (lSpan >= minCubeDepth ? "wall" : "thin") : "none",
                        rSpan, rightFound ? (rSpan >= minCubeDepth ? "wall" : "thin") : "none");
            }

            if (anyBand)
            {
                bool result = maxLspan >= minCubeDepth && maxRspan >= minCubeDepth;
                if (log)
                {
                    Debugger.Log(bandLog.ToString());
                    Debugger.LogFormat("WTW [{0}] maxL={1:F2} maxR={2:F2} minDepth={3} -> {4}",
                        name, maxLspan, maxRspan, minCubeDepth, result ? "WTW" : "NOT-WTW");
                }
                return result;
            }

            // No edge-proximate vertices in any band.
            // Fall back to Y-restricted bounds (vertices only from [wallYMin, wallYMax)),
            // which excludes the full-width ground slab at y=0-1 that fools the mesh.bounds
            // approach. Use a more generous gap (1.3 m) to accept buildings whose walls are
            // slightly inset from the lot edge due to thick masonry or modelling offset.
            if (restrictedMinX == float.MaxValue)
            {
                // No geometry at wall height at all → not WTW.
                if (log)
                    Debugger.LogFormat("WTW [{0}] no verts in yRange=[{1},{2}] -> NOT-WTW",
                        name, wallYMin, wallYMax);
                return false;
            }
            {
                const float fallbackMaxGap = 1.3f;
                float leftGap  = restrictedMinX + halfCellW;
                float rightGap = halfCellW - restrictedMaxX;
                bool result    = leftGap <= fallbackMaxGap && rightGap <= fallbackMaxGap;
                if (log)
                    Debugger.LogFormat("WTW [{0}] no edge-verts in yRange=[{1},{2}], RESTRICTED-FALLBACK leftGap={3:F2} rightGap={4:F2} threshold={5} -> {6}",
                        name, wallYMin, wallYMax, leftGap, rightGap, fallbackMaxGap, result ? "WTW" : "NOT-WTW");
                return result;
            }
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
            if (clean.Contains(".")) return "Workshop";

            var prefab = PrefabCollection<BuildingInfo>.FindLoaded(prefabName);
            if (prefab == null) return "Vanilla asset";

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

            if (parts.Count == 0) return "Vanilla asset";
            return "Included in " + string.Join(", ", parts.ToArray());
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
