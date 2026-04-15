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
                case RejectionReason.ZeroSpawnRate:
                    report.RejectedZeroSpawnRate++;
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
            sb.AppendFormat("[BuildingThemes2] District {0} theme compile:\n", districtId);
            sb.AppendFormat("  Candidates: {0} | Accepted: {1} | Rejected: {2}\n",
                report.TotalCandidates, report.Accepted,
                report.TotalCandidates - report.Accepted);
            sb.AppendFormat("    Not in theme: {0} | Variation: {1} | Zero spawn rate: {2}\n",
                report.RejectedNotInTheme, report.RejectedVariation, report.RejectedZeroSpawnRate);

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
            var report = GetReport(districtId);
            if (report == null)
                return "No diagnostics available for this district.\nEnable 'Generate Debug Output' in mod settings and re-assign a theme to generate data.";

            var sb = new StringBuilder();
            sb.AppendFormat("District {0} — Last theme compile\n", districtId);
            sb.AppendLine(new string('-', 40));
            sb.AppendFormat("Total prefab candidates : {0}\n", report.TotalCandidates);
            sb.AppendFormat("Accepted (will spawn)   : {0}\n", report.Accepted);
            sb.AppendLine();
            sb.AppendLine("Rejected:");
            sb.AppendFormat("  Not in theme          : {0}\n", report.RejectedNotInTheme);
            sb.AppendFormat("  Variation (filtered)  : {0}\n", report.RejectedVariation);
            sb.AppendFormat("  Zero spawn rate       : {0}\n", report.RejectedZeroSpawnRate);

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
        public int RejectedZeroSpawnRate;
        public readonly List<string> MissingAssetNames = new List<string>();
    }

    public enum RejectionReason
    {
        Accepted,
        NotInTheme,
        MissingAsset,
        Variation,
        ZeroSpawnRate
    }
}
