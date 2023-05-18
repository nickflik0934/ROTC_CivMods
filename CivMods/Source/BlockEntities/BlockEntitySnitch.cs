using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace CivMods
{
    public static class Ext0
    {
        public const string InfractionsKey = "breakintree";

        public static void AddInfraction(this ITreeAttribute tree, Infraction breakin)
        {
            ITreeAttribute breakinTree = tree.GetOrAddTreeAttribute(InfractionsKey);
            long breakinCount = breakinTree.GetLong("count", -1);

            ITreeAttribute breakinV = breakinTree.GetOrAddTreeAttribute(breakinCount.ToString("x16"));

            breakinV.SetLong("thingId", breakin.thingId);
            breakinV.SetInt("infraction", (int)breakin.infraction);
            breakinV.SetInt("locationX", breakin.location.x);
            breakinV.SetInt("locationY", breakin.location.y);
            breakinV.SetInt("locationZ", breakin.location.z);
            breakinV.SetString("playerUid", breakin.playerUid);
            breakinV.SetString("playerName", breakin.playerName);
            breakinV.SetLong("timestamp", breakin.timestamp.ToBinary());
            breakinV.SetBool("successful", breakin.successful);
            breakinCount++;

            breakinTree.SetLong("count", breakinCount);
        }

        public static Infraction GetInfraction(this ITreeAttribute tree, long index)
        {
            ITreeAttribute breakinTree = tree.GetOrAddTreeAttribute(InfractionsKey);
            ITreeAttribute breakinV = breakinTree.GetOrAddTreeAttribute(index.ToString("x16"));

            return new Infraction()
            {
                thingId = breakinV.GetLong("thingId", breakinTree.GetInt("blockId")),
                infraction = (EnumInfraction)breakinV.GetInt("infraction"),
                location = new int3()
                {
                    x = breakinV.GetInt("locationX"),
                    y = breakinV.GetInt("locationY"),
                    z = breakinV.GetInt("locationZ"),
                },
                playerUid = breakinV.GetString("playerUid", ""),
                playerName = breakinV.GetString("playerName", "Somebody"),
                timestamp = DateTime.FromBinary(breakinV.GetLong("timestamp")),
                successful = breakinV.GetBool("successful", true)
            };
        }

        public static Infraction[] GetInfractions(this ITreeAttribute tree)
        {
            var breakinTree = tree.GetOrAddTreeAttribute(InfractionsKey);
            long breakinCount = breakinTree.GetLong("count");

            Infraction[] breakins = new Infraction[breakinCount];
            for (long i = 0; i < breakinCount; i++)
            {
                breakins[i] = tree.GetInfraction(i);
            }
            return breakins;
        }
    }

    internal class BlockEntitySnitch : BlockEntity
    {
        public List<Infraction> Infractions = new List<Infraction>();
        public bool cooldown = true;
        public int limit = 512;

        public string OwnerUID { get; set; }
        public EnumSnitchMode Mode { get; set; }

        public IServerPlayer Owner { get => Api.World.PlayerByUid(OwnerUID) as IServerPlayer; }
        public bool Alarm { get; set; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (api.Side.IsServer())
            {
                RegisterGameTickListener(dt =>
                {
                    SimpleParticleProperties props = Pos.DownCopy().TemporalEffectAtPos(api);
                    props.MinPos.Add(0, 0.5, 0);
                    api.World.SpawnParticles(props);
                    List<IPlayer> intruders = new List<IPlayer>();

                    if (cooldown && api.World.GetPlayersAround(Pos.ToVec3d(), 13, 13).Any(e =>
                    {
                        if (e.PlayerUID == OwnerUID || OwnerUID == null || OwnerUID == "") return false;

                        if (!InSnitchableGroup(e as IServerPlayer)) return false;

                        intruders.Add(e);
                        return true;
                    }))
                    {
                        cooldown = false;
                        foreach (var val in intruders)
                        {
                            Infractions.Add(new Infraction(val.PlayerUID, val.PlayerName, EnumInfraction.Trespass, new int3(Pos.X, Pos.Y, Pos.Z), DateTime.UtcNow));
                        }

                        Random rnd = api.World.Rand;

                        BlockPos rndPos = new BlockPos()
                        {
                            X = rnd.Next(Pos.X - 13, Pos.X + 13),
                            Y = rnd.Next(Pos.Y - 13, Pos.Y + 13),
                            Z = rnd.Next(Pos.Z - 13, Pos.Z + 13)
                        };

                        if (Alarm) api.World.PlaySoundAt(new AssetLocation("sounds/creature/bell/alarm"), rndPos.X, rndPos.Y, rndPos.Z, null, false, 128, 1.0f);

                        RegisterDelayedCallback(dt2 => cooldown = true, 9000);
                    }
                }, 30);
            }
        }

        public void NotifyOfBreak(IServerPlayer byPlayer, int oldblockId, BlockPos pos, bool successful = true)
        {
            Api.World.FrameProfiler.Mark("snitch-notify-break-pre");

            if (!InSnitchableGroup(byPlayer)) return;

            Infractions.Add(new Infraction(byPlayer.PlayerUID, byPlayer.PlayerName, EnumInfraction.Break, new int3(pos.X, pos.Y, pos.Z), DateTime.UtcNow, oldblockId, successful));

            Api.World.FrameProfiler.Mark("snitch-notify-break-done");
        }

        public void NotifyOfUse(IServerPlayer byPlayer, int blockId, BlockPos pos, bool successful = true)
        {
            Api.World.FrameProfiler.Mark("snitch-notify-use-pre");

            if (!InSnitchableGroup(byPlayer)) return;

            Infractions.Add(new Infraction(byPlayer.PlayerUID, byPlayer.PlayerName, EnumInfraction.Use, new int3(pos.X, pos.Y, pos.Z), DateTime.UtcNow, blockId, successful));
            
            Api.World.FrameProfiler.Mark("snitch-notify-use-done");
        }

        public void NotifyOfPlace(IServerPlayer byPlayer, BlockPos pos, bool successful = true)
        {
            Api.World.FrameProfiler.Mark("snitch-notify-place-pre");

            if (!InSnitchableGroup(byPlayer)) return;

            int tryPlaceID = byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Block?.Id ?? 0;

            Infractions.Add(new Infraction(byPlayer.PlayerUID, byPlayer.PlayerName, EnumInfraction.Place, new int3(pos.X, pos.Y, pos.Z), DateTime.UtcNow, tryPlaceID, successful));

            Api.World.FrameProfiler.Mark("snitch-notify-place-done");
        }

        public void NotifyOfAttack(IServerPlayer byPlayer, long entityId, BlockPos pos, bool successful = true)
        {
            Api.World.FrameProfiler.Mark("snitch-notify-attack-pre");

            if (!InSnitchableGroup(byPlayer)) return;

            Infractions.Add(new Infraction(byPlayer.PlayerUID, byPlayer.PlayerName, EnumInfraction.Attack, new int3(pos.X, pos.Y, pos.Z), DateTime.UtcNow, entityId, successful));

            Api.World.FrameProfiler.Mark("snitch-notify-attack-done");
        }

        public void NotifyOfInteract(IServerPlayer byPlayer, long entityId, BlockPos pos, bool successful = true)
        {
            Api.World.FrameProfiler.Mark("snitch-notify-interact-pre");
            
            if (!InSnitchableGroup(byPlayer)) return;

            Infractions.Add(new Infraction(byPlayer.PlayerUID, byPlayer.PlayerName, EnumInfraction.Interact, new int3(pos.X, pos.Y, pos.Z), DateTime.UtcNow, entityId, successful));
            
            Api.World.FrameProfiler.Mark("snitch-notify-interact-done");
        }

        public bool InSnitchableGroup(IServerPlayer byPlayer)
        {
            if (byPlayer.PlayerUID == OwnerUID) return false;

            var ownerGroups = Owner?.GetGroups();
            var grps = ownerGroups?.Where((a) => a.GroupName != "Proximity").ToList();

            if (Mode == EnumSnitchMode.NonGroup && grps != null)
            {
                foreach (var group in byPlayer.GetGroups())
                {
                    foreach (var ownerGroup in grps)
                    {
                        if (group.GroupUid == ownerGroup.GroupUid) return false;
                    }
                }
            }

            return true;
        }

        public List<string> legacyBreakins = new List<string>();

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            OwnerUID = tree.GetString("owner");
            Mode = (EnumSnitchMode)tree.GetInt("mode", 0);
            Alarm = tree.GetBool("alarm", true);

            for (int i = 0; i < limit; i++)
            {
                string str = tree.GetString("breakins" + i);
                if (str != null) legacyBreakins.Add(str);
            }

            Infractions = tree.GetInfractions().ToList();

            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetString("owner", OwnerUID);
            tree.SetInt("mode", (int)Mode);
            tree.SetBool("alarm", Alarm);

            for (int i = 0; i < Infractions.Count; i++)
            {
                tree.AddInfraction(Infractions[i]);
            }
            base.ToTreeAttributes(tree);
        }
    }
}