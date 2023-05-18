using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CivMods
{
    [HarmonyPatch(typeof(BlockStaticTranslocator), nameof(BlockStaticTranslocator.OnBlockInteractStart))]
	internal class StandingTranslocator
    {
		internal static CharacterClass GetClass(BlockPos pos, CharacterSystem modSys)
        {
			int classIndex = GameMath.Mod(GameMath.MurmurHash3(pos.X, pos.Y, pos.Z), modSys.characterClasses.Count);
			return modSys.characterClasses[classIndex];
		}

		static long BehindDelaySet(IPlayer byPlayer, double delay)
        {
			long time = DateTime.Now.AddDays(-delay).ToBinary();
			byPlayer.Entity.WatchedAttributes.SetLong("civmods-lastClassChange", time);
			return time;
		}
		
		internal static void Prefix(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side.IsServer() && byPlayer.InventoryManager.ActiveHotbarSlot.Empty && byPlayer.Entity.Controls.Sneak)
            {
				var sapi = world.Api as ICoreServerAPI;
				IServerPlayer serverPlayer = (IServerPlayer)byPlayer;

				var pos = blockSel.Position;
				var tl = world.BlockAccessor.GetBlockEntity<BlockEntityStaticTranslocator>(pos);

				CharacterSystem modSys = sapi.ModLoader.GetModSystem<CharacterSystem>();
				var chrClass = GetClass(pos, modSys);

				double delay = CivModSystem.serverConfig.ClassChangeDelayInDays;
				long lastChange = byPlayer.Entity.WatchedAttributes.TryGetLong("civmods-lastClassChange") ?? BehindDelaySet(byPlayer, delay);
				

				if (tl?.FullyRepaired ?? false)
				{
					string langClass = Lang.Get("characterclass-" + chrClass.Code);

					if (byPlayer.Entity.WatchedAttributes.GetString("characterClass") == chrClass.Code)
					{
						sapi.SendIngameError(serverPlayer, "alreadyclass", string.Format("You are already one of the {0}s.", langClass));
					}
					else if (DateTime.FromBinary(lastChange).AddDays(delay) <= DateTime.Now)
					{
						modSys.setCharacterClass(byPlayer.Entity, chrClass.Code, false);
						byPlayer.Entity.WatchedAttributes.SetLong("civmods-lastClassChange", DateTime.Now.ToBinary());
						
						world.PlaySoundFor(new AssetLocation("sounds/weather/tracks/verylowtremble"), byPlayer, false, 32, 2.0f);
						sapi.SendMessage(byPlayer, 0, string.Format("You are now considered a {0} to those around you.", langClass), EnumChatType.OwnMessage);
					}
					else
					{
						sapi.SendIngameError(serverPlayer, "classdelay", string.Format("You can only change your class once every {0} days!", Math.Round(delay, 1)));
					}
				}
				else
				{
					sapi.SendIngameError(serverPlayer, "unrepaired", string.Format("You can only change your class with fully repaired static translocators!"));
				}
				byPlayer.Entity.WatchedAttributes.MarkPathDirty("civmods-lastClassChange");
			}
        }

		internal static void Postfix(ref bool __result)
        {
			__result = true;
        }
    }
}