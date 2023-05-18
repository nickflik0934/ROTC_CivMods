using System;
using System.Linq;
using System.Runtime.InteropServices;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace CivMods
{
    public struct Infraction
    {
        public string playerUid;
        public string playerName;
        public EnumInfraction infraction;
        public int3 location;
        public DateTime timestamp;
        public long thingId;
        public bool successful;

        public const string InfBreak = "{0}: {1} tried to break a block ({3}) at {2}.";
        public const string InfPlace = "{0}: {1} tried to place a block ({3}) at {2}.";
        public const string InfTrespass = "{0}: {1} is inside the radius of {2}.";
        public const string InfUse = "{0}: {1} tried to use a block ({3}) at {2}.";
        public const string InfHurt = "{0}: {1} tried to attack an entity ({3}) at {2}.";
        public const string InfInteract = "{0}: {1} tried to interact with an entity ({3}) at {2}.";

        public const string InfBreakS = "{0}: {1} broke a block ({3}) at {2}.";
        public const string InfPlaceS = "{0}: {1} placed a block ({3}) at {2}.";
        public const string InfUseS = "{0}: {1} used a block ({3}) at {2}.";
        public const string InfHurtS = "{0}: {1} attacked an entity ({3}) at {2}.";
        public const string InfInteractS = "{0}: {1} interacted with an entity ({3}) at {2}.";

        public static readonly string[] Infractions = new string[] { InfBreak, InfPlace, InfTrespass, InfUse, InfHurt, InfInteract };
        public static readonly string[] InfractionsS = new string[] { InfBreakS, InfPlaceS, InfTrespass, InfUseS, InfHurtS, InfInteractS };

        public Infraction(string playerUid, string playerName, EnumInfraction infraction, int3 location, DateTime timestamp, long thingId = 0, bool successful = true)
        {
            this.playerUid = playerUid;
            this.playerName = playerName;
            this.infraction = infraction;
            this.location = location;
            this.timestamp = timestamp;
            this.thingId = thingId;
            this.successful = successful;
        }

        public byte[] ToBytes()
        {
            int size = Marshal.SizeOf(this);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(this, ptr, true);

            byte[] bytes = new byte[size];
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);

            return bytes;
        }

        public void AddToTree(ITreeAttribute tree, string key)
        {
            tree.SetBytes(key, ToBytes());
        }

        public void FromBytes(byte[] bytes)
        {
            int size = bytes.Length;
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, ptr, size);
            
            Marshal.PtrToStructure(ptr, this);
            Marshal.FreeHGlobal(ptr);
        }

        public void GetFromTree(ITreeAttribute tree, string key)
        {
            FromBytes(tree.GetBytes(key));
        }

        public string GetInfString(IWorldAccessor world)
        {
            string pl = playerUid;
            string inf = successful ? InfractionsS[(int)infraction] : Infractions[(int)infraction];
            Block block = world.GetBlock((int)thingId);
            Entity entity = world.GetEntityById(thingId);
            string langThing = infraction == EnumInfraction.Attack || infraction == EnumInfraction.Interact ? entity?.GetName() : Lang.Get(block.Code.Clone().WithPathPrefix("block-").ToString());

            return string.Format(
                inf,
                timestamp,
                world.AllPlayers.FirstOrDefault((p) => p.PlayerUID == pl)?.PlayerName ?? playerName,
                location.RelativeToSpawn(world),
                langThing
            );
        }
    }
}