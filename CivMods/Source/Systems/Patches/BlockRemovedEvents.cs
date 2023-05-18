using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CivMods
{
    [HarmonyPatch(typeof(Block), "OnBlockRemoved")]
    internal class BlockRemovedEvents
    {
        public static void Postfix(Block __instance, IWorldAccessor world, BlockPos pos)
        {
            var civMods = world.Api.ModLoader.GetModSystem<CivModSystem>();
            switch (world.Side)
            {
                case EnumAppSide.Server:
                    civMods.InvokeOnBlockRemoved(world, pos, __instance);
                    break;

                case EnumAppSide.Client:
                    break;

                case EnumAppSide.Universal:
                    break;

                default:
                    break;
            }
        }
    }
}