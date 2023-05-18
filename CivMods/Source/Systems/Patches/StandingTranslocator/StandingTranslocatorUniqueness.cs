using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CivMods
{
    [HarmonyPatch(typeof(BlockEntityStaticTranslocator), nameof(BlockEntityStaticTranslocator.OnTesselation))]
	internal class StandingTranslocatorUniqueness
    {
		internal static void Postfix(BlockEntityStaticTranslocator __instance, ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
			var capi = __instance.Api as ICoreClientAPI;
			CharacterSystem modSys = __instance.Api.ModLoader.GetModSystem<CharacterSystem>();
			CharacterClass chrClass = StandingTranslocator.GetClass(__instance.Pos, modSys);
			BlockPos pos = __instance.Pos;

			int gearIndex = GameMath.Mod(GameMath.MurmurHash3(pos.X, pos.Y, pos.Z), chrClass.Gear.Length);

            foreach (var renderedStack in chrClass.Gear)
            {
				if (renderedStack.Resolve(__instance.Api.World, ""))
				{
					var stack = renderedStack.ResolvedItemstack;
					string key = string.Format("civmods:{0}", renderedStack.Code);

					__instance.Api.Event.EnqueueMainThreadTask(() =>
					{
						var mesh = stack.Item<ItemWearable>().GenMesh(stack, capi.BlockTextureAtlas);
						if (__instance.Api.ObjectCache.ContainsKey(key))
						{
							__instance.Api.ObjectCache.Remove(key);
						}
						__instance.Api.ObjectCache.Add(key, mesh);
					}, "");

					object dat = null;

					//Wait for main thread to be done
					while (!__instance.Api.ObjectCache.TryGetValue(key, out dat)) ; ;

					MeshData meshDat = (dat as MeshData).Clone();
					BlockFacing facing = BlockFacing.FromCode(__instance.Block.Variant["side"]);
					meshDat.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, GameMath.DEG2RAD * facing.HorizontalAngleIndex * 90.0f, 0);
					
					meshDat.Translate(0f, 0.2f, 0f);

					for (int i = 0; i < meshDat.RenderPassesAndExtraBits.Length; i++)
                    {
						meshDat.RenderPassesAndExtraBits[i] &= 0x1FF;
						meshDat.RenderPassesAndExtraBits[i] |= (short)EnumChunkRenderPass.Transparent;
					}

					mesher.AddMeshData(meshDat);
				}
			}
		}
    }
}