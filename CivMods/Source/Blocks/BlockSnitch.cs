using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace CivMods
{
    internal class BlockSnitch : Block
    {
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            if (world.GetBlockEntitiesAround(pos, new Vec2i(11, 11)).Any(e => (e is BlockEntitySnitch)))
            {
                world.RegisterCallback(dt => world.BlockAccessor.BreakBlock(pos, null), 500);
            }
            base.OnBlockPlaced(world, pos, byItemStack);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntitySnitch be = (blockSel?.Position?.BlockEntity(world) as BlockEntitySnitch);

            if (world.Side == EnumAppSide.Server)
            {
                IServerPlayer serverPlayer = (IServerPlayer)byPlayer;

                if (world.Api.ModLoader.GetModSystem<CivModSystem>().TryAccessSnitch(be, serverPlayer))
                {
                    ICoreServerAPI sapi = (ICoreServerAPI)world.Api;

                    if (be != null)
                    {
                        if (be.OwnerUID == null || be.OwnerUID == "")
                        {
                            be.OwnerUID = byPlayer.PlayerUID;
                            sapi.SendMessage(byPlayer, 0, "You now own this snitch.", EnumChatType.OwnMessage);
                        }
                        else if (byPlayer.Entity.Controls.Sneak)
                        {
                            if (byPlayer.Entity.Controls.Sprint)
                            {
                                be.Alarm = !be.Alarm;
                                sapi.SendMessage(byPlayer, 0, string.Format("Snitch alarm is now {0}.", be.Alarm ? "on" : "off"), EnumChatType.OwnMessage);
                            }
                            else
                            {
                                be.Mode = (EnumSnitchMode)(((int)be.Mode + 1) % 2);
                                sapi.SendMessage(byPlayer, 0, string.Format("Snitch is now in {0} infraction mode.", be.Mode), EnumChatType.OwnMessage);
                            }
                        }
                    }
                }
            }
            else (world.Api as ICoreClientAPI)?.SendChatMessage("/snitchinfo");

            be?.MarkDirty();

            return true;
        }
    }
}