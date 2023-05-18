using HarmonyLib;
using Microsoft.SqlServer.Server;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace CivMods
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    internal struct LwModinfo
    {
        public string Name;
        public string Version;
        public byte Side;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    internal class ModInfoPacket
    {
        public List<LwModinfo> ModInfos = new List<LwModinfo>();

        internal void Add(ModInfo info)
        {
            ModInfos.Add(new LwModinfo() {  Name = info.Name, Version = info.Version, Side = (byte)info.Side });
        }
    }

    internal class _76ceff8969968ef1cdcb28310804279f : ModSystem
    {
        public override double ExecuteOrder() => double.MaxValue;

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Network.RegisterChannel("76ceff8969968ef1cdcb28310804279f")
                .RegisterMessageType(typeof(ModInfoPacket))
                .SetMessageHandler((IServerPlayer byPlayer, ModInfoPacket packet) =>
            {
                StringBuilder blder = new StringBuilder().AppendLine();

                foreach (var mod in packet.ModInfos)
                {
                    if ((EnumAppSide)mod.Side == EnumAppSide.Client)
                    {
                        blder.Append(string.Format("{0}:v{1}", mod.Name, mod.Version));
                        blder.AppendLine();
                    }
                }

                api.World.Logger.Notification(string.Format("Player {0} self reported these client mods: {1}", byPlayer.PlayerName, blder.ToString()));
            });
            base.StartServerSide(api);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            var cNet = api.Network.RegisterChannel("76ceff8969968ef1cdcb28310804279f")
                .RegisterMessageType(typeof(ModInfoPacket));

            ModInfoPacket packet = new ModInfoPacket();

            foreach (var mod in api.ModLoader.Mods)
            {
                packet.Add(mod.Info);
            }

            api.Event.LevelFinalize += () =>
            {
                DateTime startTime = DateTime.Now;

                while (!cNet.Connected)
                {
                    DateTime nowTime = DateTime.Now;
                    TimeSpan elapsed = nowTime.Subtract(startTime);
                    if (elapsed.TotalSeconds > 5)
                    {
                        return;
                    }
                }

                cNet.SendPacket(packet);
            };
        }
    }

    internal class CivModSystem : ModSystem
    {
        public ModSystemBlockReinforcement Reinforcement { get => api.ModLoader.GetModSystem<ModSystemBlockReinforcement>(); }
        private ICoreAPI api;
        private ICoreServerAPI sapi;
        private ICoreClientAPI capi;

        private IPBan[] IPBans { get => serverConfig.IPBans; set => serverConfig.IPBans = value; }
        private OTTUse[] OTTUsed { get => serverConfig.OTTUsed; set => serverConfig.OTTUsed = value; }

        public static CivModsServerConfig serverConfig;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            sapi = api as ICoreServerAPI;
            capi = api as ICoreClientAPI;

            api.RegisterBlockClass("BlockSnitch", typeof(BlockSnitch));
            api.RegisterBlockClass("BlockStackableUnderwater", typeof(BlockStackableUnderwater));
            api.RegisterBlockClass("BlockAcid", typeof(BlockAcid));
            api.RegisterBlockEntityClass("Acid", typeof(BlockEntityAcid));
            api.RegisterBlockEntityClass("Snitch", typeof(BlockEntitySnitch));
            api.RegisterItemClass("ItemBlueprint", typeof(ItemBlueprint));
            api.RegisterItemClass("ItemHedgeClippers", typeof(ItemHedgeClippers));
            api.RegisterBlockBehaviorClass("UnbreakableByTier", typeof(BlockBehaviorUnbreakableByTier));
            //api.RegisterEntityBehaviorClass("suffocation", typeof(EntityBehaviorSuffocate));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.LevelFinalize += () => RemovePlayerMapLayers();

            SnitchInitClient(api);
            //AirBarInitClient(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            ServerConfigInit(api);
            InitHandlersServer(api);
            InitDebugCommandsServer(api);
            InitSnitchCommandsServer(api);
            InitCmdIPBan(api);
            InitCmdOTT(api);
        }

        public void ServerConfigInit(ICoreServerAPI api)
        {
            serverConfig = new CivModsServerConfig();
            serverConfig.Initialize(api);
            serverConfig.Save();
        }

        public void InitHandlersServer(ICoreServerAPI api)
        {
            api.Event.PlayerJoin += HandleIPBans;
            api.Event.SaveGameLoaded += () => RemovePlayerMapLayers();
            api.Event.DidPlaceBlock += PlaceBlockEvent;
            api.Event.DidBreakBlock += BreakBlockEvent;
            api.Event.DidUseBlock += UseBlockEvent;
            api.Event.OnPlayerInteractEntity += PlayerInteractEntityEvent;

            OnBlockRemoved += BlockRemovedHandler;
            api.Event.PlayerCreate += TellEveryoneTheyAreNew;

            api.Event.OnEntitySpawn += (e) =>
            {
                if (e.HasBehavior<EntityBehaviorNameTag>())
                {
                    e.GetBehavior<EntityBehaviorNameTag>().ShowOnlyWhenTargeted = true;
                }
            };
        }


        struct ottRequest
        {
            public IServerPlayer requester;
            public IServerPlayer recipient;
            public DateTime requestTime;

            public ottRequest(IServerPlayer requester, IServerPlayer recipient)
            {
                this.requester = requester;
                this.recipient = recipient;
                this.requestTime = DateTime.UtcNow;
            }
        }

        List<ottRequest> requests = new List<ottRequest>();

        internal void acceptOrDenyTeleport(ICoreServerAPI api, IServerPlayer recipient, string playerName, bool denied = false)
        {
            int timeLimit = serverConfig.OTTTimeout;

            foreach (ottRequest request in requests)
            {
                if(request.recipient != recipient)
                {
                    continue;
                }

                if(request.requestTime > DateTime.UtcNow.AddSeconds(timeLimit))
                {
                    requests.Remove(request);
                    continue;
                }

                if(request.requester.PlayerName != playerName)
                {
                    continue;
                }

                if (denied)
                {
                    requests.Remove(request);
                    continue;
                }

                requests.Remove(request);
                recipient.SendMessage(0, "Teleport accepted. Teleporting " + request.requester.PlayerName + " to you.", EnumChatType.CommandSuccess);

                request.requester.Entity.TeleportTo(request.recipient.Entity.Pos.AsBlockPos);

                // Basically List<T>.Add for arrays
                OTTUsed = OTTUsed.Replace(new OTTUse(request.requester.PlayerUID));
                serverConfig.Save();
                return;
            }

            recipient.SendMessage(0, "No request found or ran out of time.", EnumChatType.CommandError);
        }

        internal void requestTeleport(ICoreServerAPI api, IServerPlayer requester, string requestName)
        {
            foreach (OTTUse ottUse in OTTUsed)
            {
                if(ottUse.playerUID == requester.PlayerUID)
                {
                    requester.SendMessage(0, "You have already used your one time teleport!", EnumChatType.Notification);
                    return;
                }
            }

            if (requestName == null || requestName == "")
            {
                requester.SendMessage(0, "Unknown player name given. Try /ott [name]", EnumChatType.Notification);
                return;
            }

            IServerPlayer recipient = null;
            foreach (IServerPlayer otherPlayer in api.Server.Players)
            {
                if (otherPlayer.PlayerName == requestName)
                {
                    recipient = otherPlayer;
                    break;
                }
            }

            if (recipient == null)
            {
                requester.SendMessage(0, "Player " + requestName + " not found.", EnumChatType.Notification);
                return;
            }

            foreach (ottRequest request in requests)
            {
                if(request.requester.PlayerName != requester.PlayerName)
                {
                    continue;
                }

                if (DateTime.UtcNow > request.requestTime.AddSeconds(serverConfig.OTTTimeout))
                {
                    requests.Remove(request);
                }
                else
                {
                    requester.SendMessage(0, "Already requested a ott. Please wait " + (DateTime.UtcNow.AddSeconds(serverConfig.OTTTimeout) - request.requestTime).Seconds + " seconds.", EnumChatType.Notification);
                }
            }

            requests.Insert(0, new ottRequest(requester, recipient));

            requester.SendMessage(0, "Player " + requestName + " found, asking the player if they accept...", EnumChatType.Notification);
            recipient.SendMessage(0, "Player " + requester.PlayerName + " is requesting to teleport to you, do you accept? Use '/ott accept' or '/ott deny'", EnumChatType.Notification);
        }

        internal void InitCmdOTT(ICoreServerAPI api)
        {
            api.RegisterCommand("ott", "Request-teleport to a player.", "[name] or [accept/deny] [playername]", (player, id, args) =>
            {
                if(args.Length == 0)
                {
                    player.SendMessage(0, "Invalid command, try /ott [name] or /ott [accept/deny] [playername]", EnumChatType.Notification);
                    return;
                }
                string argument = args.PopWord();

                if(argument.ToLower() == "accept" || argument.ToLower() == "deny")
                {
                    string playerName = args.PopWord();
                    acceptOrDenyTeleport(api, player, playerName, argument.ToLower() == "deny");
                    return;
                }


                requestTeleport(api, player, argument);
            }, Privilege.root);


            //    switch (cmd0)
            //    {
            //        case "remove":
            //            string ipf = args.PopAll();
            //            var tmpban = new IPBan(ipf, "", DateTime.Now);

            //            if (tmpban.Valid)
            //            {
            //                IPBans = IPBans.Replace(tmpban);
            //            }
            //            else
            //            {
            //                player.SendMessage(0, "Invalid IP Format.", EnumChatType.Notification);
            //            }

            //            HandleIPBans(null);
            //            break;
            //        case "list":
            //            foreach (var ban in IPBans)
            //            {
            //                player.SendMessage(0, ban.GetString(), EnumChatType.Notification);
            //            }
            //            break;
            //        default:
            //            double days = args.PopDouble(50.0).Value;
            //            byte range = (byte)args.PopInt(4).Value;
            //            string reason = args.PopAll();

            //            if (cmd0 != null)
            //            {
            //                var toBan = api.World.AllPlayers.Where(p => p.PlayerName == cmd0);
            //                foreach (var banning in toBan)
            //                {
            //                    IServerPlayer serverPlayer = banning as IServerPlayer;

            //                    string ip = serverPlayer.IpAddress.Contains('[') ? serverPlayer.IpAddress.Split('[', ']', ':')[4] : serverPlayer.IpAddress;
            //                    IPBans = IPBans.Replace(new IPBan(ip, reason, DateTime.Now.AddDays(days), range));

            //                    //for players on same IP
            //                    foreach (var player_ in api.World.AllPlayers)
            //                    {
            //                        HandleIPBans(player_ as IServerPlayer);
            //                    }
            //                }
            //            }
            //            serverConfig.Save();
            //            break;
            //    }

            //}, "controlserver");
        }

        internal void InitCmdIPBan(ICoreServerAPI api)
        {
            api.RegisterCommand("ipban", "Bans a player by IP from joining the server.", "name|remove|list, days|ip, range (1-4), reason", (player, id, args) =>
            {
                string cmd0 = args.PopWord();
                switch (cmd0)
                {
                    case "remove":
                        string ipf = args.PopAll();
                        var tmpban = new IPBan(ipf, "", DateTime.Now);
                        
                        if (tmpban.Valid)
                        {
                            IPBans = IPBans.Replace(tmpban);
                        }
                        else
                        {
                            player.SendMessage(0, "Invalid IP Format.", EnumChatType.Notification);
                        }
                        
                        HandleIPBans(null);
                        break;
                    case "list":
                        foreach (var ban in IPBans)
                        {
                            player.SendMessage(0, ban.GetString(), EnumChatType.Notification);
                        }
                        break;
                    default:
                        double days = args.PopDouble(50.0).Value;
                        byte range = (byte)args.PopInt(4).Value;
                        string reason = args.PopAll();

                        if (cmd0 != null)
                        {
                            var toBan = api.World.AllPlayers.Where(p => p.PlayerName == cmd0);
                            foreach (var banning in toBan)
                            {
                                IServerPlayer serverPlayer = banning as IServerPlayer;

                                string ip = serverPlayer.IpAddress.Contains('[') ? serverPlayer.IpAddress.Split('[', ']', ':')[4] : serverPlayer.IpAddress;
                                IPBans = IPBans.Replace(new IPBan(ip, reason, DateTime.Now.AddDays(days), range));

                                //for players on same IP
                                foreach (var player_ in api.World.AllPlayers)
                                {
                                    HandleIPBans(player_ as IServerPlayer);
                                }
                            }
                        }
                        serverConfig.Save();
                        break;
                }

            }, "controlserver");
        }

        public void HandleIPBans(IServerPlayer player)
        {
            HashSet<IPBan> expiredBans = new HashSet<IPBan>();
            
            uint ip = 0;

            player?.IpAddress?.IP2Int(out ip);

            foreach (var ban in IPBans)
            {
                if (ban.UntilDate < DateTime.Now) 
                { 
                    expiredBans.Add(ban);
                    continue;
                }

                if (ban.IsIpBanned(ip))
                {
                    player?.Disconnect(ban.Reason);
                    break;
                }
            }

            foreach (var ban in expiredBans)
            {
                IPBans = IPBans.Remove(ban);
            }
        }

        public void InitDebugCommandsServer(ICoreServerAPI api)
        {
            api.RegisterCommand("blocktickcount", "Get count of main thread block sim ticks currently queued up.", "", (byPlayer, id, args) =>
            {
                var blockSim = api.World.GetField<ServerSystem[]>("Systems").OfType<ServerSystemBlockSimulation>().Single();
                var queuedTicks = blockSim.GetField<ConcurrentQueue<object>>("queuedTicks");
                int count = queuedTicks.Count;
                api.SendMessage(byPlayer, 0, string.Format("{0}", count), EnumChatType.OwnMessage);
            }, "controlserver");
        }

        public void InitSnitchCommandsServer(ICoreServerAPI api)
        {
            api.RegisterCommand("snitchinfo", "Get snitch info", "", GetSnitchInfo, Privilege.root);

            api.RegisterCommand("snitchroot", "Create and remove snitch block entities", "", (byPlayer, id, args) =>
            {
                BlockPos pos = byPlayer.CurrentBlockSelection?.Position;
                if (api.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.Use))
                {
                    string arg = args.PopWord();
                    switch (arg)
                    {
                        case "create":
                            if (pos.BlockEntity(api) == null)
                            {
                                if (!api.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11)).Any(be => be is BlockEntitySnitch))
                                {
                                    api.World.BlockAccessor.SpawnBlockEntity("Snitch", pos);
                                    ((BlockEntitySnitch)pos.BlockEntity(byPlayer.Entity.World)).OwnerUID = byPlayer.PlayerUID;
                                }
                                else
                                {
                                    api.SendMessage(byPlayer, 0, "Already exists a snitch within 11 blocks!", EnumChatType.OwnMessage);
                                }
                            }
                            else
                            {
                                api.SendMessage(byPlayer, 0, "Cannot create a snitch where there already exists a Block Entity!", EnumChatType.OwnMessage);
                            }
                            break;

                        case "remove":
                            if (pos.BlockEntity(api.World) is BlockEntitySnitch)
                            {
                                api.World.BlockAccessor.RemoveBlockEntity(pos);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }, Privilege.root);
        }

        public void RemovePlayerMapLayers()
        {
            var mapLayers = api.ModLoader.GetModSystem<WorldMapManager>().MapLayers;

            foreach (var val in new List<MapLayer>(mapLayers.OfType<PlayerMapLayer>()))
            {
                mapLayers.Remove(val);
            }
        }

        public bool TryAccessSnitch(BlockEntitySnitch snitch, IServerPlayer player)
        {
            if (snitch == null || player == null) return false;

            if (!Reinforcement.GetReinforcment(snitch.Pos)?.Locked ?? false && snitch.OwnerUID == player.PlayerUID) return true;
            else if (!Reinforcement.IsLockedForInteract(snitch.Pos, player)) return true;
            return false;
        }

        public delegate void BlockRemovedDelegate(IWorldAccessor world, BlockPos pos, Block oldBlock);

        public event BlockRemovedDelegate OnBlockRemoved;

        //why
        public void InvokeOnBlockRemoved(IWorldAccessor world, BlockPos pos, Block oldBlock)
        {
            OnBlockRemoved.Invoke(world, pos, oldBlock);
        }

        private void PlayerInteractEntityEvent(Entity entity, IPlayer byPlayer, ItemSlot slot, Vec3d hitPosition, int mode, ref EnumHandling handling)
        {
            EnumInteractMode interactMode = (EnumInteractMode)mode;
            BlockPos pos = entity.Pos.AsBlockPos;

            List<BlockEntity> list = byPlayer.Entity.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11));

            switch (interactMode)
            {
                case EnumInteractMode.Attack:
                    foreach (var e in list)
                    {
                        (e as BlockEntitySnitch)?.NotifyOfAttack((IServerPlayer)byPlayer, entity.EntityId, pos);
                    }
                    break;

                case EnumInteractMode.Interact:
                    foreach (var e in list)
                    {
                        (e as BlockEntitySnitch)?.NotifyOfInteract((IServerPlayer)byPlayer, entity.EntityId, pos);
                    }
                    break;

                default:
                    break;
            }
        }

        internal void GetSnitchInfo(IServerPlayer byPlayer, int id, CmdArgs args)
        {
            bool legacy = args.PopBool() ?? false;
            BlockPos pos = byPlayer.CurrentBlockSelection?.Position;
            if (!api.World.Claims.TryAccess(byPlayer, pos, EnumBlockAccessFlags.Use))
            {
                return;
            }

            BlockEntitySnitch bes = (pos.BlockEntity(api.World) as BlockEntitySnitch);
            if (TryAccessSnitch(bes, byPlayer) && !byPlayer.Entity.Controls.Sneak)
            {
                StringBuilder breakins = new StringBuilder("Last 5 breakins:").AppendLine();

                if (legacy)
                {
                    for (int i = bes.legacyBreakins.Count - 5; i < bes.legacyBreakins.Count; i++)
                    {
                        if (bes.legacyBreakins.Count == 0) break;
                        if (i < 0) continue;

                        breakins.AppendLine(bes.legacyBreakins[i]);
                    }
                }
                else
                {
                    for (int i = bes.Infractions.Count - 5; i < bes.Infractions.Count; i++)
                    {
                        if (bes.Infractions.Count == 0) break;
                        if (i < 0) continue;

                        breakins.AppendLine(bes.Infractions[i].GetInfString(api.World));
                    }
                }

                sapi.SendMessage(byPlayer, 0, breakins.ToString(), EnumChatType.OwnMessage);
            }
            else if (api.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11)).Any(be => TryAccessSnitch(be as BlockEntitySnitch, byPlayer)))
            {
                foreach (var val in api.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11)))
                {
                    var be = (val as BlockEntitySnitch);
                    if (val is BlockEntitySnitch && be != null && be.OwnerUID == byPlayer.PlayerUID && !byPlayer.Entity.Controls.Sneak)
                    {
                        StringBuilder breakins = new StringBuilder("Last 5 breakins:").AppendLine();

                        if (legacy)
                        {
                            for (int i = be.legacyBreakins.Count - 5; i < be.legacyBreakins.Count; i++)
                            {
                                if (be.legacyBreakins.Count == 0) break;
                                if (i < 0) continue;

                                breakins.AppendLine(bes.legacyBreakins[i]);
                            }
                        }
                        else
                        {
                            for (int i = be.Infractions.Count - 5; i < be.Infractions.Count; i++)
                            {
                                if (be.Infractions.Count == 0) break;
                                if (i < 0) continue;

                                breakins.AppendLine(bes.Infractions[i].GetInfString(api.World));
                            }
                        }

                        sapi.SendMessage(byPlayer, 0, breakins.ToString(), EnumChatType.OwnMessage);
                        break;
                    }
                }
            }
            else
            {
                sapi.SendMessage(byPlayer, 0, "Must look at or be in radius of a snitch, or you don't own this one!", EnumChatType.OwnMessage);
            }
        }

        private void TellEveryoneTheyAreNew(IServerPlayer byPlayer)
        {
            string msg = string.Format("Player {0} is new here, say HI!", byPlayer.PlayerName);
            sapi.SendMessageToGroup(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
        }

        public BlockPos GetSpawn(IServerPlayer byPlayer)
        {
            byPlayer.MigrateOldSpawn();
            var s = byPlayer.GetSpawnPosition(false).XYZInt;

            return new BlockPos(s.X, s.Y, s.Z);
        }

        private void BlockRemovedHandler(IWorldAccessor world, BlockPos pos, Block oldBlock)
        {
            if (oldBlock is BlockBed && oldBlock.Variant["part"] == "head")
            {
                foreach (IServerPlayer player in world.AllPlayers)
                {
                    var s = GetSpawn(player);
                    s.Down();
                    if (s.Equals(pos))
                    {
                        player.MigrateOldSpawn();
                        player.ResetSpawn();
                    }
                }
            }
        }

        private void SnitchInitClient(ICoreClientAPI api)
        {
            api.RegisterCommand("snitchexport", "Export All Breakins To A File", "", (id, args) =>
            {
                BlockPos pos = api?.World?.Player?.CurrentBlockSelection?.Position;
                if (pos != null)
                {
                    var snitch = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntitySnitch;

                    if (snitch == null)
                    {
                        api.ShowChatMessage("Selected block is not a snitch, or is missing its BlockEntity, please replace if so.");
                        return;
                    }

                    StringBuilder builder = new StringBuilder();
                    if (snitch.legacyBreakins.Count != 0)
                    {
                        builder.AppendLine("Legacy Breakins:");
                        foreach (var val in snitch.legacyBreakins)
                        {
                            builder.AppendLine(val);
                        }
                    }

                    builder.AppendLine("Latest Breakins:");

                    builder.AppendLine(snitch.Infractions.Count.ToString());

                    foreach (var val in snitch.Infractions)
                    {
                        builder.AppendLine(val.GetInfString(api.World));
                    }

                    using (TextWriter tw = new StreamWriter("breakins.txt"))
                    {
                        tw.Write(builder);
                        tw.Close();
                    }

                    api.ShowChatMessage("Exported to breakins.txt in game folder.");
                }
            });
        }

        //private void AirBarInitClient(ICoreClientAPI api)
        //{
        //    HudElementAirBar airBar = new HudElementAirBar(api);
        //    airBar.TryOpen();
        //}

        private void UseBlockEvent(IServerPlayer byPlayer, BlockSelection blockSel)
        {

            BlockPos pos = blockSel?.Position;
            Block block = pos?.GetBlock(byPlayer.Entity.World);

            if (block is BlockBed)
            {
                if (block.Variant["part"] == "head")
                {
                    SetSpawnAndBedPos(byPlayer, pos);
                }
                else
                {
                    BlockFacing blockFacing = BlockFacing.FromCode(block.Variant["side"]).Opposite;

                    SetSpawnAndBedPos(byPlayer, pos.AddCopy(blockFacing));
                }
            }

            List<BlockEntity> list = byPlayer.Entity.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11));
            foreach (var entity in list)
            {
                (entity as BlockEntitySnitch)?.NotifyOfUse(byPlayer, block.Id, pos);
            }
        }

        public void SetSpawnAndBedPos(IServerPlayer byPlayer, BlockPos pos)
        {
            byPlayer.MigrateOldSpawn();
            byPlayer.SetTempSpawn(pos);

            byPlayer.SendMessage(0, "Your spawn position has been set.", EnumChatType.Notification);
        }

        private void BreakBlockEvent(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel)
        {
            ItemSlot offhand = byPlayer?.InventoryManager?.GetHotbarInventory()?[10];
            var handling = EnumHandHandling.Handled;
            BlockPos pos = blockSel.Position;

            (offhand?.Itemstack?.Item as ItemPlumbAndSquare)?.OnHeldAttackStart(offhand, byPlayer.Entity, blockSel, null, ref handling);
            if (handling != EnumHandHandling.Handled)
            {
                byPlayer.Entity.World.BlockAccessor.BreakBlock(pos, byPlayer);
            }

            if (pos.BlockEntity(byPlayer.Entity.World) is BlockEntitySnitch)
            {
                byPlayer.Entity.World.BlockAccessor.RemoveBlockEntity(pos);
            }

            List<BlockEntity> list = byPlayer.Entity.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11));
            foreach (var entity in list)
            {
                (entity as BlockEntitySnitch)?.NotifyOfBreak(byPlayer, oldblockId, pos);
            }
        }

        private void PlaceBlockEvent(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
        {
            ItemSlot offhand = byPlayer?.InventoryManager?.GetHotbarInventory()?[10];
            var handling = EnumHandHandling.Handled;
            BlockPos pos = blockSel.Position;

            (offhand?.Itemstack?.Item as ItemPlumbAndSquare)?.OnHeldInteractStart(offhand, byPlayer.Entity, blockSel, null, true, ref handling);

            List<BlockEntity> list = byPlayer.Entity.World.GetBlockEntitiesAround(pos, new Vec2i(11, 11));
            foreach (var entity in list)
            {
                (entity as BlockEntitySnitch)?.NotifyOfPlace(byPlayer, pos);
            }
        }

        public override void Dispose()
        {
            serverConfig?.Save();
            serverConfig?.Dispose();
            base.Dispose();
        }
    }
}