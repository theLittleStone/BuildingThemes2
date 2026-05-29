using System;
using System.Runtime.CompilerServices;
using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using UnityEngine;

namespace BuildingThemes.HarmonyPatches.ZoneBlockPatch
{
    public static class SimulationStepPatch
    {
        // Zone grid dimensions — updated in SetUp() if 81 Tiles mod is active.
        private static int _zoneGridResolution = ZoneManager.ZONEGRID_RESOLUTION;
        private static int _zoneGridHalfResolution = ZoneManager.ZONEGRID_RESOLUTION / 2;
        private static readonly int EIGHTY_ONE_ZONEGRID_RESOLUTION = 270;
        private static readonly int EIGHTY_ONE_HALF_ZONEGRID_RESOLUTION = 270 / 2;

        private static bool deployed;
        private static int debugCount;

        // Increase grid bounds when 81 Tiles mod is detected.
        public static void SetUp()
        {
            if (!Util.IsModActive(BuildingThemesMod.EIGHTY_ONE_MOD) &&
                !Util.IsModAssemblyActive(BuildingThemesMod.EIGHTY_ONE_2_ASSEMBLY))
            {
                return;
            }
            _zoneGridResolution = EIGHTY_ONE_ZONEGRID_RESOLUTION;
            _zoneGridHalfResolution = EIGHTY_ONE_HALF_ZONEGRID_RESOLUTION;
        }

        public static void Deploy()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || deployed) return;

            // Apply reverse patches so our stubs delegate back to the original private helpers.
            Harmony.ReversePatch(
                AccessTools.Method(typeof(ZoneBlock), "CheckBlock"),
                new HarmonyMethod(typeof(SimulationStepPatch), nameof(CheckBlock)));
            Harmony.ReversePatch(
                AccessTools.Method(typeof(ZoneBlock), "IsGoodPlace"),
                new HarmonyMethod(typeof(SimulationStepPatch), nameof(IsGoodPlace)));

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(ZoneBlock), "SimulationStep"),
                prefix: new PatchUtil.MethodDefinition(typeof(SimulationStepPatch), nameof(Prefix)));

            deployed = true;
            Debugger.Log("ZoneBlock.SimulationStep patched.");
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(ZoneBlock), "SimulationStep"));

            deployed = false;
            debugCount = 0;
            Debugger.Log("ZoneBlock.SimulationStep unpatched.");
        }

        // Stubs replaced by Harmony ReversePatch with the original private implementations.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckBlock(ref ZoneBlock __instance, ushort blockID, ushort otherID,
            ref ZoneBlock other, int[] xBuffer, ItemClass.Zone zone, Vector2 startPos,
            Vector2 xDir, Vector2 zDir, Quad2 quad)
        {
            throw new NotImplementedException("CheckBlock stub was not replaced by Harmony reverse patch");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsGoodPlace(ref ZoneBlock __instance, Vector2 position)
        {
            throw new NotImplementedException("IsGoodPlace stub was not replaced by Harmony reverse patch");
        }

        // Prefix returns false to completely replace ZoneBlock.SimulationStep.
        // Logic is identical to the original ZoneBlockDetour, with __instance in place of zoneBlock,
        // and the temporary ImmaterialResourceManager revert/re-redirect removed (Harmony handles it).
        // Per-district count of consecutive zone blocks skipped because they lacked electricity.
        // When this reaches the fallback threshold the electricity filter is suspended for that
        // district so zones can still grow while the player extends their power grid.
        private static readonly int[] s_electricityMissCount = new int[128];
        private const int ELECTRICITY_FALLBACK_THRESHOLD = 40;

        // Per-district count of consecutive zone blocks skipped because the spawn position had
        // no adjacent existing building. When this reaches the fallback threshold the adjacency
        // filter is suspended for that district — so the first building in an empty area lands
        // anywhere, then subsequent ones cluster against it.
        private static readonly int[] s_adjacentMissCount = new int[128];
        private const int ADJACENT_FALLBACK_THRESHOLD = 40;
        // Buildings in CS1 are bucketed in a 270×270 grid with 64 m cells.
        private const int BUILDING_GRID_RES = 270;
        private const float BUILDING_GRID_CELL = 64f;
        private const int BUILDING_GRID_HALF = BUILDING_GRID_RES / 2;

        // Maximum metres we'll shift the new building along the road to make it touch its
        // neighbour. Large enough to pull a building across one empty cell (8 m) plus the
        // typical mesh inset, so a building that spawned a cell away still snaps flush.
        private const float SNAP_MAX_SHIFT = 10f;
        // Buildings whose along-road centre is further than this are not candidates — they're
        // too far away to be the "next building" on the same lot row.
        private const float SNAP_ALONG_MAX = 40f;
        // Tolerance (rad) for considering two buildings aligned with the same road. ~17°.
        private const float SNAP_ANGLE_TOL = 0.30f;

        // Finds the nearest aligned, same-lot-row building along the road and slides vector6 so
        // the new building's bounding-box edge meets it (closing the visible gap). Bounding
        // boxes come from each prefab's mesh (capped at the cell allocation) rotated into world
        // space, so the snap works for meshes narrower than their lot and for non-grid-aligned
        // models.
        //
        // RETURNS true when a snappable same-row neighbour was found (whether or not a shift was
        // needed) — the caller uses this as the "is this spawn adjacent?" signal. Returns false
        // when there's no aligned neighbour within range, i.e. the building would spawn isolated.
        private static bool SnapToAdjacentBuilding(
            ref Vector3 vector6, float roadAngle, Vector2 xDirection, Vector2 zDirection,
            BuildingInfo newInfo, int newLengthCells, int newWidthCells)
        {
            if (newInfo == null) return false;

            var bm = Singleton<BuildingManager>.instance;
            // CORRECTED AXES: in CS1's ZoneBlock, xDirection is PERPENDICULAR to the road (the
            // depth into the lot) and zDirection runs ALONG the road. Buildings in a row are
            // separated along the road, so the snap shifts along `alongUnit` (= zDirection); the
            // same-row test uses `acrossUnit` (= xDirection, depth). Moving along xDirection was
            // the bug that pushed buildings into the road / into the park behind.
            Vector2 alongUnit  = zDirection / 8f;  // along the road
            Vector2 acrossUnit = xDirection / 8f;  // perpendicular, into the lot (depth)
            bool log = Debugger.Enabled;

            // Same-road buildings store Building.m_angle == roadAngle + π/2 (the num30 we pass to
            // CreateBuilding). Used only to reject buildings on perpendicular roads.
            float newAngle = roadAngle + Mathf.PI * 0.5f;
            // Frontage (along road) is the mesh.x / m_cellWidth axis — that's the side the WtW
            // filter checks for walls. Depth (perpendicular) is mesh.z / m_cellLength.
            GetBuildingExtents(newInfo, out float newFrontHalf, out float newDepthHalf);

            int gx = Mathf.Clamp((int)(vector6.x / BUILDING_GRID_CELL + BUILDING_GRID_HALF), 0, BUILDING_GRID_RES - 1);
            int gz = Mathf.Clamp((int)(vector6.z / BUILDING_GRID_CELL + BUILDING_GRID_HALF), 0, BUILDING_GRID_RES - 1);

            float bestShift = 0f;
            float bestAbs   = float.MaxValue;
            ushort bestId   = 0;
            int considered = 0;

            for (int z = Mathf.Max(0, gz - 1); z <= Mathf.Min(BUILDING_GRID_RES - 1, gz + 1); z++)
            {
                for (int x = Mathf.Max(0, gx - 1); x <= Mathf.Min(BUILDING_GRID_RES - 1, gx + 1); x++)
                {
                    ushort id = bm.m_buildingGrid[z * BUILDING_GRID_RES + x];
                    int safety = 0;
                    while (id != 0)
                    {
                        var b = bm.m_buildings.m_buffer[id];
                        if ((b.m_flags & Building.Flags.Created) != 0 && b.Info != null)
                        {
                            considered++;
                            // Same road, either side OK (π flip allowed). Reject perpendicular roads.
                            float diff = Mathf.Repeat(b.m_angle - newAngle, Mathf.PI);
                            if (diff > Mathf.PI * 0.5f) diff = Mathf.PI - diff;
                            if (diff > SNAP_ANGLE_TOL) { id = b.m_nextGridBuilding; if (++safety >= 49152) break; continue; }

                            Vector2 d = new Vector2(b.m_position.x - vector6.x, b.m_position.z - vector6.z);
                            float along  = Vector2.Dot(d, alongUnit);   // separation along the road
                            float across = Vector2.Dot(d, acrossUnit);  // separation perpendicular (depth)

                            GetBuildingExtents(b.Info, out float exFrontHalf, out float exDepthHalf);

                            // Same lot row: the perpendicular (depth) gap must be ~0 (negative for
                            // buildings fronting the same road; positive across the street / back row).
                            float perpGap = Mathf.Abs(across) - newDepthHalf - exDepthHalf;
                            if (perpGap > 2f) { id = b.m_nextGridBuilding; if (++safety >= 49152) break; continue; }

                            float alongAbs = Mathf.Abs(along);
                            if (alongAbs > SNAP_ALONG_MAX) { id = b.m_nextGridBuilding; if (++safety >= 49152) break; continue; }

                            // Along-road gap from frontage edge to frontage edge.
                            float gap = alongAbs - newFrontHalf - exFrontHalf;
                            if (gap < 0f || gap > SNAP_MAX_SHIFT) { id = b.m_nextGridBuilding; if (++safety >= 49152) break; continue; }

                            float shift = along > 0f ? gap : -gap;  // toward the neighbour, along road
                            float absShift = Mathf.Abs(shift);
                            if (absShift < bestAbs)
                            {
                                bestAbs   = absShift;
                                bestShift = shift;
                                bestId    = id;
                            }
                        }
                        id = b.m_nextGridBuilding;
                        if (++safety >= 49152) break;
                    }
                }
            }

            bool found = bestId != 0;
            if (found && bestAbs > 0.05f)
            {
                Vector2 shift2 = alongUnit * bestShift;  // shift ALONG the road
                vector6.x += shift2.x;
                vector6.z += shift2.y;
                // Single concise line per actual snap — the routine "no neighbour / skip" cases
                // are intentionally silent so the log stays readable with debug output on.
                if (log)
                    Debugger.LogFormat("[ADJ-SNAP] {0}: snapped {1:F2} m along road to building #{2} (scanned {3})",
                        newInfo.name, bestShift, bestId, considered);
            }
            return found;
        }

        // Returns a building's half-extents in its own frame, with no rotation maths needed:
        //   frontHalf — half the frontage (the mesh.x / m_cellWidth axis, which the WtW filter
        //               checks for side walls). This is the extent ALONG the road, so two
        //               neighbours touch when their centres are frontHalf+frontHalf apart.
        //   depthHalf — half the depth (mesh.z / m_cellLength axis), the extent PERPENDICULAR
        //               to the road, used only for the same-row test.
        // Each axis is capped at the cell allocation (m_cellWidth/m_cellLength × 8 m) so garden
        // or landscape props that spill past the lot don't bloat the extent and make every
        // building read as "overlapping". For a WtW building whose wall is slightly inset, the
        // mesh extent is below the cap and is used as-is, so the snap can close that small gap.
        private static void GetBuildingExtents(BuildingInfo info, out float frontHalf, out float depthHalf)
        {
            float capX = info.m_cellWidth  * 8f;
            float capZ = info.m_cellLength * 8f;
            float sizeX = capX, sizeZ = capZ;
            if (info.m_mesh != null && info.m_mesh.bounds.size.x > 0f)
            {
                var sz = info.m_mesh.bounds.size;
                sizeX = Mathf.Min(sz.x, capX);
                sizeZ = Mathf.Min(sz.z, capZ);
            }
            frontHalf = sizeX * 0.5f;  // along road (frontage / wall axis)
            depthHalf = sizeZ * 0.5f;  // perpendicular (depth)
        }

        public static bool Prefix(ref ZoneBlock __instance, ushort blockID)
        {
            if (Debugger.Enabled && debugCount < 10)
            {
                debugCount++;
                Debugger.LogFormat("Patched ZoneBlock.SimulationStep called. blockID: {0}, position: {1}.",
                    blockID, __instance.m_position);
            }

            ZoneManager zoneManager = Singleton<ZoneManager>.instance;
            int rowCount = __instance.RowCount;
            float m_angle = __instance.m_angle;
            Vector2 xDirection = new Vector2(Mathf.Cos(m_angle), Mathf.Sin(m_angle)) * 8f;
            Vector2 zDirection = new Vector2(xDirection.y, -xDirection.x);
            ulong num = __instance.m_valid & ~(__instance.m_occupied1 | __instance.m_occupied2);
            int spawnpointRow = 0;
            ItemClass.Zone zone = ItemClass.Zone.Unzoned;
            for (int i = 0; i < 4; i++)
            {
                if (zone != 0)
                {
                    break;
                }
                spawnpointRow = Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)rowCount);
                if ((num & (ulong)(1L << (spawnpointRow << 3))) != 0)
                {
                    zone = __instance.GetZone(0, spawnpointRow);
                }
            }
            DistrictManager instance2 = Singleton<DistrictManager>.instance;

            Vector3 m_position = (Vector3)__instance.m_position;

            byte district = instance2.GetDistrict(m_position);

            // Fast path: if no BT2 theme is active for this district (or city-wide), let the
            // original SimulationStep run. This ensures the district's native m_Style (set by
            // the Districts & Styles DLC or District Styles Plus) is respected for building
            // selection instead of BT2's fallback pool, which would mix all styles together.
            {
                var mgr2 = BuildingThemesManager.instance;
                if (mgr2 == null || !mgr2.IsEffectivelyThemed(district))
                    return true;
            }

            int num4;
            switch (zone)
            {
                case ItemClass.Zone.ResidentialLow:
                    num4 = zoneManager.m_actualResidentialDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateResidentialLowDemandOffset();
                    break;
                case ItemClass.Zone.ResidentialHigh:
                    num4 = zoneManager.m_actualResidentialDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateResidentialHighDemandOffset();
                    break;
                case ItemClass.Zone.CommercialLow:
                    num4 = zoneManager.m_actualCommercialDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateCommercialLowDemandOffset();
                    break;
                case ItemClass.Zone.CommercialHigh:
                    num4 = zoneManager.m_actualCommercialDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateCommercialHighDemandOffset();
                    break;
                case ItemClass.Zone.Industrial:
                    num4 = zoneManager.m_actualWorkplaceDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateIndustrialDemandOffset();
                    break;
                case ItemClass.Zone.Office:
                    num4 = zoneManager.m_actualWorkplaceDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateOfficeDemandOffset();
                    break;
                default:
                    return false;
            }
            Vector2 a = VectorUtils.XZ(m_position);
            Vector2 vector3 = a - 3.5f * xDirection + ((float)spawnpointRow - 3.5f) * zDirection;
            int[] tmpXBuffer = zoneManager.m_tmpXBuffer;
            for (int i = 0; i < 13; i++)
            {
                tmpXBuffer[i] = 0;
            }
            Quad2 quad = default(Quad2);
            quad.a = a - 4f * xDirection + ((float)spawnpointRow - 10f) * zDirection;
            quad.b = a + 3f * xDirection + ((float)spawnpointRow - 10f) * zDirection;
            quad.c = a + 3f * xDirection + ((float)spawnpointRow + 2f) * zDirection;
            quad.d = a - 4f * xDirection + ((float)spawnpointRow + 2f) * zDirection;
            Vector2 vector4 = quad.Min();
            Vector2 vector5 = quad.Max();

            int num5 = Mathf.Max((int)((vector4.x - 46f) / 64f + _zoneGridHalfResolution), 0);
            int num6 = Mathf.Max((int)((vector4.y - 46f) / 64f + _zoneGridHalfResolution), 0);
            int num7 = Mathf.Min((int)((vector5.x + 46f) / 64f + _zoneGridHalfResolution), _zoneGridResolution - 1);
            int num8 = Mathf.Min((int)((vector5.y + 46f) / 64f + _zoneGridHalfResolution), _zoneGridResolution - 1);

            for (int j = num6; j <= num8; j++)
            {
                for (int k = num5; k <= num7; k++)
                {
                    ushort num9 = zoneManager.m_zoneGrid[j * _zoneGridResolution + k];
                    int num10 = 0;
                    while (num9 != 0)
                    {
                        Vector3 positionVar = zoneManager.m_blocks.m_buffer[(int)num9].m_position;
                        float num11 = Mathf.Max(
                            Mathf.Max(vector4.x - 46f - positionVar.x, vector4.y - 46f - positionVar.z),
                            Mathf.Max(positionVar.x - vector5.x - 46f, positionVar.z - vector5.y - 46f));

                        if (num11 < 0f)
                        {
                            CheckBlock(ref __instance, blockID, num9,
                                ref zoneManager.m_blocks.m_buffer[num9],
                                tmpXBuffer, zone, vector3, xDirection, zDirection, quad);
                        }
                        num9 = zoneManager.m_blocks.m_buffer[(int)num9].m_nextGridBlock;
                        if (++num10 >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core,
                                "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }

            for (int l = 0; l < 13; l++)
            {
                uint num12 = (uint)tmpXBuffer[l];
                int num13 = 0;
                bool newFlag = (num12 & 0x10000) == 65536;
                bool flag = (num12 & 0x20000) == 131072;
                bool flag2 = false;
                while ((num12 & 1u) != 0u)
                {
                    num13++;
                    flag2 = ((num12 & 65536u) != 0u);
                    num12 >>= 1;
                }
                switch (num13)
                {
                    case 5:
                    case 6:
                        num13 = ((!flag2) ? 4 : (num13 - (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) + 2)));
                        num13 |= 0x40000;
                        break;
                    case 7:
                        num13 = 4;
                        num13 |= 0x40000;
                        break;
                }
                if (num13 != 0)
                {
                    if (newFlag) num13 |= 0x10000;
                    if (flag)    num13 |= 0x20000;
                }
                tmpXBuffer[l] = num13;
            }

            int num14 = tmpXBuffer[6] & 65535;
            if (num14 == 0) return false;

            bool flag3 = IsGoodPlace(ref __instance, vector3);

            // Electricity preference: skip this block when the spawn position has no conductivity,
            // unless too many consecutive blocks have already been skipped (fallback to normal
            // spawning so zones can still grow before the player extends their power grid).
            if (BuildingThemesManager.instance.GetDistrictPreferElectricity(district))
            {
                bool hasPower;
                Singleton<ElectricityManager>.instance
                    .CheckElectricity(new Vector3(vector3.x, 0f, vector3.y), out hasPower);
                if (hasPower)
                {
                    s_electricityMissCount[district] = 0;
                }
                else
                {
                    s_electricityMissCount[district]++;
                    if (s_electricityMissCount[district] < ELECTRICITY_FALLBACK_THRESHOLD)
                        return false;
                }
            }

            // Adjacency preference is enforced later, at vector6 (the actual building position),
            // because the decision and the wall-to-wall snap must use the SAME same-row neighbour
            // search — otherwise this early check would green-light spawns the snap can't reach.
            // See the SnapToAdjacentBuilding call just before CreateBuilding.

            if (Singleton<SimulationManager>.instance.m_randomizer.Int32(100u) >= num4)
            {
                if (flag3) zoneManager.m_goodAreaFound[(int)zone] = 1024;
                return false;
            }
            if (!flag3 && zoneManager.m_goodAreaFound[(int)zone] > -1024)
            {
                if (zoneManager.m_goodAreaFound[(int)zone] == 0)
                    zoneManager.m_goodAreaFound[(int)zone] = -1;
                return false;
            }

            int num15 = 6;
            int num16 = 6;
            bool flag4 = true;
            while (true)
            {
                if (flag4)
                {
                    while (num15 != 0)
                    {
                        if ((tmpXBuffer[num15 - 1] & 65535) != num14) break;
                        num15--;
                    }
                    while (num16 != 12)
                    {
                        if ((tmpXBuffer[num16 + 1] & 65535) != num14) break;
                        num16++;
                    }
                }
                else
                {
                    while (num15 != 0)
                    {
                        if ((tmpXBuffer[num15 - 1] & 65535) < num14) break;
                        num15--;
                    }
                    while (num16 != 12)
                    {
                        if ((tmpXBuffer[num16 + 1] & 65535) < num14) break;
                        num16++;
                    }
                }
                int num17 = num15;
                int num18 = num16;
                while (num17 != 0)
                {
                    if ((tmpXBuffer[num17 - 1] & 65535) < 2) break;
                    num17--;
                }
                while (num18 != 12)
                {
                    if ((tmpXBuffer[num18 + 1] & 65535) < 2) break;
                    num18++;
                }
                bool flag5 = num17 != 0 && num17 == num15 - 1;
                bool flag6 = num18 != 12 && num18 == num16 + 1;
                if (flag5 && flag6)
                {
                    if (num16 - num15 > 2) { num15++; num16--; break; }
                    if (num14 <= 2) { if (!flag4) break; }
                    else num14--;
                }
                else if (flag5)
                {
                    if (num16 - num15 > 1) { num15++; break; }
                    if (num14 <= 2) { if (!flag4) break; }
                    else num14--;
                }
                else if (flag6)
                {
                    if (num16 - num15 > 1) { num16--; break; }
                    if (num14 <= 2) { if (!flag4) break; }
                    else num14--;
                }
                else
                {
                    if (num15 != num16) break;
                    if (num14 <= 2) { if (!flag4) break; }
                    else num14--;
                }
                flag4 = false;
            }

            int num19;
            int num20;
            if (num14 == 1 && num16 - num15 >= 1)
            {
                num15 += Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)(num16 - num15));
                num16 = num15 + 1;
                num19 = num15 + Singleton<SimulationManager>.instance.m_randomizer.Int32(2u);
                num20 = num19;
            }
            else
            {
                do
                {
                    num19 = num15;
                    num20 = num16;
                    if (num16 - num15 == 2)
                    {
                        if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0) num20--;
                        else num19++;
                    }
                    else if (num16 - num15 == 3)
                    {
                        if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0) num20 -= 2;
                        else num19 += 2;
                    }
                    else if (num16 - num15 == 4)
                    {
                        if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0) { num16 -= 2; num20 -= 3; }
                        else { num15 += 2; num19 += 3; }
                    }
                    else if (num16 - num15 == 5)
                    {
                        if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0) { num16 -= 3; num20 -= 2; }
                        else { num15 += 3; num19 += 2; }
                    }
                    else if (num16 - num15 >= 6)
                    {
                        if (num15 == 0 || num16 == 12)
                        {
                            if (num15 == 0) { num15 = 3; num19 = 2; }
                            if (num16 == 12) { num16 = 9; num20 = 10; }
                        }
                        else if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0)
                        {
                            num16 = num15 + 3;
                            num20 = num19 + 2;
                        }
                        else
                        {
                            num15 = num16 - 3;
                            num19 = num20 - 2;
                        }
                    }
                }
                while (num16 - num15 > 3 || num20 - num19 > 3);
            }

            int depth_A = 4;
            int width_A = num16 - num15 + 1;
            BuildingInfo.ZoningMode zoningMode = BuildingInfo.ZoningMode.Straight;
            bool flag7 = true;
            for (int m = num15; m <= num16; m++)
            {
                depth_A = Mathf.Min(depth_A, tmpXBuffer[m] & 65535);
                if ((tmpXBuffer[m] & 0x40000) == 0) flag7 = false;
            }
            if (num16 >= num15)
            {
                if ((tmpXBuffer[num15] & 65536) != 0)
                {
                    zoningMode = BuildingInfo.ZoningMode.CornerLeft;
                    num20 = num15 + num20 - num19;
                    num19 = num15;
                }
                else if (((uint)tmpXBuffer[num16] & 0x20000u) != 0)
                {
                    zoningMode = BuildingInfo.ZoningMode.CornerRight;
                    num19 = num16 + num19 - num20;
                    num20 = num16;
                }
            }

            int depth_B = 4;
            int width_B = num20 - num19 + 1;
            BuildingInfo.ZoningMode zoningMode2 = BuildingInfo.ZoningMode.Straight;
            bool flag8 = true;
            for (int n = num19; n <= num20; n++)
            {
                depth_B = Mathf.Min(depth_B, tmpXBuffer[n] & 65535);
                if ((tmpXBuffer[n] & 0x40000) == 0) flag8 = false;
            }
            if (num20 >= num19)
            {
                if (((uint)tmpXBuffer[num19] & 0x10000u) != 0) zoningMode2 = BuildingInfo.ZoningMode.CornerLeft;
                if (((uint)tmpXBuffer[num20] & 0x20000u) != 0) zoningMode2 = BuildingInfo.ZoningMode.CornerRight;
            }

            ItemClass.Service service = ItemClass.Service.None;
            ItemClass.SubService subService = ItemClass.SubService.None;
            ItemClass.Level level = ItemClass.Level.Level1;
            switch (zone)
            {
                case ItemClass.Zone.ResidentialLow:
                    service = ItemClass.Service.Residential;
                    subService = ItemClass.SubService.ResidentialLow;
                    break;
                case ItemClass.Zone.ResidentialHigh:
                    service = ItemClass.Service.Residential;
                    subService = ItemClass.SubService.ResidentialHigh;
                    break;
                case ItemClass.Zone.CommercialLow:
                    service = ItemClass.Service.Commercial;
                    subService = ItemClass.SubService.CommercialLow;
                    break;
                case ItemClass.Zone.CommercialHigh:
                    service = ItemClass.Service.Commercial;
                    subService = ItemClass.SubService.CommercialHigh;
                    break;
                case ItemClass.Zone.Industrial:
                    service = ItemClass.Service.Industrial;
                    break;
                case ItemClass.Zone.Office:
                    service = ItemClass.Service.Office;
                    break;
                default:
                    return false;
            }

            BuildingInfo buildingInfo = null;
            Vector3 vector6 = Vector3.zero;
            int num25_row = 0;
            int length = 0;
            int width = 0;
            BuildingInfo.ZoningMode zoningMode3 = BuildingInfo.ZoningMode.Straight;
            int num28 = 0;

            int depth_alt = Mathf.Min(depth_A, 4);
            int width_alt = width_A;

            // Size-preference path: replaces the 8-candidate while loop when the district has
            // a non-Default preference for the current zone's service type.
            //
            // CORNER LOTS ARE EXCLUDED ON PURPOSE. Corner geometry (handedness, the swapped-
            // dimension bucket in GetAreaIndex that lets one corner asset serve both corners,
            // and the 90° rotate-flip below) is handled correctly only by the vanilla
            // 8-candidate loop further down. Bolting size preference onto corners caused
            // wrong-handedness rejections AND empty corners (the strict `return false` here has
            // no corner→straight fallback). Size preference is conceptually a straight-lot
            // feature anyway, so any corner lot defers to the vanilla loop, which reproduces the
            // default corner behaviour (corner-first selection, then straight fallback).
            SizePreference sizePref = BuildingThemesManager.instance != null
                ? BuildingThemesManager.instance.GetDistrictSizePreference(district, service)
                : SizePreference.Default;

            bool straightLot = zoningMode  == BuildingInfo.ZoningMode.Straight
                            && zoningMode2 == BuildingInfo.ZoningMode.Straight;

            if (sizePref != SizePreference.Default && straightLot)
            {
                // Compute a real spawn position for GetIndustryType / GetCommercialType
                // (vector6 is still zero here — use the primary lot center as approximation).
                Vector3 prefSpawnPos = m_position + VectorUtils.X_Y(
                    ((float)depth_A * 0.5f - 4f) * xDirection +
                    ((float)(num15 + num16 + 1) * 0.5f + (float)spawnpointRow - 10f) * zDirection);

                // Resolve subService/level for zone types that depend on spawn position.
                if (zone == ItemClass.Zone.Industrial)
                    ZoneBlock.GetIndustryType(prefSpawnPos, out subService, out level);
                else if (zone == ItemClass.Zone.CommercialLow || zone == ItemClass.Zone.CommercialHigh)
                    ZoneBlock.GetCommercialType(prefSpawnPos, zone, width_A, depth_A, out subService, out level);
                else if (zone == ItemClass.Zone.ResidentialLow || zone == ItemClass.Zone.ResidentialHigh)
                    ZoneBlock.GetResidentialType(prefSpawnPos, zone, width_A, depth_A, out subService, out level);
                else if (zone == ItemClass.Zone.Office)
                    ZoneBlock.GetOfficeType(prefSpawnPos, zone, width_A, depth_A, out subService, out level);

                buildingInfo = BuildingThemesManager.instance.GetRandomBuildingInfoWithPreference(
                    district, service, subService, level,
                    width_A, depth_A, BuildingInfo.ZoningMode.Straight,
                    ref Singleton<SimulationManager>.instance.m_randomizer);

                if (buildingInfo != null)
                {
                    num25_row   = num15 + num16 + 1;
                    length      = buildingInfo.m_cellLength;
                    width       = buildingInfo.m_cellWidth;
                    zoningMode3 = BuildingInfo.ZoningMode.Straight;
                    if (buildingInfo.m_cellWidth < width_A)
                        num25_row += buildingInfo.m_cellWidth - width_A;

                    if (Debugger.Enabled)
                        Debugger.LogFormat("SIZEPREF-SPAWN: name={0} cellW={1} reqLotW_A={2} num25_row={3} num15={4} num16={5}",
                            buildingInfo.name, buildingInfo.m_cellWidth, width_A, num25_row, num15, num16);
                    vector6 = m_position + VectorUtils.X_Y(
                        ((float)length * 0.5f - 4f) * xDirection +
                        ((float)num25_row * 0.5f + (float)spawnpointRow - 10f) * zDirection);
                    goto IL_AfterLoop;
                }

                return false; // no theme building fits this straight lot → leave empty (strict size-pref)
            }

            while (num28 < 8)
            {
                switch (num28)
                {
                    // Corner cases
                    case 0:
                        if (zoningMode != BuildingInfo.ZoningMode.Straight)
                        {
                            num25_row = num15 + num16 + 1;
                            length = depth_A;
                            width = width_A;
                            zoningMode3 = zoningMode;
                            goto IL_D6A;
                        }
                        break;
                    case 1:
                        if (zoningMode2 != BuildingInfo.ZoningMode.Straight)
                        {
                            num25_row = num19 + num20 + 1;
                            length = depth_B;
                            width = width_B;
                            zoningMode3 = zoningMode2;
                            goto IL_D6A;
                        }
                        break;
                    case 2:
                        if (zoningMode != BuildingInfo.ZoningMode.Straight && depth_A >= 4)
                        {
                            num25_row = num15 + num16 + 1;
                            length = ((!flag7) ? 2 : 3);
                            width = width_A;
                            zoningMode3 = zoningMode;
                            goto IL_D6A;
                        }
                        break;
                    case 3:
                        if (zoningMode2 != BuildingInfo.ZoningMode.Straight && depth_B >= 4)
                        {
                            num25_row = num19 + num20 + 1;
                            length = ((!flag8) ? 2 : 3);
                            width = width_B;
                            zoningMode3 = zoningMode2;
                            goto IL_D6A;
                        }
                        break;
                    // begin mod: shrink corner plots to fit available theme assets
                    case 4:
                        if (zoningMode != BuildingInfo.ZoningMode.Straight)
                        {
                            if (width_alt > 1)
                            {
                                width_alt--;
                            }
                            else if (depth_alt > 1)
                            {
                                depth_alt--;
                                width_alt = width_A;
                            }
                            else break;

                            if (width_alt == width_A)
                            {
                                num25_row = num15 + num16 + 1;
                            }
                            else
                            {
                                if (zoningMode == BuildingInfo.ZoningMode.CornerLeft)
                                    num25_row = num15 + num16 + 1 - (width_A - width_alt);
                                else
                                    num25_row = num15 + num16 + 1 + (width_A - width_alt);
                            }

                            length = depth_alt;
                            width = width_alt;
                            zoningMode3 = zoningMode;
                            num28--;
                            goto IL_D6A;
                        }
                        break;
                    // end mod
                    // Straight cases
                    case 5:
                        num25_row = num15 + num16 + 1;
                        length = depth_A;
                        width = width_A;
                        zoningMode3 = BuildingInfo.ZoningMode.Straight;
                        goto IL_D6A;
                    case 6:
                        // Reset shrink variables for the second straight attempt
                        depth_alt = Mathf.Min(depth_A, 4);
                        width_alt = width_A;

                        num25_row = num19 + num20 + 1;
                        length = depth_B;
                        width = width_B;
                        zoningMode3 = BuildingInfo.ZoningMode.Straight;
                        goto IL_D6A;
                    // begin mod: shrink straight plots to fit available theme assets
                    case 7:
                        if (width_alt > 1)
                        {
                            width_alt--;
                        }
                        else break;

                        // Place the narrower building at the left (street-side) edge of the
                        // original lot. For a building of width w starting at column num15:
                        //   num25_row = num15 + (num15 + w - 1) + 1 = 2 * num15 + w
                        // This is correct for any shrink amount, including multi-step shrinks.
                        num25_row = 2 * num15 + width_alt;

                        length = depth_alt;
                        width = width_alt;
                        zoningMode3 = BuildingInfo.ZoningMode.Straight;
                        num28--;
                        goto IL_D6A;
                    // end mod
                    default:
                        goto IL_D6A;
                }

            IL_DF0:
                num28++;
                continue;

            IL_D6A:
                vector6 = m_position + VectorUtils.X_Y(
                    ((float)length * 0.5f - 4f) * xDirection +
                    ((float)num25_row * 0.5f + (float)spawnpointRow - 10f) * zDirection);

                if (zone == ItemClass.Zone.Industrial)
                {
                    ZoneBlock.GetIndustryType(vector6, out subService, out level);
                }
                else if (zone == ItemClass.Zone.CommercialLow || zone == ItemClass.Zone.CommercialHigh)
                {
                    ZoneBlock.GetCommercialType(vector6, zone, width, length, out subService, out level);
                }
                else if (zone == ItemClass.Zone.ResidentialLow || zone == ItemClass.Zone.ResidentialHigh)
                {
                    ZoneBlock.GetResidentialType(vector6, zone, width, length, out subService, out level);
                }
                else if (zone == ItemClass.Zone.Office)
                {
                    ZoneBlock.GetOfficeType(vector6, zone, width, length, out subService, out level);
                }

                byte district2 = instance2.GetDistrict(vector6);
                ushort style = instance2.m_districts.m_buffer[(int)district2].m_Style;
                if (Singleton<BuildingManager>.instance.m_BuildingWrapper != null)
                {
                    Singleton<BuildingManager>.instance.m_BuildingWrapper
                        .OnCalculateSpawn(vector6, ref service, ref subService, ref level, ref style);
                }

                buildingInfo = RandomBuildings.GetRandomBuildingInfo_Spawn(
                    vector6, ref Singleton<SimulationManager>.instance.m_randomizer,
                    service, subService, level, width, length, zoningMode3, style);

                if (buildingInfo != null)
                {
                    // If the prefab is shallower than requested, shrink the plot so no land is wasted.
                    if (buildingInfo.GetWidth() == width && buildingInfo.GetLength() != length)
                    {
                        float biggestPropPosZ = 0;
                        if (buildingInfo.m_props != null)
                        {
                            foreach (var prop in buildingInfo.m_props)
                            {
                                if (prop == null) continue;
                                biggestPropPosZ = Mathf.Max(biggestPropPosZ,
                                    buildingInfo.m_expandFrontYard ? prop.m_position.z : -prop.m_position.z);
                            }
                        }

                        float occupiedExtraSpace = biggestPropPosZ - buildingInfo.GetLength() * 4;
                        if (occupiedExtraSpace <= 0)
                        {
                            length = buildingInfo.GetLength();
                        }
                        else
                        {
                            int newLength = buildingInfo.GetLength() + Mathf.CeilToInt(occupiedExtraSpace / 8);
                            length = Mathf.Min(length, newLength);
                        }
                        vector6 = m_position + VectorUtils.X_Y(
                            ((float)length * 0.5f - 4f) * xDirection +
                            ((float)num25_row * 0.5f + (float)spawnpointRow - 10f) * zDirection);
                    }
                    else if (buildingInfo.GetLength() == width && buildingInfo.GetWidth() != length)
                    {
                        length = buildingInfo.GetWidth();
                        vector6 = m_position + VectorUtils.X_Y(
                            ((float)length * 0.5f - 4f) * xDirection +
                            ((float)num25_row * 0.5f + (float)spawnpointRow - 10f) * zDirection);
                    }

                    // When the building is narrower than the lot we searched, left-align it
                    // at the lot edge instead of centering it (which causes visual gaps on
                    // both sides). num25_row encodes the lot as 2*leftCol + lotWidth; subtracting
                    // the width difference shifts the center left so the building starts at leftCol.
                    if (buildingInfo.m_cellWidth < width)
                    {
                        int old25 = num25_row;
                        num25_row += buildingInfo.m_cellWidth - width;
                        width = buildingInfo.m_cellWidth;
                        vector6 = m_position + VectorUtils.X_Y(
                            ((float)length * 0.5f - 4f) * xDirection +
                            ((float)num25_row * 0.5f + (float)spawnpointRow - 10f) * zDirection);
                        if (Debugger.Enabled)
                            Debugger.LogFormat("WIDTH-FIX: cellW={0} old_num25={1} new_num25={2} num15={3} num16={4}",
                                buildingInfo.m_cellWidth, old25, num25_row, num15, num16);
                    }
                    else if (Debugger.Enabled)
                    {
                        Debugger.LogFormat("WIDTH-FIX: no fix needed cellW={0} == width={1}",
                            buildingInfo.m_cellWidth, width);
                    }

                    if (Debugger.Enabled)
                    {
                        Debugger.LogFormat("SPAWN: name={0} cellW={1} cellL={2} reqW={3} reqL={4} num25_row={5} num15={6} num16={7} num19={8} num20={9} num28={10}",
                            buildingInfo.name, buildingInfo.m_cellWidth, buildingInfo.m_cellLength,
                            width, length, num25_row, num15, num16, num19, num20, num28);
                    }
                    break;
                }

                goto IL_DF0;
            }

            IL_AfterLoop:
            if (buildingInfo == null)
            {
                if (Debugger.Enabled)
                {
                    Debugger.LogFormat("No prefab found: {0}, {1}, {2}, {3} x {4}",
                        service, subService, level, width, length);
                }
                // Mirror the pattern used in GetUpgradeInfoPatch and GetRandomBuildingInfoPatch:
                // s_intentionalNull=true means strict mode deliberately produced no result → leave
                // lot empty. s_intentionalNull=false means vanilla fallback is configured → return
                // true so the original ZoneBlock.SimulationStep runs and picks from vanilla's pool
                // (which respects the active specialization, DLC ownership, etc.).
                bool intentionalSpawnNull = RandomBuildings.s_intentionalNull;
                RandomBuildings.s_intentionalNull = false;
                return !intentionalSpawnNull;
            }

            float num29 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(vector6));
            if (num29 > vector6.y || Singleton<DisasterManager>.instance.IsEvacuating(vector6))
            {
                return false;
            }

            // Adjacency + wall-to-wall snap (preferAdjacent). One same-row neighbour search drives
            // BOTH decisions: if a snappable neighbour exists, slide vector6 so the new building's
            // mesh edge meets it; if none exists, the building would spawn isolated, so skip this
            // tick (with a miss-counter fallback so the first building in an empty district can
            // still land and seed the cluster).
            byte snapDistrict = instance2.GetDistrict(vector6);
            if (BuildingThemesManager.instance.GetDistrictPreferAdjacent(snapDistrict))
            {
                bool found = SnapToAdjacentBuilding(
                    ref vector6, m_angle, xDirection, zDirection,
                    buildingInfo, length, width);
                if (found)
                {
                    s_adjacentMissCount[snapDistrict] = 0;
                }
                else
                {
                    s_adjacentMissCount[snapDistrict]++;
                    bool suspending = s_adjacentMissCount[snapDistrict] >= ADJACENT_FALLBACK_THRESHOLD;
                    // Routine skips are silent; only log the (rare) fallback that lets an isolated
                    // building seed a new cluster.
                    if (suspending && Debugger.Enabled)
                        Debugger.LogFormat("[ADJ] district={0}: no neighbour after {1} tries — allowing isolated spawn of {2} to seed a cluster",
                            snapDistrict, ADJACENT_FALLBACK_THRESHOLD, buildingInfo.name);
                    if (!suspending) return false;
                }
            }

            float num30 = m_angle + (float)Math.PI / 2f;
            if (zoningMode3 == BuildingInfo.ZoningMode.CornerLeft && buildingInfo.m_zoningMode == BuildingInfo.ZoningMode.CornerRight)
            {
                num30 -= (float)Math.PI / 2f;
                length = width;
            }
            else if (zoningMode3 == BuildingInfo.ZoningMode.CornerRight && buildingInfo.m_zoningMode == BuildingInfo.ZoningMode.CornerLeft)
            {
                num30 += (float)Math.PI / 2f;
                length = width;
            }

            if (Singleton<BuildingManager>.instance.CreateBuilding(
                out var building, ref Singleton<SimulationManager>.instance.m_randomizer,
                buildingInfo, vector6, num30, length,
                Singleton<SimulationManager>.instance.m_currentBuildIndex))
            {
                Singleton<SimulationManager>.instance.m_currentBuildIndex++;
                switch (service)
                {
                    case ItemClass.Service.Residential:
                        zoneManager.m_actualResidentialDemand = Mathf.Max(0, zoneManager.m_actualResidentialDemand - 5);
                        break;
                    case ItemClass.Service.Commercial:
                        zoneManager.m_actualCommercialDemand = Mathf.Max(0, zoneManager.m_actualCommercialDemand - 5);
                        break;
                    case ItemClass.Service.Industrial:
                        zoneManager.m_actualWorkplaceDemand = Mathf.Max(0, zoneManager.m_actualWorkplaceDemand - 5);
                        break;
                    case ItemClass.Service.Office:
                        zoneManager.m_actualWorkplaceDemand = Mathf.Max(0, zoneManager.m_actualWorkplaceDemand - 5);
                        break;
                }
                if (zone == ItemClass.Zone.ResidentialHigh || zone == ItemClass.Zone.CommercialHigh)
                {
                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].m_flags
                        |= Building.Flags.HighDensity;
                }
            }
            zoneManager.m_goodAreaFound[(int)zone] = 1024;

            return false; // always skip original — we've done all the work
        }
    }
}
