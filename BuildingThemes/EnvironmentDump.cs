using System;
using System.IO;
using System.Text;
using ColossalFramework;
using ColossalFramework.Plugins;

namespace BuildingThemes
{
    // Dev tool. Only runs when Debugger.Enabled is true AND a `.dump-env` sentinel file exists
    // in the mod path (touch the file to opt in; remove it when done).
    //
    // Writes a comprehensive prefab_dump.xml capturing everything needed for offline processing
    // of the bundled themes:
    //   - Every BuildingInfo prefab's identity + DLC tags + class info + size
    //   - Every DistrictStyle's name, package, built-in flag, and building membership
    //   - m_areaBuildings for the current environment's spawn pool
    //
    // One dump per environment is needed (m_areaBuildings is env-specific).
    internal static class EnvironmentDump
    {
        private const string SentinelFile = ".dump-env";
        private const string LogPrefix = "[BT2:DUMP] ";

        private static void Status(string msg)
        {
            UnityEngine.Debug.Log(LogPrefix + msg);
        }

        public static void RunIfRequested()
        {
            if (!Debugger.Enabled) return;
            try
            {
                string modPath = FindModPath();
                if (modPath == null)
                {
                    Status("could not locate BT2 mod path; aborting.");
                    return;
                }

                string sentinel = Path.Combine(modPath, SentinelFile);
                if (!File.Exists(sentinel)) return;

                Status("sentinel found — running comprehensive prefab dump.");

                string env = Singleton<SimulationManager>.instance.m_metaData.m_environment ?? "";
                string outPath = Path.Combine(modPath, "prefab_dump.xml");
                var stats = WriteDump(outPath, env);
                Status("wrote " + stats.PrefabCount + " prefabs, " + stats.StyleCount + " styles to " + outPath);
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

        private struct DumpStats
        {
            public int PrefabCount;
            public int StyleCount;
        }

        // Single comprehensive snapshot, intentionally unfiltered. Workshop assets and
        // non-growable prefabs are included so offline tooling can decide what to keep.
        private static DumpStats WriteDump(string outPath, string currentEnv)
        {
            var sb = new StringBuilder(1 << 20); // ~1 MB initial buffer
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendFormat(
                "<PrefabDump generatedUtc=\"{0:u}\" currentEnvironment=\"{1}\">",
                DateTime.UtcNow, XmlEscape(currentEnv)).AppendLine();

            int prefabCount = WritePrefabs(sb);
            int styleCount  = WriteStyles(sb);
            int areaCount   = WriteAreaBuildings(sb);
            Status("AreaBuildings section: " + areaCount + " unique prefab names in current env's spawn pool.");

            sb.AppendLine("</PrefabDump>");
            File.WriteAllText(outPath, sb.ToString());

            return new DumpStats { PrefabCount = prefabCount, StyleCount = styleCount };
        }

        // Captures every prefab name present in any bucket of BuildingManager.m_areaBuildings
        // for the current environment. Lets offline tooling cross-reference "is this prefab
        // spawnable on THIS env" without having to reload the game.
        private static int WriteAreaBuildings(StringBuilder sb)
        {
            sb.AppendLine("  <AreaBuildings>");
            int count = 0;

            var bm = Singleton<BuildingManager>.instance;
            if (bm == null)
            {
                sb.AppendLine("  </AreaBuildings>");
                return 0;
            }

            // Force-populate the env-filtered pools; CS1 lazy-inits them on demand.
            var refresh = typeof(BuildingManager).GetMethod("RefreshAreaBuildings",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (refresh != null) refresh.Invoke(bm, null);

            var field = typeof(BuildingManager).GetField("m_areaBuildings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                sb.AppendLine("  </AreaBuildings>");
                return 0;
            }

            var pools = field.GetValue(bm) as Array;
            if (pools == null)
            {
                sb.AppendLine("  </AreaBuildings>");
                return 0;
            }

            var seen = new System.Collections.Generic.HashSet<string>();
            var names = new System.Collections.Generic.List<string>();
            for (int b = 0; b < pools.Length; b++)
            {
                var bucket = pools.GetValue(b) as FastList<ushort>;
                if (bucket == null) continue;
                for (int i = 0; i < bucket.m_size; i++)
                {
                    var prefab = PrefabCollection<BuildingInfo>.GetPrefab(bucket.m_buffer[i]);
                    if (prefab == null || string.IsNullOrEmpty(prefab.name)) continue;
                    if (seen.Add(prefab.name)) names.Add(prefab.name);
                }
            }
            names.Sort(StringComparer.Ordinal);

            foreach (var n in names)
            {
                sb.Append("    <B");
                AppendAttr(sb, "n", n);
                sb.AppendLine(" />");
                count++;
            }

            sb.AppendLine("  </AreaBuildings>");
            return count;
        }

        private static int WritePrefabs(StringBuilder sb)
        {
            sb.AppendLine("  <Prefabs>");
            int count = 0;

            uint total = (uint)PrefabCollection<BuildingInfo>.PrefabCount();
            for (uint i = 0; i < total; i++)
            {
                var prefab = PrefabCollection<BuildingInfo>.GetPrefab(i);
                if (prefab == null) continue;
                if (string.IsNullOrEmpty(prefab.name)) continue;

                string svc       = prefab.m_class != null ? prefab.m_class.m_service.ToString() : "None";
                string subsvc    = prefab.m_class != null ? prefab.m_class.m_subService.ToString() : "None";
                string level     = prefab.m_class != null ? prefab.m_class.m_level.ToString() : "None";
                string zone      = prefab.m_class != null ? prefab.m_class.GetZone().ToString() : "None";

                sb.Append("    <P");
                AppendAttr(sb, "n",       prefab.name);
                AppendAttr(sb, "exp",     prefab.m_requiredExpansion.ToString());
                AppendAttr(sb, "pack",    prefab.m_requiredModderPack.ToString());
                AppendAttr(sb, "dlc",     prefab.m_dlcRequired.ToString());
                AppendAttr(sb, "svc",     svc);
                AppendAttr(sb, "subsvc",  subsvc);
                AppendAttr(sb, "lvl",     level);
                AppendAttr(sb, "zone",    zone);
                AppendAttrInt(sb, "w",    prefab.m_cellWidth);
                AppendAttrInt(sb, "l",    prefab.m_cellLength);
                AppendAttr(sb, "pl",      prefab.m_placementStyle.ToString());
                sb.AppendLine(" />");
                count++;
            }

            sb.AppendLine("  </Prefabs>");
            return count;
        }

        private static int WriteStyles(StringBuilder sb)
        {
            sb.AppendLine("  <Styles>");
            int count = 0;

            var styles = Singleton<DistrictManager>.instance.m_Styles;
            if (styles != null)
            {
                foreach (var style in styles)
                {
                    if (style == null) continue;
                    sb.Append("    <S");
                    AppendAttr(sb, "name",    style.Name ?? "");
                    AppendAttr(sb, "pkg",     style.PackageName ?? "");
                    AppendAttr(sb, "full",    style.FullName ?? "");
                    AppendAttr(sb, "builtIn", style.BuiltIn ? "true" : "false");
                    sb.AppendLine(">");

                    var infos = style.GetBuildingInfos();
                    if (infos != null)
                    {
                        foreach (var info in infos)
                        {
                            if (info == null || string.IsNullOrEmpty(info.name)) continue;
                            sb.Append("      <B");
                            AppendAttr(sb, "n", info.name);
                            sb.AppendLine(" />");
                        }
                    }

                    sb.AppendLine("    </S>");
                    count++;
                }
            }

            sb.AppendLine("  </Styles>");
            return count;
        }

        private static void AppendAttr(StringBuilder sb, string name, string value)
        {
            sb.Append(' ').Append(name).Append("=\"").Append(XmlEscape(value)).Append('"');
        }

        private static void AppendAttrInt(StringBuilder sb, string name, int value)
        {
            sb.Append(' ').Append(name).Append("=\"").Append(value).Append('"');
        }

        private static string XmlEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
