using HarmonyLib;
using System.Text;
using Vintagestory.GameContent;

namespace CivMods
{
    [HarmonyPatch(typeof(BlockStaticTranslocator), nameof(BlockStaticTranslocator.GetPlacedBlockInfo))]
	internal class StandingTranslocatorInfo
	{
		internal static void Postfix(ref string __result)
		{
			StringBuilder bdr = new StringBuilder(__result);
			bdr.AppendLine();
			bdr.Append("Sneak right click with an empty hand to adopt this class.");
			__result = bdr.ToString();
		}
	}
}