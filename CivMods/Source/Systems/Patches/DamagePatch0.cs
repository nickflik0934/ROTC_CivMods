using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CivMods
{
    /*
    [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.DamageItem))]
    public class DamagePatch0
    {
        public static bool Replacing(ItemSlot slot, EnumTool? tool)
        {
            return tool.HasValue && !(slot is DummySlot);
        }

        public static void Prefix()
        {
            CivModSystem.serverConfig.Load();
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            const int origIndex = 42;
            int count = instructions.Count();

            var call = CodeInstruction.Call(typeof(DamagePatch0), nameof(DamagePatch0.Replacing));
            var ldArg3 = new CodeInstruction(OpCodes.Ldarg_3);
            var ldArg0 = new CodeInstruction(OpCodes.Ldarg_0);
            CodeInstruction curInst;

            for (int i = 0; i < count; i++)
            {
                if (i == origIndex && (CivModSystem.serverConfig?.DummySlotDamagePatch ?? false))
                {
                    yield return ldArg3;
                    yield return ldArg0; i++;
                    curInst = instructions.ElementAt(i);
                    yield return curInst; i++;
                    yield return call; i++;
                }
                
                curInst = instructions.ElementAt(i);
                yield return curInst;
            }
        }
    }
    */
}