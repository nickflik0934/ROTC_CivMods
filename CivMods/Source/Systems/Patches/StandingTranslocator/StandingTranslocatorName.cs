using HarmonyLib;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CivMods
{
    [HarmonyPatch(typeof(BlockStaticTranslocator), nameof(BlockStaticTranslocator.GetPlacedBlockName))]
	internal class StandingTranslocatorName
    {
		internal static void Postfix(IWorldAccessor world, BlockPos pos, ref string __result)
        {
			CharacterSystem modSys = world.Api.ModLoader.GetModSystem<CharacterSystem>();

			var chrClass = StandingTranslocator.GetClass(pos, modSys);
			string name = Lang.Get("characterclass-" + chrClass.Code);

			if (!__result.Contains(name))
			{
				StringBuilder bdr = new StringBuilder(__result);
				bdr.Append(string.Format(" of the {0}", name));
				__result = bdr.ToString();
			}
		}
    }
}