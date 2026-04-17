using ColossalFramework;

namespace BuildingThemes
{
    /// <summary>
    /// Gradually demolishes growable buildings that are not valid for their district's active themes.
    /// Called every BuildingManager.SimulationStepImpl call (all four subSteps).
    /// Only runs when at least one district has autoBulldoze enabled.
    /// </summary>
    internal static class AutoBulldozeService
    {
        // How many building slots to inspect each tick.
        // The array is sparse (most slots are empty service buildings), so this needs to be
        // large enough that real growable buildings are encountered regularly.
        // 49152 total slots / 128 per tick = ~384 ticks per full city pass.
        // SimulationStepImpl fires for all 4 subSteps (~240/s at normal speed), so a full
        // pass takes ~1.6 seconds — responsive without being jarring.
        private const int BatchSize = 128;

        private static ushort s_cursor = 0;
        private static int s_tickCount = 0;

        /// <summary>
        /// Resets the scan cursor so the next tick starts from the beginning of the
        /// building array.  Call this whenever theme data changes so newly-invalid
        /// buildings are caught within one full scan cycle rather than waiting for the
        /// cursor to loop around naturally.
        /// </summary>
        internal static void ResetCursor()
        {
            Debugger.LogFormat("[AutoBulldoze] Cursor reset due to theme change (was {0}).", s_cursor);
            s_cursor = 0;
        }

        internal static void Tick()
        {
            var mgr = BuildingThemesManager.instance;
            if (mgr == null) return;

            var bm = Singleton<BuildingManager>.instance;
            var dm = Singleton<DistrictManager>.instance;
            if (bm == null || dm == null) return;

            s_tickCount++;

            // Heartbeat: confirm the service is running (visible with debug output on)
            if (s_tickCount % 4000 == 1)
                Debugger.LogFormat("[AutoBulldoze] Tick #{0}, cursor={1}", s_tickCount, s_cursor);

            int size = (int)bm.m_buildings.m_size;
            int scanned = 0;
            int eligible = 0;
            int released = 0;

            while (scanned < BatchSize)
            {
                if (++s_cursor >= size) s_cursor = 1;
                scanned++;

                ref Building b = ref bm.m_buildings.m_buffer[s_cursor];

                if ((b.m_flags & Building.Flags.Created) == 0) continue;

                BuildingInfo info = b.Info;
                if (info == null) continue;

                // Only auto-growable zone buildings (residential / commercial / industrial / office)
                if (info.m_placementStyle != ItemClass.Placement.Automatic) continue;
                if (ItemClass.GetPrivateServiceIndex(info.m_class.m_service) == -1) continue;

                byte districtId = dm.GetDistrict(b.m_position);

                if (!mgr.GetDistrictAutoBulldoze(districtId)) continue;
                if (!mgr.IsThemeManagementEnabled(districtId)) continue;
                if (mgr.IsBlacklistModeEnabled(districtId)) continue;

                eligible++;

                bool valid = mgr.IsBuildingValidForDistrict(s_cursor, districtId);

                Debugger.LogVerbose("[AutoBulldoze] id={0} district={1} prefab=\"{2}\" valid={3}",
                    s_cursor, districtId, info.name, valid);

                if (!valid)
                {
                    Debugger.LogFormat("[AutoBulldoze] Releasing building id={0} \"{1}\" in district {2}",
                        s_cursor, info.name, districtId);
                    bm.ReleaseBuilding(s_cursor);
                    released++;
                }
            }

            // Summary log for every tick that found eligible buildings
            if (eligible > 0)
                Debugger.LogFormat("[AutoBulldoze] Tick #{0}: scanned={1} eligible={2} released={3}",
                    s_tickCount, scanned, eligible, released);
        }
    }
}
