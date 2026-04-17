using ColossalFramework;
using System.Collections.Generic;
using System.Text;

namespace BuildingThemes.Diagnostics
{
    /// <summary>
    /// Collects per-district statistics during theme compilation so users can
    /// understand why buildings are or are not spawning.
    /// Only active when Debugger.Enabled is true (to avoid overhead in normal play).
    /// </summary>
    public static class ThemeDiagnostics
    {
        private static readonly Dictionary<byte, DistrictReport> _reports =
            new Dictionary<byte, DistrictReport>();

        /// <summary>
        /// Call at the start of CompileDistrictThemes to reset the report for that district.
        /// </summary>
        public static void BeginCompile(byte districtId)
        {
            _reports[districtId] = new DistrictReport { DistrictId = districtId };
        }

        /// <summary>
        /// Record one prefab candidate during RefreshAreaBuildings.
        /// </summary>
        public static void RecordBuilding(byte districtId, string name, RejectionReason reason)
        {
            DistrictReport report;
            if (!_reports.TryGetValue(districtId, out report)) return;

            report.TotalCandidates++;
            switch (reason)
            {
                case RejectionReason.Accepted:
                    report.Accepted++;
                    break;
                case RejectionReason.NotInTheme:
                    report.RejectedNotInTheme++;
                    break;
                case RejectionReason.MissingAsset:
                    report.RejectedMissingAsset++;
                    report.MissingAssetNames.Add(name);
                    break;
                case RejectionReason.Variation:
                    report.RejectedVariation++;
                    break;
            }
        }

        /// <summary>
        /// Returns the last compiled report for a district, or null if none exists.
        /// </summary>
        public static DistrictReport GetReport(byte districtId)
        {
            DistrictReport report;
            return _reports.TryGetValue(districtId, out report) ? report : null;
        }

        /// <summary>
        /// Clears all stored reports (called on level unload / reset).
        /// </summary>
        public static void Reset()
        {
            _reports.Clear();
        }

        /// <summary>
        /// Writes the report for a district to the debug log.
        /// </summary>
        public static void LogReport(byte districtId)
        {
            var report = GetReport(districtId);
            if (report == null) return;

            var sb = new StringBuilder();
            sb.AppendFormat("District {0} theme compile:\n", districtId);
            sb.AppendFormat("  Candidates: {0} | Accepted: {1} | Rejected: {2}\n",
                report.TotalCandidates, report.Accepted,
                report.TotalCandidates - report.Accepted);
            sb.AppendFormat("    Not in theme: {0} | Variation: {1}\n",
                report.RejectedNotInTheme, report.RejectedVariation);

            if (report.RejectedMissingAsset > 0)
            {
                sb.AppendFormat("    Missing assets: {0}\n", report.RejectedMissingAsset);
                foreach (var name in report.MissingAssetNames)
                    sb.AppendFormat("      - {0}\n", name);
            }

            Debugger.Log(sb.ToString());
        }

        /// <summary>
        /// Returns a human-readable summary string for the diagnostics UI modal.
        /// </summary>
        public static string FormatReport(byte districtId)
        {
            var sb = new StringBuilder();

            // ── Spawn-candidate compile report ────────────────────────────────────
            var report = GetReport(districtId);
            if (report == null)
            {
                if (!Debugger.Enabled)
                    sb.AppendLine("Debug output is disabled.\n\nEnable 'Generate Debug Output' in the mod settings (main menu → Options → Building Themes 2), then re-enable theme management for this district to collect data.");
                else
                    sb.AppendLine("No data for this district yet.\n\nDebug output is enabled — re-enable theme management for this district (uncheck and re-check 'Enable Theme Management') to trigger a fresh compile and collect diagnostics.");
            }
            else
            {
                sb.AppendFormat("District {0} — Last theme compile\n", districtId);
                sb.AppendLine(new string('-', 40));
                sb.AppendFormat("Total prefab candidates : {0}\n", report.TotalCandidates);
                sb.AppendFormat("Accepted (will spawn)   : {0}\n", report.Accepted);
                sb.AppendLine();
                sb.AppendLine("Rejected:");
                sb.AppendFormat("  Not in theme          : {0}\n", report.RejectedNotInTheme);
                sb.AppendFormat("  Variation (filtered)  : {0}\n", report.RejectedVariation);

                if (report.RejectedMissingAsset > 0)
                {
                    sb.AppendLine();
                    sb.AppendFormat("Missing assets ({0}):\n", report.RejectedMissingAsset);
                    foreach (var name in report.MissingAssetNames)
                        sb.AppendFormat("  {0}\n", name);
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("No missing assets.");
                }
            }

            // ── Non-theme buildings currently in district ─────────────────────────
            string nonTheme = FormatNonThemeBuildings(districtId);
            if (nonTheme != null)
            {
                sb.AppendLine();
                sb.AppendLine(new string('-', 40));
                sb.Append(nonTheme);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Scans the building manager and returns a formatted list of growable buildings
        /// currently placed in <paramref name="districtId"/> that are not valid for the
        /// district's active themes.  Returns null when theme management is off or in
        /// blacklist mode (where the concept of "non-theme" doesn't apply).
        /// </summary>
        public static string FormatNonThemeBuildings(byte districtId)
        {
            var mgr = BuildingThemesManager.instance;
            if (mgr == null) return null;
            if (!mgr.IsThemeManagementEnabled(districtId)) return null;
            if (mgr.IsBlacklistModeEnabled(districtId)) return null;

            var bm = Singleton<BuildingManager>.instance;
            var dm = Singleton<DistrictManager>.instance;
            if (bm == null || dm == null) return null;

            // building name → count
            var counts = new SortedDictionary<string, int>();
            int total = 0;
            int size = (int)bm.m_buildings.m_size;

            for (ushort i = 1; i < size; i++)
            {
                var b = bm.m_buildings.m_buffer[i];
                if ((b.m_flags & Building.Flags.Created) == 0) continue;

                var info = b.Info;
                if (info == null) continue;
                if (info.m_placementStyle != ItemClass.Placement.Automatic) continue;
                if (ItemClass.GetPrivateServiceIndex(info.m_class.m_service) == -1) continue;

                if (dm.GetDistrict(b.m_position) != districtId) continue;

                if (!mgr.IsBuildingValidForDistrict(i, districtId))
                {
                    int c;
                    counts.TryGetValue(info.name, out c);
                    counts[info.name] = c + 1;
                    total++;
                }
            }

            var sb = new StringBuilder();
            if (total == 0)
            {
                sb.AppendLine("Non-theme buildings in district: none");
            }
            else
            {
                sb.AppendFormat("Non-theme buildings in district ({0} buildings, {1} types):\n",
                    total, counts.Count);
                foreach (var kv in counts)
                    sb.AppendFormat("  {0} \u00d7 {1}\n", kv.Key, kv.Value);  // × sign
            }
            return sb.ToString();
        }
    }

    public class DistrictReport
    {
        public byte DistrictId;
        public int TotalCandidates;
        public int Accepted;
        public int RejectedNotInTheme;
        public int RejectedMissingAsset;
        public int RejectedVariation;
        public readonly List<string> MissingAssetNames = new List<string>();
    }

    public enum RejectionReason
    {
        Accepted,
        NotInTheme,
        MissingAsset,
        Variation,
    }
}
