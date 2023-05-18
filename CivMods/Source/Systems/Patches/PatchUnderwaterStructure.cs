using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.ServerMods;

namespace CivMods
{

    [HarmonyPatch(typeof(WorldGenStructure), "TryGenerateUnderwater")]
    internal class PatchUnderwaterStructure
    {
        internal static bool Prefix(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos pos, ref bool __result, WorldGenStructure __instance, LCGRandom ___rand, BlockPos ___tmpPos, BlockSchematicStructure[][] ___schematicDatas, int[] ___replaceblockids, int ___climateUpLeft, int ___climateUpRight, int ___climateBotLeft, int ___climateBotRight)
        {
			__result = false;
			int num = ___rand.NextInt(___schematicDatas.Length);
			int num2 = ___rand.NextInt(4);
			BlockSchematicStructure blockSchematicStructure = ___schematicDatas[num][num2];
			int num3 = (int)Math.Ceiling(blockSchematicStructure.SizeX / 2f);
			int num4 = (int)Math.Ceiling(blockSchematicStructure.SizeZ / 2f);
			if (blockAccessor.GetTerrainMapheightAt(pos) + 1 != worldForCollectibleResolve.SeaLevel)
			{
				return false;
			}
			___tmpPos.Set(pos.X - num3, 0, pos.Z - num4);
			if (blockAccessor.GetTerrainMapheightAt(___tmpPos) + 1 != worldForCollectibleResolve.SeaLevel)
			{
				return false;
			}
			___tmpPos.Set(pos.X + num3, 0, pos.Z - num4);
			if (blockAccessor.GetTerrainMapheightAt(___tmpPos) + 1 != worldForCollectibleResolve.SeaLevel)
			{
				return false;
			}
			___tmpPos.Set(pos.X - num3, 0, pos.Z + num4);
			if (blockAccessor.GetTerrainMapheightAt(___tmpPos) + 1 != worldForCollectibleResolve.SeaLevel)
			{
				return false;
			}
			___tmpPos.Set(pos.X + num3, 0, pos.Z + num4);
			if (blockAccessor.GetTerrainMapheightAt(___tmpPos) + 1 != worldForCollectibleResolve.SeaLevel)
			{
				return false;
			}
			___tmpPos.Set(pos);
			Block block;
			while ((block = blockAccessor.GetBlock(pos)).IsLiquid() || block.BlockId == 0)
			{
				pos.Y--;
			}
			pos.Y += 2;
			___tmpPos.Set(pos.X - num3, pos.Y + blockSchematicStructure.SizeY + __instance.OffsetY, pos.Z - num4);
			if (!blockAccessor.GetBlock(___tmpPos).IsLiquid())
			{
				return false;
			}
			___tmpPos.Set(pos.X + num3, pos.Y + blockSchematicStructure.SizeY + __instance.OffsetY, pos.Z - num4);
			if (!blockAccessor.GetBlock(___tmpPos).IsLiquid())
			{
				return false;
			}
			___tmpPos.Set(pos.X - num3, pos.Y + blockSchematicStructure.SizeY + __instance.OffsetY, pos.Z + num4);
			if (!blockAccessor.GetBlock(___tmpPos).IsLiquid())
			{
				return false;
			}
			___tmpPos.Set(pos.X + num3, pos.Y + blockSchematicStructure.SizeY + __instance.OffsetY, pos.Z + num4);
			if (!blockAccessor.GetBlock(___tmpPos).IsLiquid())
			{
				return false;
			}
			if (!__instance.satisfiesMinDistance(pos, worldForCollectibleResolve))
			{
				return false;
			}
			if (__instance.isStructureAt(pos, worldForCollectibleResolve))
			{
				return false;
			}
			pos.Y -= 2;
			__instance.LastPlacedSchematicLocation.Set(pos.X, pos.Y, pos.Z, pos.X + blockSchematicStructure.SizeX, pos.Y + blockSchematicStructure.SizeY, pos.Z + blockSchematicStructure.SizeZ);
			__instance.LastPlacedSchematic = blockSchematicStructure;
			blockSchematicStructure.Place(blockAccessor, worldForCollectibleResolve, pos);
			__result = true;
			return false;
		}
    }
}