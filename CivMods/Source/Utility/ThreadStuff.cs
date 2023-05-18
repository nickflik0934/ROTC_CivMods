using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Server;

namespace CivMods
{
    internal static class ThreadStuff
    {
        private static Type clientThreadType;
        private static Type serverThreadType;

        static ThreadStuff()
        {
            var ts = AccessTools.GetTypesFromAssembly(Assembly.GetAssembly(typeof(ClientMain)));
            clientThreadType = ts.Where((t, b) => t.Name == "ClientThread").Single();
            ts = AccessTools.GetTypesFromAssembly(Assembly.GetAssembly(typeof(ServerMain)));
            serverThreadType = ts.Where((t, b) => t.Name == "ServerThread").Single();
        }

        public static Thread InjectClientThread(this ICoreClientAPI capi, string name, params ClientSystem[] systems) => capi.World.InjectClientThread(name, systems);

        public static Thread InjectClientThread(this IClientWorldAccessor world, string name, params ClientSystem[] systems)
        {
            object instance;
            Thread thread;

            instance = clientThreadType.CreateInstance();
            instance.SetField("game", world as ClientMain);
            instance.SetField("threadName", name);
            instance.SetField("clientsystems", systems);
            instance.SetField("lastFramePassedTime", new Stopwatch());
            instance.SetField("totalPassedTime", new Stopwatch());
            instance.SetField("paused", false);

            List<Thread> clientThreads = (world as ClientMain).GetField<List<Thread>>("clientThreads");
            Stack<ClientSystem> vanillaSystems = new Stack<ClientSystem>((world as ClientMain).GetField<ClientSystem[]>("clientSystems"));

            foreach (var system in systems)
            {
                vanillaSystems.Push(system);
            }

            (world as ClientMain).SetField("clientSystems", vanillaSystems.ToArray());

            thread = new Thread(() => instance.CallMethod("Process"))
            {
                IsBackground = true,
                Name = name
            };

            thread.Start();

            clientThreads.Add(thread);

            return thread;
        }

        public static Thread InjectServerThread(this ICoreServerAPI sapi, string name, params ServerSystem[] systems) => sapi.World.InjectServerThread(name, systems);

        public static Thread InjectServerThread(this IServerWorldAccessor world, string name, params ServerSystem[] systems)
        {
            object instance;
            Thread thread;

            instance = serverThreadType.CreateInstance();
            instance.SetField("server", world as ServerMain);
            instance.SetField("threadName", name);
            instance.SetField("serversystems", systems);
            instance.SetField("lastFramePassedTime", new Stopwatch());
            instance.SetField("totalPassedTime", new Stopwatch());
            instance.SetField("paused", false);

            List<Thread> serverThreads = (world as ServerMain).GetField<List<Thread>>("Serverthreads");
            Stack<ServerSystem> vanillaSystems = new Stack<ServerSystem>((world as ServerMain).GetField<ServerSystem[]>("Systems"));

            foreach (var system in systems)
            {
                vanillaSystems.Push(system);
            }

            (world as ServerMain).SetField("Systems", vanillaSystems.ToArray());

            thread = new Thread(() => instance.CallMethod("Process"))
            {
                IsBackground = true,
                Name = name
            };

            thread.Start();

            serverThreads.Add(thread);

            return thread;
        }
    }
}