using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BuildingThemes
{
    /// <summary>
    /// Checks for known incompatible mods and detects Harmony patch conflicts on critical methods.
    /// Called once at OnLevelLoaded.
    /// </summary>
    public static class ModCompatibilityChecker
    {
        public enum Severity { Warning, Critical }

        public struct ConflictInfo
        {
            public string ModName;
            public string Reason;
            public Severity Level;
        }

        private struct KnownBadEntry
        {
            public string Id;
            public bool IsAssembly;
            public string DisplayName;
            public Severity Level;
            public string Reason;
        }

        private static readonly KnownBadEntry[] KnownBadMods =
        {
            new KnownBadEntry
            {
                Id          = BuildingThemesMod.EIGHTY_ONE_MOD,
                IsAssembly  = false,
                DisplayName = "81 Tiles (original)",
                Level       = Severity.Critical,
                Reason      = "Uses the old Detour/Redirect system that conflicts with Building Themes 2 " +
                              "spawning patches. Use '81 Tiles 2' (EightyOne2) instead."
            },
        };

        /// <summary>Runs all compatibility checks and returns any conflicts found.</summary>
        public static List<ConflictInfo> Check()
        {
            var conflicts = new List<ConflictInfo>();

            Debugger.LogFormat("Building Themes 2: ModCompatibilityChecker starting — {0} known-bad entry(s).", KnownBadMods.Length);

            // 1. Known-bad mod list
            foreach (var entry in KnownBadMods)
            {
                bool active = entry.IsAssembly
                    ? Util.IsModAssemblyActive(entry.Id)
                    : Util.IsModActive(entry.Id);
                Debugger.LogFormat("Building Themes 2: Checked '{0}' — active={1}.", entry.DisplayName, active);
                if (active)
                    conflicts.Add(new ConflictInfo
                    {
                        ModName = entry.DisplayName,
                        Reason  = entry.Reason,
                        Level   = entry.Level
                    });
            }

            // 2. Harmony runtime inspection — detect foreign Prefixes on ZoneBlock.SimulationStep
            //    that might block our own prefix from running.
            try
            {
                const string ourHarmonyId = BuildingThemesMod.HarmonyId;
                var method = typeof(ZoneBlock).GetMethod("SimulationStep",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    var info = Harmony.GetPatchInfo(method);
                    if (info != null)
                    {
                        Debugger.LogFormat("Building Themes 2: ZoneBlock.SimulationStep has {0} prefix(es).", info.Prefixes.Count);
                        foreach (var prefix in info.Prefixes)
                        {
                            Debugger.LogFormat("Building Themes 2: Prefix owner='{0}' (ours={1}).", prefix.owner, prefix.owner == ourHarmonyId);
                            if (prefix.owner == ourHarmonyId) continue;
                            conflicts.Add(new ConflictInfo
                            {
                                ModName = prefix.owner,
                                Reason  = "Has a Prefix on ZoneBlock.SimulationStep. " +
                                          "If that prefix returns false, Building Themes 2 spawning will be silently bypassed.",
                                Level   = Severity.Warning
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Debugger.LogFormat("Building Themes 2: ModCompatibilityChecker done — {0} conflict(s) found.", conflicts.Count);
            return conflicts;
        }
    }
}
