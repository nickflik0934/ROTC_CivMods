namespace CivMods
{
    /*
    public class EntityLobotomizer
    {
        public static bool Common(EntityBehavior __instance)
        {
            if (__instance.entity is EntityPlayer) return true;

            Vec3d tgt = __instance.entity.ServerPos.XYZ;

            IPlayer[] players = __instance.entity.World.GetPlayersAround(tgt, 64, 64) ?? new IPlayer[0];

            return players.Length != 0;
        }
    }

    [HarmonyPatch(typeof(EntityBehaviorTaskAI), "OnGameTick")]
    class EntityLobotomizer0
    {
        public static bool Prefix(EntityBehaviorTaskAI __instance)
        {
            return EntityLobotomizer.Common(__instance);
        }
    }

    [HarmonyPatch(typeof(EntityBehaviorControlledPhysics), "OnGameTick")]
    class EntityLobotomizer1
    {
        public static bool Prefix(EntityBehaviorControlledPhysics __instance)
        {
            return EntityLobotomizer.Common(__instance);
        }
    }

    [HarmonyPatch(typeof(BlockEntityBerryBush), "Initialize")]
    class EntityLobotomizer2
    {
        public static bool Prefix()
        {
            return false;
        }

        public static void Postfix(BlockEntityBerryBush __instance, ICoreAPI api)
        {
            __instance.Api = api;

            foreach (var val in __instance.Behaviors)
            {
                val.Initialize(api, val.properties);
            }

            if (api is ICoreServerAPI)
            {
                if (__instance.GetField<double>("transitionHoursLeft") <= 0)
                {
                    __instance.SetField("transitionHoursLeft", __instance.GetHoursForNextStage());
                    __instance.SetField("lastCheckAtTotalDays", api.World.Calendar.TotalDays);
                }

                __instance.RegisterGameTickListener((dt) => __instance.CallMethod("CheckGrow", dt), 120000);

                api.ModLoader.GetModSystem<POIRegistry>().AddPOI(__instance);
                __instance.SetField("roomreg", api.ModLoader.GetModSystem<RoomRegistry>());
                double? d = __instance.GetField<double?>("totalDaysForNextStageOld");

                if (d != null)
                {
                    __instance.SetField("transitionHoursLeft", ((double)d - api.World.Calendar.TotalDays) * api.World.Calendar.HoursPerDay);
                }
            }
        }
    }
    */
}