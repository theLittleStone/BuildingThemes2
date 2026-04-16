using System.Linq;

namespace BuildingThemes
{
    /// <summary>
    /// Detects whether Skyve (mod manager) is installed and active in the current session.
    /// Detection is done once via assembly scanning and cached for the lifetime of the session.
    /// </summary>
    public static class SkyveDetector
    {
        private static bool? _detected;

        public static bool IsInstalled
        {
            get
            {
                if (!_detected.HasValue)
                {
                    _detected = System.AppDomain.CurrentDomain.GetAssemblies()
                        .Any(a =>
                        {
                            string name = a.GetName().Name;
                            return name.Contains("Skyve") || name.Contains("SkyveApp") || name.Contains("SkyveCore");
                        });
                }
                return _detected.Value;
            }
        }

        /// <summary>Resets the cached detection result (call on level unload if needed).</summary>
        public static void Reset() { _detected = null; }
    }
}
