using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace CivMods
{
    [HarmonyPatch(typeof(BlockLeaves), nameof(BlockLeaves.OnPickBlock))]
    internal class BlockLeavesPickFix
    {
        public static bool Prefix(BlockLeaves __instance, IWorldAccessor world, ref ItemStack __result)
        {
            if (__instance.Variant.ContainsKey("type"))
            {

                if (__instance.Variant.ContainsKey("rot"))
                {
                    __result = new ItemStack(world.GetBlock(__instance.CodeWithVariants(new string[] { "type", "rot" }, new string[] { "placed", "up" })));
                }
                else
                {
                    __result = new ItemStack(world.GetBlock(__instance.CodeWithVariant("type", "placed")));
                }
                return false;
            }
            return true;
        }
    }
}