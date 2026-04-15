using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Math;
using System;

namespace BuildingThemes.HarmonyPatches.PrivateBuildingAIPatch
{
    public static class GetUpgradeInfoPatch
    {
        private static bool deployed;

        // GetUpgradeInfo is declared only on PrivateBuildingAI and is not overridden
        // by any of the four subclasses — one patch on the base class covers them all.
        private static readonly Type[] AiTypes =
        {
            typeof(PrivateBuildingAI),
        };

        public static void Deploy()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || deployed) return;

            foreach (var aiType in AiTypes)
            {
                PatchUtil.Patch(
                    new PatchUtil.MethodDefinition(aiType, "GetUpgradeInfo"),
                    prefix: new PatchUtil.MethodDefinition(typeof(GetUpgradeInfoPatch), nameof(Prefix)));
                Debugger.LogFormat("Building Themes: {0}.GetUpgradeInfo patched.", aiType.Name);
            }

            deployed = true;
        }

        public static void Revert()
        {
            if (!HarmonyHelper.IsHarmonyInstalled || !deployed) return;

            foreach (var aiType in AiTypes)
            {
                PatchUtil.Unpatch(new PatchUtil.MethodDefinition(aiType, "GetUpgradeInfo"));
                Debugger.LogFormat("Building Themes: {0}.GetUpgradeInfo unpatched.", aiType.Name);
            }

            deployed = false;
        }

        // Prefix: select a themed upgrade building.
        // If no themed building is found, fall through to the original.
        public static bool Prefix(ushort buildingID, ref Building data, ref BuildingInfo __result)
        {
            // This method is very fragile — no logging here.
            BuildingInfo info = data.Info;

            Randomizer randomizer = new Randomizer((int)buildingID);
            for (int i = 0; i <= (int)info.m_class.m_level; i++)
            {
                randomizer.Int32(1000u);
            }

            ItemClass.Level level = info.m_class.m_level + 1;
            DistrictManager instance = Singleton<DistrictManager>.instance;
            byte district = instance.GetDistrict(data.m_position);
            ushort style = instance.m_districts.m_buffer[(int)district].m_Style;

            __result = RandomBuildings.GetRandomBuildingInfo_Upgrade(
                data.m_position, data.m_infoIndex,
                ref randomizer,
                info.m_class.m_service, info.m_class.m_subService,
                level, data.Width, data.Length, info.m_zoningMode, style);

            // Consume the intentional-null flag set by strict mode.
            bool intentional = RandomBuildings.s_intentionalNull;
            RandomBuildings.s_intentionalNull = false;

            // Return false (skip original) when we found a themed upgrade OR when strict mode
            // deliberately blocked the upgrade (intentional == true → building stays at current level).
            return __result == null && !intentional;
        }
    }
}
