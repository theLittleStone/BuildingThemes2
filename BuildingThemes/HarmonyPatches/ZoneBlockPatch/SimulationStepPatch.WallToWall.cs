using System;
using System.Runtime.CompilerServices;
using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using UnityEngine;

namespace BuildingThemes.HarmonyPatches.ZoneBlockPatch
{
    public static partial class SimulationStepPatch
    {
        // Buildings in CS1 are bucketed in a 270×270 grid with 64 m cells.
        private const int BUILDING_GRID_RES = 270;
        private const float BUILDING_GRID_CELL = 64f;
        private const int BUILDING_GRID_HALF = BUILDING_GRID_RES / 2;

        // Maximum metres we'll shift the new building along a road to make it touch its
        // neighbour. Large enough to pull a building across one empty cell (8 m) plus the
        // typical mesh inset, so a building that spawned a cell away still snaps flush.
        private const float SNAP_MAX_SHIFT = 10f;
        // Buildings whose centre is further than this along the move axis are not candidates —
        // too far away to be the "next building" on the same frontage line.
        private const float SNAP_ALONG_MAX = 40f;
        // How far off the frontage line a candidate may sit and still count as "same row".
        private const float SNAP_PERP_TOL = 2f;

        // Straight-lot snap: slide the building along the road it fronts (zDirection) until its
        // edge meets the nearest building on the same frontage line. Returns true when a
        // clustering neighbour exists (touching or with a closeable gap) — the caller uses this
        // as the "is this spawn adjacent?" signal for the miss-counter.
        private static bool SnapToAdjacentBuilding(
            ref Vector3 vector6, float roadAngle, Vector2 xDirection, Vector2 zDirection,
            BuildingInfo newInfo)
        {
            if (newInfo == null) return false;

            // A straight building's stored angle is roadAngle + π/2 (no corner flip). Its
            // frontage runs along the road (zDirection); depth runs perpendicular (xDirection).
            float worldAngle = roadAngle + Mathf.PI * 0.5f;
            Vector2 moveAxis = zDirection / 8f;  // along the road
            Vector2 lineAxis = xDirection / 8f;  // perpendicular (depth) — the frontage line

            bool found = TryFindSnap(vector6, worldAngle, newInfo, moveAxis, lineAxis, out float shift);
            if (shift != 0f)
            {
                // Validate before committing: the leading edge (toward the neighbour) must not
                // land on a perpendicular road/path. If it would, the chosen neighbour is across
                // a road — revert the snap and leave the building at its vanilla position.
                float selfHalfMove = ExtentAlongAxis(newInfo, worldAngle, moveAxis);
                Vector3 lead = vector6;
                lead.x += moveAxis.x * (shift + Mathf.Sign(shift) * selfHalfMove);
                lead.z += moveAxis.y * (shift + Mathf.Sign(shift) * selfHalfMove);

                if (OverlapsCrossRoad(lead, moveAxis))
                {
                    if (Debugger.Enabled)
                        Debugger.LogFormat("[ADJ-SNAP] {0}: skipped {1:F2} m snap (would cross a road)",
                            newInfo.name, shift);
                }
                else
                {
                    Vector2 delta = moveAxis * shift;
                    vector6.x += delta.x;
                    vector6.z += delta.y;
                    if (Debugger.Enabled)
                        Debugger.LogFormat("[ADJ-SNAP] {0} angle={1:F0}°: snapped {2:F2} m along road",
                            newInfo.name, worldAngle * Mathf.Rad2Deg, shift);
                }
            }
            return found;
        }

        // World half-extent of a building's footprint projected onto a unit XZ axis, using the
        // building's stored angle θ. Basis is anchored to the (verified) straight-spawn layout:
        // a building's frontage (mesh.x / m_cellWidth) runs along (cos θ, sin θ) and its depth
        // (mesh.z / m_cellLength) along (sin θ, -cos θ). |dot| is taken, so only the axis (not
        // its sign) matters. Mesh dims are capped at the cell allocation (×8 m) so garden/
        // landscape props that spill past the lot don't bloat the extent.
        private static float ExtentAlongAxis(BuildingInfo info, float angle, Vector2 axisUnit)
        {
            float capX = info.m_cellWidth  * 8f;
            float capZ = info.m_cellLength * 8f;
            float sizeX = capX, sizeZ = capZ;
            if (info.m_mesh != null && info.m_mesh.bounds.size.x > 0f)
            {
                var s = info.m_mesh.bounds.size;
                sizeX = Mathf.Min(s.x, capX);
                sizeZ = Mathf.Min(s.z, capZ);
            }
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            Vector2 frontDir = new Vector2(cos,  sin);  // frontage / mesh.x world direction
            Vector2 depthDir = new Vector2(sin, -cos);  // depth / mesh.z world direction
            return 0.5f * (sizeX * Mathf.Abs(Vector2.Dot(frontDir, axisUnit))
                         + sizeZ * Mathf.Abs(Vector2.Dot(depthDir, axisUnit)));
        }

        // 2D distance from point p to segment [a,b].
        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f) return (p - a).magnitude;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return (p - (a + ab * t)).magnitude;
        }

        // True when `point` lies on a road that runs roughly PERPENDICULAR to `alongDir` — i.e. a
        // cross street. Used to validate a snap before committing: snapping a building along its
        // own road must never push it across a perpendicular road/path. The building's OWN
        // fronting road runs parallel to alongDir and is excluded by the perpendicularity test,
        // so this only flags genuine cross roads. The net segment grid is the same 270×270 @ 64 m
        // grid as buildings. Only ItemClass.Service.Road segments count (this also covers the
        // narrow wall-to-wall pedestrian streets from Plazas & Promenades).
        private static bool OverlapsCrossRoad(Vector3 point, Vector2 alongDir)
        {
            var nm = Singleton<NetManager>.instance;
            if (nm == null) return false;

            Vector2 p = new Vector2(point.x, point.z);
            int gx = Mathf.Clamp((int)(point.x / BUILDING_GRID_CELL + BUILDING_GRID_HALF), 0, BUILDING_GRID_RES - 1);
            int gz = Mathf.Clamp((int)(point.z / BUILDING_GRID_CELL + BUILDING_GRID_HALF), 0, BUILDING_GRID_RES - 1);

            for (int z = Mathf.Max(0, gz - 1); z <= Mathf.Min(BUILDING_GRID_RES - 1, gz + 1); z++)
            {
                for (int x = Mathf.Max(0, gx - 1); x <= Mathf.Min(BUILDING_GRID_RES - 1, gx + 1); x++)
                {
                    ushort id = nm.m_segmentGrid[z * BUILDING_GRID_RES + x];
                    int safety = 0;
                    while (id != 0)
                    {
                        NetSegment seg = nm.m_segments.m_buffer[id];
                        NetInfo info = seg.Info;
                        if (info != null && info.m_class != null
                            && info.m_class.m_service == ItemClass.Service.Road
                            && seg.m_startNode != 0 && seg.m_endNode != 0)
                        {
                            Vector3 a3 = nm.m_nodes.m_buffer[seg.m_startNode].m_position;
                            Vector3 b3 = nm.m_nodes.m_buffer[seg.m_endNode].m_position;
                            Vector2 segDir = new Vector2(b3.x - a3.x, b3.z - a3.z);
                            float segLen = segDir.magnitude;
                            if (segLen > 0.1f)
                            {
                                segDir /= segLen;
                                // Only cross streets (perpendicular-ish to our travel). Roads
                                // parallel to alongDir (our fronting road, or parallel roads) are
                                // skipped so we don't flag the building's own legitimate frontage.
                                if (Mathf.Abs(Vector2.Dot(segDir, alongDir)) < 0.5f)
                                {
                                    float dist = DistancePointToSegment(
                                        p, new Vector2(a3.x, a3.z), new Vector2(b3.x, b3.z));
                                    if (dist < info.m_halfWidth + 0.5f)
                                        return true;
                                }
                            }
                        }
                        id = nm.m_segments.m_buffer[id].m_nextGridSegment;
                        if (++safety >= 36864) break;
                    }
                }
            }
            return false;
        }

        // Corner-lot snap: nudge a freshly-placed corner building toward the neighbours down
        // BOTH streets. The block's road (zDirection) and the cross-street (xDirection) are
        // perpendicular, so each gap is closed on its own axis and the two shifts simply add.
        // Never suppresses placement; only moves to close existing gaps.
        private static void SnapCornerToNeighbours(
            ref Vector3 vector6, Vector2 xDirection, Vector2 zDirection,
            float worldAngle, BuildingInfo newInfo)
        {
            if (newInfo == null) return;

            Vector2 axisRoad  = zDirection / 8f;  // along the block's road
            Vector2 axisCross = xDirection / 8f;  // along the perpendicular cross-street

            bool foundRoad  = TryFindSnap(vector6, worldAngle, newInfo, axisRoad,  axisCross, out float shiftRoad);
            bool foundCross = TryFindSnap(vector6, worldAngle, newInfo, axisCross, axisRoad,  out float shiftCross);

            // Validate each axis independently: cancel a shift whose leading edge would land on a
            // perpendicular road (the neighbour is across a road). The two axes are perpendicular,
            // so each is checked against roads running across its own travel direction.
            if (shiftRoad != 0f)
            {
                float halfRoad = ExtentAlongAxis(newInfo, worldAngle, axisRoad);
                Vector3 lead = vector6;
                lead.x += axisRoad.x * (shiftRoad + Mathf.Sign(shiftRoad) * halfRoad);
                lead.z += axisRoad.y * (shiftRoad + Mathf.Sign(shiftRoad) * halfRoad);
                if (OverlapsCrossRoad(lead, axisRoad)) shiftRoad = 0f;
            }
            if (shiftCross != 0f)
            {
                float halfCross = ExtentAlongAxis(newInfo, worldAngle, axisCross);
                Vector3 lead = vector6;
                lead.x += axisCross.x * (shiftCross + Mathf.Sign(shiftCross) * halfCross);
                lead.z += axisCross.y * (shiftCross + Mathf.Sign(shiftCross) * halfCross);
                if (OverlapsCrossRoad(lead, axisCross)) shiftCross = 0f;
            }

            if (shiftRoad != 0f || shiftCross != 0f)
            {
                Vector2 delta = axisRoad * shiftRoad + axisCross * shiftCross;
                vector6.x += delta.x;
                vector6.z += delta.y;
            }

            // Always log corner placements (corners are rare) so a test session can confirm the
            // corner path runs and see, per axis, whether a neighbour was found and how far we
            // moved — even when nothing moved (no neighbour yet, or already flush).
            if (Debugger.Enabled)
                Debugger.LogFormat("[ADJ-CORNER] {0} ({1}) angle={2:F0}°: road(found={3} shift={4:F2}) cross(found={5} shift={6:F2})",
                    newInfo.name, newInfo.m_zoningMode, worldAngle * Mathf.Rad2Deg,
                    foundRoad, shiftRoad, foundCross, shiftCross);
        }

        // Core snap primitive (used by both the straight and corner snaps). Searches the 3×3
        // building grid for the nearest building on the same frontage line as `pos` — i.e. whose
        // perpendicular offset along `lineAxis` overlaps ours — and reports the signed shift
        // along `moveAxis` that brings our edge flush against it.
        //
        // There is NO orientation filter on candidates: we only ever move along OUR own frontage
        // axis, which can't push into the road or past the cap, so the neighbour's own facing is
        // irrelevant. This is what lets a building snap to a perpendicular neighbour (a corner)
        // that an angle filter would wrongly reject. The user opts into snapping per district.
        //
        // Returns true when a qualifying neighbour exists at all (touching OR with a closeable
        // gap); `shift` is 0 when already touching or when none is found.
        private static bool TryFindSnap(
            Vector3 pos, float worldAngle, BuildingInfo newInfo,
            Vector2 moveAxis, Vector2 lineAxis, out float shift)
        {
            shift = 0f;
            var bm = Singleton<BuildingManager>.instance;
            float selfHalfMove = ExtentAlongAxis(newInfo, worldAngle, moveAxis);
            float selfHalfLine = ExtentAlongAxis(newInfo, worldAngle, lineAxis);

            int gx = Mathf.Clamp((int)(pos.x / BUILDING_GRID_CELL + BUILDING_GRID_HALF), 0, BUILDING_GRID_RES - 1);
            int gz = Mathf.Clamp((int)(pos.z / BUILDING_GRID_CELL + BUILDING_GRID_HALF), 0, BUILDING_GRID_RES - 1);

            float bestShift = 0f, bestAbs = float.MaxValue;
            bool found = false;

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
                            Vector2 d = new Vector2(b.m_position.x - pos.x, b.m_position.z - pos.z);
                            float along = Vector2.Dot(d, moveAxis);
                            float line  = Vector2.Dot(d, lineAxis);

                            float exHalfMove = ExtentAlongAxis(b.Info, b.m_angle, moveAxis);
                            float exHalfLine = ExtentAlongAxis(b.Info, b.m_angle, lineAxis);

                            // Same frontage line: perpendicular gap ~0 (they overlap across the
                            // street). Excludes across-street and back-row buildings.
                            float perpGap = Mathf.Abs(line) - selfHalfLine - exHalfLine;
                            float alongAbs = Mathf.Abs(along);
                            float gap = alongAbs - selfHalfMove - exHalfMove;

                            if (perpGap <= SNAP_PERP_TOL && alongAbs <= SNAP_ALONG_MAX && gap <= SNAP_MAX_SHIFT)
                            {
                                // A neighbour we cluster against (touching when gap <= 0, or with
                                // a closeable gap). Counts for the "adjacent?" signal either way.
                                found = true;
                                if (gap > 0.05f)
                                {
                                    float s = along > 0f ? gap : -gap;  // toward the neighbour
                                    if (Mathf.Abs(s) < bestAbs)
                                    {
                                        bestAbs = Mathf.Abs(s);
                                        bestShift = s;
                                    }
                                }
                            }
                        }
                        id = b.m_nextGridBuilding;
                        if (++safety >= 49152) break;
                    }
                }
            }

            if (bestAbs != float.MaxValue) shift = bestShift;
            return found;
        }
    }
}
