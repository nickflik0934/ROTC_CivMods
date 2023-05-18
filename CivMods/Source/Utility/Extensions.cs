﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace CivMods
{
    public static class Extensions
    {
        public static string Int2IP(this uint @int)
        {
            byte a = (byte)((@int & 0xFF000000) >> 24);
            byte b = (byte)((@int & 0x00FF0000) >> 16);
            byte c = (byte)((@int & 0x0000FF00) >> 08);
            byte d = (byte)((@int & 0x000000FF) >> 00);

            return string.Format("{0}.{1}.{2}.{3}", a, b, c, d);
        }

        public static byte[] Int2Bytes(this uint @int)
        {
            byte a = (byte)((@int & 0xFF000000) >> 24);
            byte b = (byte)((@int & 0x00FF0000) >> 16);
            byte c = (byte)((@int & 0x0000FF00) >> 08);
            byte d = (byte)((@int & 0x000000FF) >> 00);

            return new byte[] { a, b, c, d };
        }

        public static bool IP2Int(this string str, out uint ip)
        {
            if (str.Contains('[') && str.Contains(']') && str.Contains(':'))
            {
                str = str.Split('[', ']', ':')[4];
            }

            ip = 0u;
            bool valid = true;
            var splits = str.Split('.');

            if (splits.Length != 4)
            {
                return false;
            }

            valid &= uint.TryParse(splits[0], out uint a);
            valid &= uint.TryParse(splits[1], out uint b);
            valid &= uint.TryParse(splits[2], out uint c);
            valid &= uint.TryParse(splits[3], out uint d);

            if (valid)
            {
                ip |= a << 24;
                ip |= b << 16;
                ip |= c << 8;
                ip |= d << 0;
            }

            return valid;
        }
        public static T[] Replace<T>(this T[] arr, T rep)
        {
            var tmparr = arr.Where(a => a.GetHashCode() != rep.GetHashCode()).ToArray();
            tmparr = tmparr.Append(rep);
            return tmparr;
        }
        public static bool Replace<T>(this HashSet<T> set, T replaced)
        {
            bool existed = set.Remove(replaced);
            set.Add(replaced);
            return existed;
        }
        public static bool TryAccess(this Entity entity, BlockPos pos, EnumBlockAccessFlags flags)
        {
            return entity.World.Claims.TryAccess((entity as EntityPlayer)?.Player, pos, flags);
        }
        public static T NextR<T>(this T[] array, ref uint index)
        {
            index = (uint)(++index % array.Length);
            return array[index];
        }

        public static T NextR<T>(this List<T> array, ref uint index) => array.ToArray().NextR(ref index);

        public static T NextR<T>(this HashSet<T> array, ref uint index) => array.ToArray().NextR(ref index);

        public static T Prev<T>(this T[] array, ref uint index)
        {
            index = index > 0 ? index - 1 : (uint)(array.Length - 1);
            return array[index];
        }

        public static Block GetBlock(this BlockPos pos, IWorldAccessor world)
        { return world.BlockAccessor.GetBlock(pos); }

        public static Block GetBlock(this BlockPos pos, ICoreAPI api)
        { return pos.GetBlock(api.World); }

        public static BlockEntity GetBlockEntity(this BlockPos pos, IWorldAccessor world) => world.BlockAccessor.GetBlockEntity(pos);

        public static Block GetBlock(this AssetLocation asset, ICoreAPI api)
        {
            if (api.World.BlockAccessor.GetBlock(asset) != null)
            {
                return api.World.BlockAccessor.GetBlock(asset);
            }
            return null;
        }

        public static Item GetItem(this AssetLocation asset, ICoreAPI api)
        {
            if (api.World.GetItem(asset) != null)
            {
                return api.World.GetItem(asset);
            }
            return null;
        }

        public static AssetLocation ToAsset(this string asset)
        { return new AssetLocation(asset); }

        public static Block ToBlock(this string block, ICoreAPI api) => block.WithDomain().ToAsset().GetBlock(api);

        public static Block Block(this BlockSelection sel, ICoreAPI api) => api.World.BlockAccessor.GetBlock(sel.Position);

        public static BlockEntity BlockEntity(this BlockSelection sel, ICoreAPI api) => api.World.BlockAccessor.GetBlockEntity(sel.Position);

        public static AssetLocation[] ToAssets(this string[] strings)
        {
            List<AssetLocation> assets = new List<AssetLocation>();
            foreach (var val in strings)
            {
                assets.Add(val.ToAsset());
            }
            return assets.ToArray();
        }

        public static AssetLocation[] ToAssets(this List<string> strings) => strings.ToArray().ToAssets();

        public static void PlaySoundAtWithDelay(this IWorldAccessor world, AssetLocation location, BlockPos pos, int delay)
        {
            world.RegisterCallback(dt => world.PlaySoundAt(location, pos.X, pos.Y, pos.Z), delay);
        }

        public static T[] Stretch<T>(this T[] array, int amount)
        {
            if (amount < 1) return array;
            T[] parray = array;

            Array.Resize(ref array, array.Length + amount);
            for (int i = 0; i < array.Length; i++)
            {
                double scalar = (double)i / array.Length;
                array[i] = parray[(int)(scalar * parray.Length)];
            }
            return array;
        }

        public static int IndexOfMin(this IList<int> self)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }

            if (self.Count == 0)
            {
                throw new ArgumentException("List is empty.", "self");
            }

            int min = self[0];
            int minIndex = 0;

            for (int i = 1; i < self.Count; ++i)
            {
                if (self[i] < min)
                {
                    min = self[i];
                    minIndex = i;
                }
            }
            return minIndex;
        }

        public static int IndexOfMin(this int[] self) => IndexOfMin(self.ToList());

        public static bool IsSurvival(this EnumGameMode gamemode) => gamemode == EnumGameMode.Survival;

        public static bool IsCreative(this EnumGameMode gamemode) => gamemode == EnumGameMode.Creative;

        public static bool IsSpectator(this EnumGameMode gamemode) => gamemode == EnumGameMode.Spectator;

        public static bool IsGuest(this EnumGameMode gamemode) => gamemode == EnumGameMode.Guest;

        public static void PlaySoundAt(this IWorldAccessor world, AssetLocation loc, BlockPos pos) => world.PlaySoundAt(loc, pos.X, pos.Y, pos.Z);

        public static int GetID(this AssetLocation loc, ICoreAPI api) => loc.GetBlock(api).BlockId;

        public static string WithDomain(this string a) => a.IndexOf(":") == -1 ? "game:" + a : a;

        public static string[] WithDomain(this string[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = a[i].WithDomain();
            }
            return a;
        }

        public static Vec3d MidPoint(this BlockPos pos) => pos.ToVec3d().AddCopy(0.5, 0.5, 0.5);

        public static string Apd(this string a, string appended)
        {
            return a + "-" + appended;
        }

        public static string Apd(this string a, int appended)
        {
            return a + "-" + appended;
        }

        public static void SpawnItemEntity(this IWorldAccessor world, ItemStack[] stacks, Vec3d pos, Vec3d velocity = null)
        {
            foreach (ItemStack stack in stacks)
            {
                world.SpawnItemEntity(stack, pos, velocity);
            }
        }

        public static void SpawnItemEntity(this IWorldAccessor world, JsonItemStack[] stacks, Vec3d pos, Vec3d velocity = null)
        {
            foreach (JsonItemStack stack in stacks)
            {
                string err = "";
                stack.Resolve(world, err);
                if (stack.ResolvedItemstack != null) world.SpawnItemEntity(stack.ResolvedItemstack, pos, velocity);
            }
        }

        public static BlockEntity BlockEntity(this BlockPos pos, IWorldAccessor world)
        {
            return world.BlockAccessor.GetBlockEntity(pos);
        }

        public static BlockEntity BlockEntity(this BlockPos pos, ICoreAPI api) => pos.BlockEntity(api.World);

        public static BlockEntity BlockEntity(this BlockSelection sel, IWorldAccessor world)
        {
            return sel.Position.BlockEntity(world);
        }

        public static void InitializeAnimators(this BlockEntityAnimationUtil util, Vec3f rot, params string[] CacheDictKeys)
        {
            foreach (var val in CacheDictKeys)
            {
                util.InitializeAnimator(val, rot);
            }
        }

        public static void InitializeAnimators(this BlockEntityAnimationUtil util, Vec3f rot, List<string> CacheDictKeys)
        {
            InitializeAnimators(util, rot, CacheDictKeys.ToArray());
        }

        public static void SetUv(this MeshData mesh, TextureAtlasPosition texPos) => mesh.SetUv(new float[] { texPos.x1, texPos.y1, texPos.x2, texPos.y1, texPos.x2, texPos.y2, texPos.x1, texPos.y2 });

        public static bool TryGiveItemstack(this IPlayerInventoryManager manager, ItemStack[] stacks)
        {
            foreach (var val in stacks)
            {
                if (manager.TryGiveItemstack(val)) continue;
                return false;
            }
            return true;
        }

        public static bool TryGiveItemstack(this IPlayerInventoryManager manager, JsonItemStack[] stacks)
        {
            return manager.TryGiveItemstack(stacks.ResolvedStacks(manager.ActiveHotbarSlot.Inventory.Api.World));
        }

        public static double DistanceTo(this SyncedEntityPos pos, Vec3d vec)
        {
            return Math.Sqrt(pos.SquareDistanceTo(vec));
        }

        public static bool WildCardMatch(this RegistryObject obj, string a) => obj.WildCardMatch(new AssetLocation(a));

        public static ItemStack[] ResolvedStacks(this JsonItemStack[] stacks, IWorldAccessor world)
        {
            List<ItemStack> stacks1 = new List<ItemStack>();
            foreach (JsonItemStack stack in stacks)
            {
                stack.Resolve(world, null);
                stacks1.Add(stack.ResolvedItemstack);
            }
            return stacks1.ToArray();
        }

        public static object GetInstanceField<T>(this T instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = instance.GetType().GetField(fieldName, bindFlags);
            return field?.GetValue(instance);
        }

        public static List<BlockEntity> GetBlockEntities(this IWorldAccessor world, BlockPos pos, Vec3i range)
        {
            List<BlockEntity> blockEntities = new List<BlockEntity>();
            for (int x = pos.X - range.X; x < pos.X + range.X; x++)
            {
                for (int y = pos.Y - range.Y; y < pos.Y + range.Y; y++)
                {
                    for (int z = pos.Z - range.Z; z < pos.Z + range.Z; z++)
                    {
                        BlockPos p = new BlockPos(x, y, z);
                        BlockEntity be = p.BlockEntity(world);
                        if (be != null)
                            blockEntities.Add(be);
                    }
                }
            }
            return blockEntities;
        }

        public static List<BlockEntity> GetBlockEntitiesAround(this IWorldAccessor world, BlockPos pos, Vec2i range) => GetBlockEntities(world, pos, new Vec3i(range.X, range.Y, range.X));

        public static BlockPos RelativeToSpawn(this BlockPos pos, IWorldAccessor world)
        {
            return pos.SubCopy(world.DefaultSpawnPosition.AsBlockPos);
        }

        public static int3 RelativeToSpawn(this int3 pos, IWorldAccessor world)
        {
            var defSpawnPos = world.DefaultSpawnPosition.AsBlockPos;

            return new int3(pos.x - defSpawnPos.X, pos.y - defSpawnPos.Y, pos.z - defSpawnPos.Z);
        }

        public static SimpleParticleProperties TemporalEffect
        {
            get
            {
                var p = new SimpleParticleProperties(
                    0.5f, 1,
                    ColorUtil.ToRgba(150, 34, 47, 44),
                    new Vec3d(),
                    new Vec3d(),
                    new Vec3f(-0.1f, -0.1f, -0.1f),
                    new Vec3f(0.1f, 0.1f, 0.1f),
                    1.5f,
                    0,
                    0.5f,
                    0.75f,
                    EnumParticleModel.Quad
                );

                p.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f);
                p.AddPos.Set(1, 2, 1);
                p.addLifeLength = 0.5f;
                p.RedEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 80);
                return p;
            }
        }

        private static readonly NatFloat rndPos = NatFloat.create(EnumDistribution.INVERSEGAUSSIAN, 0, 0.5f);

        public static SimpleParticleProperties TemporalEffectAtPos(this BlockPos pos, ICoreAPI api)
        {
            SimpleParticleProperties p = TemporalEffect;
            Vec3d posvec = pos.DownCopy().MidPoint();
            int r = 53;
            int g = 221;
            int b = 172;
            p.Color = (r << 16) | (g << 8) | (b << 0) | (50 << 24);

            p.AddPos.Set(0, 0, 0);
            p.BlueEvolve = null;
            p.RedEvolve = null;
            p.GreenEvolve = null;
            p.MinSize = 0.1f;
            p.MaxSize = 0.2f;
            p.SizeEvolve = null;
            p.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 100f);

            double xpos = rndPos.nextFloat();
            double ypos = 1.9 + api.World.Rand.NextDouble() * 0.2;
            double zpos = rndPos.nextFloat();

            p.LifeLength = GameMath.Sqrt(xpos * xpos + zpos * zpos) / 10;
            p.MinPos.Set(posvec.X + xpos, posvec.Y + ypos, posvec.Z + zpos);
            p.MinVelocity.Set(-(float)xpos, -1 - (float)api.World.Rand.NextDouble() / 2, -(float)zpos);
            p.MinQuantity = 0.25f;
            p.AddQuantity = 0;

            return p;
        }

        public static T GetBlockEntity<T>(this IBlockAccessor bA, BlockPos pos) where T : BlockEntity
        {
            return bA?.GetBlockEntity(pos) as T;
        }

        public static T Item<T>(this ItemStack itemStack) where T : Item
        {
            return itemStack?.Item as T;
        }

        public static PlayerSpawnPos GetTempSpawn(this IServerPlayer player)
        {
            var atr = player.Entity.WatchedAttributes;

            TreeAttribute civspawn;

            if (atr.HasAttribute("civmods-spawn"))
            {
                civspawn = (TreeAttribute)atr.GetAttribute("civmods-spawn");
            }
            else
            {
                return null;
            }

            int x = civspawn.GetInt("tempX");
            int y = civspawn.GetInt("tempY");
            int z = civspawn.GetInt("tempZ");

            return new PlayerSpawnPos(x, y, z);
        }

        public static PlayerSpawnPos GetOrigSpawn(this IServerPlayer player)
        {
            var atr = player.Entity.WatchedAttributes;

            TreeAttribute civspawn;

            if (atr.HasAttribute("civmods-spawn"))
            {
                civspawn = (TreeAttribute)atr.GetAttribute("civmods-spawn");
            }
            else
            {
                return null;
            }

            int x = civspawn.GetInt("origX");
            int y = civspawn.GetInt("origY");
            int z = civspawn.GetInt("origZ");

            return new PlayerSpawnPos(x, y, z);
        }

        public static void SetTempSpawn(this IServerPlayer player, int x, int y, int z)
        {
            player.SetTempSpawn(new BlockPos(x, y, z));
        }

        public static void SetTempSpawn(this IServerPlayer player, BlockPos pos)
        {
            var org = player.GetSpawnPosition(false).XYZInt;
            var atr = player.Entity.WatchedAttributes;

            TreeAttribute civspawn;

            if (atr.HasAttribute("civmods-spawn"))
            {
                civspawn = (TreeAttribute)atr.GetAttribute("civmods-spawn");
            }
            else civspawn = new TreeAttribute();

            civspawn.SetInt("tempX", pos.X);
            civspawn.SetInt("tempY", pos.Y);
            civspawn.SetInt("tempZ", pos.Z);

            if (!civspawn.HasAttribute("origX"))
            {
                civspawn.SetInt("origX", org.X);
                civspawn.SetInt("origY", org.Y);
                civspawn.SetInt("origZ", org.Z);
            }

            atr.SetAttribute("civmods-spawn", civspawn);
            atr.MarkPathDirty("civmods-spawn");

            player.SetSpawnPosition(player.GetTempSpawn());
        }

        public static void ResetSpawn(this IServerPlayer player)
        {
            player.SetSpawnPosition(player.GetOrigSpawn());
        }

        public static void SetDefaultSpawn(this IServerPlayer player)
        {
            var dft = player.Entity.World.DefaultSpawnPosition;
            var defaultS = new PlayerSpawnPos()
            {
                x = (int)dft.X,
                y = (int)dft.Y,
                z = (int)dft.Z,
                pitch = dft.Pitch,
                yaw = dft.Yaw
            };

            player.SetSpawnPosition(defaultS);
        }

        public static void MigrateOldSpawn(this IServerPlayer player)
        {
            var atr = player.Entity.WatchedAttributes;

            if (atr.HasAttribute("bedX"))
            {
                int x = atr.GetInt("bedX");
                int y = atr.GetInt("bedY");
                int z = atr.GetInt("bedZ");

                player.SetDefaultSpawn();

                player.SetTempSpawn(x, y, z);

                atr.RemoveAttribute("bedX");
                atr.RemoveAttribute("bedY");
                atr.RemoveAttribute("bedZ");
            }
        }
    }
}