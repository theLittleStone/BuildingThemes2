#if DEBUG
using System;
using System.IO;
using System.Reflection;
using System.Text;
using ColossalFramework;
using ColossalFramework.Plugins;

namespace BuildingThemes
{
    // Dev-only tool. Compiled into Debug builds only; Release (the CD-published build) excludes it.
    // Trigger: presence of an empty sentinel file `.dump-env` in the mod path. Touch it, load a vanilla
    // map for each environment (Europe / Sunny / Boreal / Tropical), and `dump_<env>.xml` will be
    // written next to the sentinel. Copy the resulting fragments into BuildingThemes.xml.
    internal static class EnvironmentDump
    {
        private const string SentinelFile = ".dump-env";
        private const string LogPrefix = "[BT2:DUMP] ";

        // Logs unconditionally (no Debugger.Enabled gate) so dev sees status even with
        // BT2 debug logging off. UnityEngine.Debug.Log always writes to output_log.txt.
        private static void Status(string msg)
        {
            UnityEngine.Debug.Log(LogPrefix + msg);
        }

        public static void RunIfRequested()
        {
            try
            {
                string modPath = FindModPath();
                if (modPath == null)
                {
                    Status("could not locate BT2 mod path; aborting.");
                    return;
                }

                string sentinel = Path.Combine(modPath, SentinelFile);
                if (!File.Exists(sentinel))
                {
                    return; // no sentinel → nothing to do, silent in normal dev runs
                }

                Status("sentinel found at " + sentinel + " — running dump.");

                string env = Singleton<SimulationManager>.instance.m_metaData.m_environment;
                if (string.IsNullOrEmpty(env))
                {
                    Status("m_environment is empty; aborting.");
                    return;
                }

                string outPath = Path.Combine(modPath, "dump_" + env + ".xml");
                int count = WriteDump(outPath, env);
                Status("wrote " + count + " buildings to " + outPath);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(LogPrefix + "failed: " + e);
            }
        }

        private static string FindModPath()
        {
            foreach (var pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo())
            {
                if (pluginInfo == null || !pluginInfo.isEnabled) continue;
                if (pluginInfo.userModInstance is BuildingThemesMod)
                    return pluginInfo.modPath;
            }
            return null;
        }

        // Snapshot the game's no-style spawn pool (BuildingManager.m_areaBuildings[0]).
        // That bucket is what vanilla spawns from when a district has no style set, and
        // the game has already environment-filtered it (Sunny / Europe / North / Winter /
        // Tropical see different sets), so this is the authoritative source for env themes.
        // Workshop assets are still excluded — they don't belong in any built-in env theme.
        private static int WriteDump(string outPath, string env)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendFormat("<!-- Environment dump: {0}. Generated {1:u}. -->", env, DateTime.UtcNow).AppendLine();
            sb.AppendLine("<!-- Copy the <Theme> element below into BuildingThemes/BuildingThemes.xml. -->");
            sb.AppendLine("<Configuration>");
            sb.AppendLine("  <Themes>");
            sb.AppendFormat("    <Theme name=\"{0}\">", env).AppendLine();
            sb.AppendLine("      <Buildings>");

            // BuildingManager.m_areaBuildings is private. Force-populate it via the
            // (also private) RefreshAreaBuildings() method, then read via reflection.
            var bm = Singleton<BuildingManager>.instance;
            if (bm == null)
            {
                Status("BuildingManager.instance is null; aborting.");
                return 0;
            }

            // Trigger refresh — buckets are lazy-initialized by the game and may all be null
            // at sim-tick time when no district has actually requested a spawn yet.
            var refreshMethod = typeof(BuildingManager).GetMethod("RefreshAreaBuildings",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (refreshMethod != null)
            {
                refreshMethod.Invoke(bm, null);
                Status("RefreshAreaBuildings() invoked.");
            }
            else
            {
                Status("RefreshAreaBuildings() method not found; proceeding anyway.");
            }

            var field = typeof(BuildingManager).GetField("m_areaBuildings", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Status("BuildingManager.m_areaBuildings field not found via reflection; aborting.");
                return 0;
            }
            var pools = field.GetValue(bm) as Array;
            if (pools == null)
            {
                Status("BuildingManager.m_areaBuildings is null; aborting.");
                return 0;
            }
            Status("m_areaBuildings array length = " + pools.Length);
            if (pools.Length == 0)
            {
                Status("m_areaBuildings is empty; aborting.");
                return 0;
            }
            // m_areaBuildings is partitioned by area characteristics (service+subService+
            // level+width+length+zoningMode → index via GetAreaIndex), NOT by env. Each
            // bucket's contents are env-filtered by the game at refresh time, so we iterate
            // every non-null bucket and merge.
            var seen = new System.Collections.Generic.HashSet<string>();
            var names = new System.Collections.Generic.List<string>();
            int nonNullBuckets = 0;
            int totalEntries = 0;

            for (int b = 0; b < pools.Length; b++)
            {
                var bucket = pools.GetValue(b) as FastList<ushort>;
                if (bucket == null) continue;
                nonNullBuckets++;
                totalEntries += bucket.m_size;

                for (int i = 0; i < bucket.m_size; i++)
                {
                    ushort idx = bucket.m_buffer[i];
                    var prefab = PrefabCollection<BuildingInfo>.GetPrefab(idx);
                    if (prefab == null) continue;
                    if (prefab.m_placementStyle != ItemClass.Placement.Automatic) continue;
                    if (prefab.m_class == null || prefab.m_class.GetZone() == ItemClass.Zone.None) continue;

                    string name = prefab.name;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (name.Contains(".")) continue; // workshop asset → never in env themes

                    if (seen.Add(name)) names.Add(name);
                }
            }
            Status("Scanned " + nonNullBuckets + " non-null buckets, " + totalEntries + " total entries.");

            names.Sort(StringComparer.Ordinal);

            foreach (var name in names)
            {
                sb.AppendFormat("        <Building name=\"{0}\" />", XmlEscape(name)).AppendLine();
            }

            int kept = names.Count;

            sb.AppendLine("      </Buildings>");
            sb.AppendLine("    </Theme>");
            sb.AppendLine("  </Themes>");
            sb.AppendLine("</Configuration>");
            sb.AppendFormat("<!-- {0} building(s) -->", kept).AppendLine();

            File.WriteAllText(outPath, sb.ToString());
            return kept;
        }

        private static string XmlEscape(string s)
        {
            return s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
#endif
