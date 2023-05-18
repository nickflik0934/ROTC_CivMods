using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace CivMods
{
    internal class BlockBehaviorDelete : BlockBehavior
    {

        public BlockBehaviorDelete(Block block) : base(block)
        {
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            base.OnBlockPlaced(world, pos, ref handling);

            world.BlockAccessor.SetBlock(block.Id, pos.AddCopy(0,1,0));
        }
    }
}