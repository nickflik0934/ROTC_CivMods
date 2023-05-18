using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CivMods
{
    internal class BlockStackableUnderwater : BlockSeaweed
    {
		internal UnderWaterGenProps genProps;
		internal Block[] Blocks { get => this.GetField<Block[]>("blocks"); set => this.SetField("blocks", value); }
		internal Random Random { get => this.GetField<Random>("random"); set => this.SetField("random", value); }
		public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            genProps = Attributes["UnderWaterGenProps"].AsObject<UnderWaterGenProps>();
		}

        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldGenRand)
        {
            //api.World.BlockAccessor.SetBlock(0, pos);

            BlockPos belowPos = pos.DownCopy();

            Block block = blockAccessor.GetBlock(belowPos);
            if (block.LiquidCode != genProps.LiquidCode) return false;

            int depth = genProps.MinDepth;
            while (depth < genProps.MaxDepth)
            {
                belowPos.Down();
                block = blockAccessor.GetBlock(belowPos);

                if (block.Fertility > 0)
                {
                    belowPos.Up();
                    PlaceStackable(blockAccessor, belowPos, depth);
                    return true;
                }
                else
                {
                    if (!block.IsLiquid()) return false;
                }

                depth++;
            }

            return false;
		}

		private void PlaceStackable(IBlockAccessor blockAccessor, BlockPos pos, int depth)
		{
			int height = Random.Next((int)Math.Round(depth * genProps.Dampening), depth);
			int placed = 0;

			if (Blocks == null)
			{
				Blocks = new Block[]
				{
					blockAccessor.GetBlock(genProps.BottomSection),
					blockAccessor.GetBlock(genProps.MiddleSection),
					blockAccessor.GetBlock(genProps.TopSection),
				};
			}

			while (placed < genProps.MaxSections && height-- > 0)
			{
				int id = height == depth - 1 ? Blocks[0].Id : placed++ == genProps.MaxSections - 1 || height == 0 ? Blocks[2].Id : Blocks[1].Id;
				blockAccessor.SetBlock(id, pos);
				pos.Up();
			}
		}
	}
}