using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CivMods
{
    public class BlockAcid : Block
    {
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            world.BlockAccessor.GetBlockEntity<BlockEntityAcid>(pos)?.Tick();
        }

        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            base.ShouldReceiveServerGameTicks(world, pos, offThreadRandom, out extra);
            return true;
        }
    }
}