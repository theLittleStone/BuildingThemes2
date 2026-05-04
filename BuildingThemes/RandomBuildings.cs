using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace BuildingThemes
{
    public static class RandomBuildings
    {
        /// <summary>
        /// Set to true when strict mode deliberately returns null for a themed district.
        /// The Harmony prefixes read this to decide whether to skip the original vanilla method.
        /// ThreadStatic ensures each simulation thread has its own copy (default value is false).
        /// Public so that third-party mods (e.g. Advanced Building Level Control) can check it
        /// after calling GetRandomBuildingInfo_Upgrade and skip their own vanilla fallback when true.
        /// </summary>
        [System.ThreadStatic]
        public static bool s_intentionalNull;

        // Throttle per-building debug messages — these methods run every simulation tick.
        // First LogThrottle events per level load are logged; the rest are silently dropped.
        private const int LogThrottle = 10;
        private static int s_spawnLogCount;
        private static int s_upgradeLogCount;

        /// <summary>Reset log throttles. Call from LoadingExtension.OnLevelLoaded.</summary>
        public static void ResetLogThrottle()
        {
            s_spawnLogCount = 0;
            s_upgradeLogCount = 0;
        }

        // called before a new building spawns on empty land (ZoneBlock.SimulationStep)
        public static BuildingInfo GetRandomBuildingInfo_Spawn(Vector3 position, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int width, int length, BuildingInfo.ZoningMode zoningMode, int style)
        {
            s_intentionalNull = false;

            var areaIndex = BuildingThemesManager.GetAreaIndex(service, subService, level, width, length, zoningMode);
            var mgr = Singleton<BuildingThemesManager>.instance;
            var districtId = Singleton<DistrictManager>.instance.GetDistrict(position);
            FastList<ushort> fastList = mgr.GetAreaBuildings(districtId, areaIndex);

            if (fastList == null || fastList.m_size == 0)
            {
                // Strict mode: if the district is themed but has no buildings for this level,
                // signal to the Prefix that vanilla should NOT run as a fallback.
                // Covers both null (footprint never in theme) and empty (pool cleared after build).
                if (mgr.GetDistrictEmptyLevelBehavior(districtId) == EmptyLevelBehavior.StrictThemeOnly
                    && mgr.IsEffectivelyThemed(districtId))
                {
                    s_intentionalNull = true;
                }

                if (Debugger.Enabled && s_spawnLogCount < LogThrottle)
                {
                    s_spawnLogCount++;
                    Debugger.LogFormat("[Themes] Spawn MISS ({0}/{1}) dist={2} {3}/{4} L{5} {6}x{7} | themed={8} levelMode={9} intentional={10}",
                        s_spawnLogCount, LogThrottle,
                        districtId, service, subService, (int)level + 1, width, length,
                        mgr.IsEffectivelyThemed(districtId),
                        mgr.GetDistrictEmptyLevelBehavior(districtId),
                        s_intentionalNull);
                }
                return null;
            }

            int index = r.Int32((uint)fastList.m_size);
            BuildingInfo result = PrefabCollection<BuildingInfo>.GetPrefab((uint)fastList.m_buffer[index]);

            if (Debugger.Enabled && s_spawnLogCount < LogThrottle)
            {
                s_spawnLogCount++;
                Debugger.LogFormat("[Themes] Spawn HIT ({0}/{1}) dist={2} {3}/{4} L{5} {6}x{7} | {8}/{9} candidates → '{10}'",
                    s_spawnLogCount, LogThrottle,
                    districtId, service, subService, (int)level + 1, width, length,
                    index + 1, fastList.m_size,
                    result != null ? result.name : "null");
            }
            return result;
        }

        // Called every frame on building upgrade.
        //
        // Two settings govern upgrades, applied in order:
        //
        //   1. Theme has buildings at the target level (loaded or partially missing):
        //      The compiled spawn pool is used. Missing-asset mode has already been applied
        //      to that pool during CompileDistrictThemes, so fill/fallback works as configured.
        //
        //   2. Theme has NO buildings configured for the target level:
        //      The Level Behavior setting determines what happens:
        //        - Vanilla fallback → vanilla upgrade building
        //        - Strict           → block the upgrade entirely
        public static BuildingInfo GetRandomBuildingInfo_Upgrade(Vector3 position, ushort prefabIndex, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int width, int length, BuildingInfo.ZoningMode zoningMode, int style)
        {
            s_intentionalNull = false;

            var mgr = BuildingThemesManager.instance;
            var districtId = Singleton<DistrictManager>.instance.GetDistrict(position);

            // Step 1 — explicit per-building "Upgrade to" mapping set in the Theme Manager UI.
            var buildingInfo = mgr.GetUpgradeBuildingInfo(prefabIndex, districtId);
            if (buildingInfo != null)
            {
                if (Debugger.Enabled && s_upgradeLogCount < LogThrottle)
                {
                    s_upgradeLogCount++;
                    Debugger.LogFormat("[Themes] Upgrade explicit ({0}/{1}) dist={2} L{3} → '{4}'",
                        s_upgradeLogCount, LogThrottle, districtId, (int)level + 1, buildingInfo.name);
                }
                return buildingInfo;
            }

            // Step 2 — if district has no active theme, let vanilla handle the upgrade.
            if (!mgr.IsEffectivelyThemed(districtId))
                return null;

            // Step 3 — compiled spawn pool for the target level.
            //   Non-null: theme has buildings here (missing-asset mode already applied) → use them.
            //   Null: theme has no buildings configured for this level → go to Level Behavior.
            var areaIndex = BuildingThemesManager.GetAreaIndex(service, subService, level, width, length, zoningMode);
            var fastList = mgr.GetAreaBuildings(districtId, areaIndex);

            if (fastList != null && fastList.m_size > 0)
            {
                int index = r.Int32((uint)fastList.m_size);
                BuildingInfo result = PrefabCollection<BuildingInfo>.GetPrefab((uint)fastList.m_buffer[index]);
                if (Debugger.Enabled && s_upgradeLogCount < LogThrottle)
                {
                    s_upgradeLogCount++;
                    Debugger.LogFormat("[Themes] Upgrade HIT ({0}/{1}) dist={2} {3}/{4} L{5} | {6}/{7} → '{8}'",
                        s_upgradeLogCount, LogThrottle,
                        districtId, service, subService, (int)level + 1,
                        index + 1, fastList.m_size,
                        result != null ? result.name : "null");
                }
                return result;
            }

            // Step 4 — no theme buildings at this level → apply Level Behavior.
            var levelBehavior = mgr.GetDistrictEmptyLevelBehavior(districtId);

            if (Debugger.Enabled && s_upgradeLogCount < LogThrottle)
            {
                s_upgradeLogCount++;
                Debugger.LogFormat("[Themes] Upgrade MISS ({0}/{1}) dist={2} {3}/{4} L{5} | levelMode={6}",
                    s_upgradeLogCount, LogThrottle,
                    districtId, service, subService, (int)level + 1, levelBehavior);
            }

            switch (levelBehavior)
            {
                case EmptyLevelBehavior.StrictThemeOnly:
                    // No theme buildings at target level and strict mode → freeze upgrade.
                    s_intentionalNull = true;
                    return null;

                default: // EmptyLevelBehavior.VanillaFallback (also handles any obsolete/unknown value)
                    // Allow vanilla to pick the upgrade building.
                    return null;
            }
        }
    }
}