using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace CivMods
{
    internal class EntityBehaviorSuffocate : EntityBehavior
    {
        public float? CurrentAir
        {
            get => entity.WatchedAttributes.TryGetFloat("currentAir");
            set
            {
                entity.WatchedAttributes.SetFloat("currentAir", value ?? 0.0f);
                entity.WatchedAttributes.MarkPathDirty("currentAir");
            }
        }

        public const float maxAir = 1.0f;

        private ICoreServerAPI sapi { get => entity.Api as ICoreServerAPI; }
        private long id;

        public CivModsServerConfig serverConfig { get => CivModSystem.serverConfig; }

        public EntityBehaviorSuffocate(Entity entity) : base(entity)
        {
        }

        public override string PropertyName() => "suffocate";

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            id = sapi?.World.RegisterGameTickListener(SuffocationWatch, 500) ?? 0;
        }

        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            base.OnEntityDespawn(despawn);
            sapi?.World.UnregisterGameTickListener(id);
        }

        public void SuffocationWatch(float dt)
        {
            if (entity is EntityItem || entity == null || entity.ServerPos == null || !entity.Alive) return;
            float change = dt / 0.5f;

            CurrentAir = CurrentAir == null ? 1.0f : CurrentAir;

            float height = entity.CollisionBox?.Height ?? 0;

            if (entity is EntityPlayer)
            {
                EntityPlayer entityPlayer = entity as EntityPlayer;
                if (entityPlayer.Player.WorldData.CurrentGameMode != EnumGameMode.Survival) return;
            }

            Vec3d modPos = entity.Pos.XYZ;

            if (InBlockBounds(modPos, height, out float suff))
            {
                suff *= change;
                if (CurrentAir > 0) CurrentAir -= suff;
                else
                {
                    CurrentAir = 0.0f;
                    DamageSource source = new DamageSource();
                    source.Source = EnumDamageSource.Drown;
                    entity.ReceiveDamage(source, serverConfig.SuffocateRateHeavy * change);
                }
            }
            else if (CurrentAir < 1.0) CurrentAir += serverConfig.BreathRate * change;

            CurrentAir = CurrentAir > 1.0 ? 1.0f : CurrentAir;
        }

        public bool InBlockBounds(Vec3d vec, float height, out float suffocation)
        {
            suffocation = 0.0f;

            vec.Add(0, height * 0.9, 0);

            Vec3d blockCenter = vec.AsBlockPos.ToVec3d().AddCopy(0.5, 0.5, 0.5);
            Block block = sapi.World.BlockAccessor.GetBlock(vec.AsBlockPos);
            BlockEntity be = sapi.World.BlockAccessor.GetBlockEntity(vec.AsBlockPos);

            double distance = Math.Sqrt(vec.SquareDistanceTo(blockCenter));
#if DEBUG
            sapi.World.SpawnParticles(1, 0xFF0000, vec, vec, Vec3f.Zero, Vec3f.Zero, 10.0f, 0.0f, 1.0f, EnumParticleModel.Cube);
#endif
            if (block.Attributes != null && block.Attributes.KeyExists("breathable") && block.Attributes["breathable"].AsBool(false))
            {
                return false;
            }

            if (block.IsLiquid() && int.TryParse(block.Variant["height"] ?? "7", out int waterHeight) && distance < 1.0 && waterHeight > 4)
            {
                suffocation = serverConfig.SuffocateRateLight;
                return true;
            }

            if (be is Vintagestory.GameContent.BlockEntityChisel)
            {
                var bec = be as Vintagestory.GameContent.BlockEntityChisel;
                var col = bec.GetField<Cuboidf[]>("selectionBoxes");

                if (IsInside(vec.AsBlockPos, vec, col))
                {
                    suffocation = serverConfig.SuffocateRateHeavy;
                    return true;
                }
            }
            else if (IsInside(vec.AsBlockPos, vec, block.CollisionBoxes))
            {
                suffocation = serverConfig.SuffocateRateHeavy;
                return true;
            }

            return false;
        }

        public bool IsInside(BlockPos origin, Vec3d position, Cuboidf[] boxes) => IsInside(origin, position.ToVec3f(), boxes);

        public bool IsInside(BlockPos origin, Vec3f position, Cuboidf[] boxes) => IsInside(origin.ToVec3f(), position, boxes);

        public bool IsInside(Vec3f origin, Vec3f position, Cuboidf[] boxes)
        {
            if (boxes == null) return false;

            for (int i = 0; i < boxes.Length; i++)
            {
                var box = boxes[i];
                var col = box.Clone();

                col.X1 += origin.X;
                col.X2 += origin.X;
                col.Y1 += origin.Y;
                col.Y2 += origin.Y;
                col.Z1 += origin.Z;
                col.Z2 += origin.Z;

                bool inside = col.Contains(position.X, position.Y, position.Z);

                if (inside) return true;
            }
            return false;
        }
    }

    public static class MiscUtilities
    {
        public static double Area(this Cuboidf cuboid)
        {
            return (cuboid.Length * cuboid.Width * cuboid.Height);
        }

        public static BlockPos AddCopy(this BlockPos pos, BlockPos copy)
        {
            return pos.AddCopy(copy.X, copy.Y, copy.Z);
        }
    }
}