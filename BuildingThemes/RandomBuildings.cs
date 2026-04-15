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
        /// </summary>
        [System.ThreadStatic]
        internal static bool s_intentionalNull;

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
                if (fastList == null
                    && mgr.GetDistrictEmptyLevelBehavior(districtId) == EmptyLevelBehavior.StrictThemeOnly
                    && mgr.IsEffectivelyThemed(districtId))
                {
                    s_intentionalNull = true;
                }

                if (Debugger.Enabled)
                    Debugger.LogFormat("[Themes] Spawn MISS  dist={0} {1}/{2} L{3} {4}x{5} | themed={6} levelMode={7} intentional={8}",
                        districtId, service, subService, (int)level + 1, width, length,
                        mgr.IsEffectivelyThemed(districtId),
                        mgr.GetDistrictEmptyLevelBehavior(districtId),
                        s_intentionalNull);
                return null;
            }

            int index = r.Int32((uint)fastList.m_size);
            BuildingInfo result = PrefabCollection<BuildingInfo>.GetPrefab((uint)fastList.m_buffer[index]);

            if (Debugger.Enabled)
                Debugger.LogFormat("[Themes] Spawn HIT   dist={0} {1}/{2} L{3} {4}x{5} | {6}/{7} candidates → '{8}'",
                    districtId, service, subService, (int)level + 1, width, length,
                    index + 1, fastList.m_size,
                    result != null ? result.name : "null");
            return result;
        }

        // Called every frame on building upgrade
        public static BuildingInfo GetRandomBuildingInfo_Upgrade(Vector3 position, ushort prefabIndex, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, int width, int length, BuildingInfo.ZoningMode zoningMode, int style)
        {
            // This method is called very frequently — keep allocations minimal.
            s_intentionalNull = false;

            var mgr = BuildingThemesManager.instance;
            var districtId = Singleton<DistrictManager>.instance.GetDistrict(position);

            // See if there is a special upgraded building
            var buildingInfo = mgr.GetUpgradeBuildingInfo(prefabIndex, districtId);
            if (buildingInfo != null)
            {
                if (Debugger.Enabled)
                    Debugger.LogFormat("[Themes] Upgrade HIT (explicit) dist={0} L{1} → '{2}'",
                        districtId, (int)level + 1, buildingInfo.name);
                return buildingInfo;
            }

            var areaIndex = BuildingThemesManager.GetAreaIndex(service, subService, level, width, length, zoningMode);
            var fastList = mgr.GetAreaBuildings(districtId, areaIndex);

            if (fastList == null || fastList.m_size == 0)
            {
                // Strict mode: block vanilla upgrade fallback for themed districts
                if (fastList == null
                    && mgr.GetDistrictEmptyLevelBehavior(districtId) == EmptyLevelBehavior.StrictThemeOnly
                    && mgr.IsEffectivelyThemed(districtId))
                {
                    s_intentionalNull = true;
                }

                if (Debugger.Enabled)
                    Debugger.LogFormat("[Themes] Upgrade MISS dist={0} {1}/{2} L{3} | themed={4} levelMode={5} intentional={6}",
                        districtId, service, subService, (int)level + 1,
                        mgr.IsEffectivelyThemed(districtId),
                        mgr.GetDistrictEmptyLevelBehavior(districtId),
                        s_intentionalNull);
                return null;
            }

            int index = r.Int32((uint)fastList.m_size);
            BuildingInfo result = PrefabCollection<BuildingInfo>.GetPrefab((uint)fastList.m_buffer[index]);

            if (Debugger.Enabled)
                Debugger.LogFormat("[Themes] Upgrade HIT  dist={0} {1}/{2} L{3} | {4}/{5} → '{6}'",
                    districtId, service, subService, (int)level + 1,
                    index + 1, fastList.m_size,
                    result != null ? result.name : "null");
            return result;
        }
    }
}