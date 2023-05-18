using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CivMods
{

    [HarmonyPatch(typeof(BlockEntitySapling), "CheckGrow")]
    internal class GenTreeFix
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var decMethod = AccessTools.GetDeclaredMethods(typeof(ITreeGenerator)).Where(m => m.Name == "GrowTree").Single();

            foreach (var inst in instructions)
            {
                if (inst.Calls(decMethod))
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 37.5f);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                }
                yield return inst;
            }
        }
    }
}