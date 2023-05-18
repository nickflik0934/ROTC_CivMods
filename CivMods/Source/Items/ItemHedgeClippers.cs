using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CivMods
{
    internal class ItemHedgeClippers : Item
    {
        ModSystemBlockReinforcement br;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            br = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
        }
        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            BlockPos pos = blockSel.Position;
            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            
            if (world.Side == EnumAppSide.Server)
            {
                if (byEntity.TryAccess(pos, EnumBlockAccessFlags.BuildOrBreak) && !br.IsReinforced(pos))
                {
                    Block spawned = block;
                    
                    if (block is BlockLeaves || block is BlockWithLeavesMotion)
                    {
                        spawned = api.World.GetBlock(block.CodeWithVariant("type", "placed")) ?? block;
                    }
                    else if (block is BlockFruitTreeFoliage)
                    {
                        var be = api.World.BlockAccessor.GetBlockEntity<BlockEntityFruitTreeFoliage>(pos);
                        if (be != null && be.PartType == EnumTreePartType.Leaves)
                        {
                            string slb = "s";
                            switch (be.FruitTreeState)
                            {
                                case EnumFruitTreeState.Empty:
                                case EnumFruitTreeState.DormantVernalized:
                                case EnumFruitTreeState.EnterDormancy:
                                case EnumFruitTreeState.Dormant:
                                case EnumFruitTreeState.Young:
                                    slb += "l";
                                    break;
                                case EnumFruitTreeState.Flowering:
                                case EnumFruitTreeState.Fruiting:
                                case EnumFruitTreeState.Ripe:
                                    slb += "lb";
                                    break;
                                default:
                                case EnumFruitTreeState.Dead:
                                    break;
                            }

                            AssetLocation asset = new AssetLocation(string.Format("civmods:placedfruittreeleaves-{0}-placed-{1}-up", slb, be.TreeType));
                            spawned = api.World.GetBlock(asset);
                        }
                    }
                    else
                    {
                        return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
                    }

                    world.BlockAccessor.SetBlock(0, blockSel.Position);
                    ItemStack stack = new ItemStack(spawned);
                    world.SpawnItemEntity(stack, new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                    world.PlaySoundAt(block.Sounds?.GetBreakSound(EnumTool.Shears), pos.X, pos.Y, pos.Z);
                }
            }
            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
        }
    }
}