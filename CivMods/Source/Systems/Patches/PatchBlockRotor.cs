using HarmonyLib;
using System;
using System.Text;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace CivMods
{
    [HarmonyPatch(typeof(BEBehaviorWindmillRotor), "CheckWindSpeed")]
    public class Patch
    { 
        public static bool Prefix(float dt, BEBehaviorWindmillRotor __instance, ref double ___windSpeed, WeatherSystemBase ___weatherSystem, ICoreAPI ___Api, BlockEntity ___Blockentity, int ___sailLength)
        {
            var windSpeed = Traverse.Create(__instance).Field("windSpeed");

            double speed = ___weatherSystem.WeatherDataSlowAccess.GetWindSpeed(___Blockentity.Pos.ToVec3d());

            if (__instance.Api.World.BlockAccessor.GetLightLevel(___Blockentity.Pos, EnumLightLevelType.OnlySunLight) < 5 && ___Api.World.Config.GetString("undergroundWindmills", "false") != "true")
            {
                windSpeed.SetValue(0.0);
            }

            if((double)windSpeed.GetValue() != 0.0)
            {
                windSpeed.SetValue(speed);
            }

            if (__instance.Api.World.Rand.NextDouble() > 0.2)
            {
                return false;
            }

            if (___sailLength == 0)
            {
                return false;
            }

            var publicObstructed = AccessTools.Method(typeof(BEBehaviorWindmillRotor), "obstructed");
            if ((bool)publicObstructed.Invoke(__instance, new object[] { ___sailLength + 1 })) {
                if ((double)windSpeed.GetValue() != 0.0) {
                    ___Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), (double)__instance.Position.X + 0.5, (double)__instance.Position.Y + 0.5, (double)__instance.Position.Z + 0.5, null, randomizePitch: false, 20f);
                }

                windSpeed.SetValue(0.0);
            }
            else
            {
                windSpeed.SetValue(speed);
            }

            return false;
        }
    }

}