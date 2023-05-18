using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CivMods
{
    public class BlockEntityAcid : BlockEntity
    {
        public double lastTick;
        public double currentTick;

        public double acidBuildup;
        public int eaten;

        ModSystemBlockReinforcement bref;

        public BlockEntityAcid()
        {
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            lastTick = api.World.Calendar.TotalHours;
            bref = Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
            Tick();
        }

        public void Tick()
        {
            currentTick = Api.World.Calendar.TotalHours;

            double dt = currentTick - lastTick;

            EatReinforcement(dt);

            lastTick = currentTick;
        }


        public void EatReinforcement(double dt)
        {
            BlockPos below = Pos.DownCopy();

            while (Api.World.BlockAccessor.GetBlock(below).Id == 0)
            {
                below.Down();
            }

            if (bref.IsReinforced(below))
            {
                acidBuildup += dt;

                if (acidBuildup > 1)
                {
                    int toEat = (int)(acidBuildup * 1.0);
                    int str = bref.GetReinforcment(below).Strength;

                    bref.ConsumeStrength(below, toEat);
                    toEat = GameMath.Min(str, toEat);
                    acidBuildup = (acidBuildup - (int)acidBuildup);

                    eaten += toEat;
                }
                MarkDirty();
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine(string.Format("Reinforcement Eaten: {0}", eaten));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            lastTick = tree.GetDouble("civmods:acid-lasttick", worldAccessForResolve.Calendar.TotalHours);
            acidBuildup = tree.GetDouble("civmods:acid-buildup");
            eaten = tree.GetInt("civmods:acid-eaten");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("civmods:acid-lasttick", lastTick);
            tree.SetDouble("civmods:acid-buildup", acidBuildup);
            tree.SetInt("civmods:acid-eaten", eaten);
        }
    }
}