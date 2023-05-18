using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace CivMods
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class CivModsServerConfig
    {
        private ICoreServerAPI sapi;

        [JsonProperty]
        private float suffocateRateLight = 0.01f;

        [JsonProperty]
        private float suffocateRateHeavy = 2.00f;

        [JsonProperty]
        private float breathRate = 0.25f;

        [JsonProperty]
        private IPBan[] ipBans = new IPBan[0];

        /*
        [JsonProperty]
        private bool dummySlotDamagePatch = false;
        */

        [JsonProperty]
        private double classChangeDelayInDays = 2.0;

        public CivModsServerConfig()
        {
        }

        public void Initialize(ICoreServerAPI sapi)
        {
            this.sapi = sapi;
            sapi.ModLoader.GetModSystem<HarmonyPatcher>().RePatch(sapi);
        }

        public float SuffocateRateLight
        {
            get { Load(); return suffocateRateLight; }
            set { suffocateRateLight = value; Save(); }
        }

        public float SuffocateRateHeavy
        {
            get { Load(); return suffocateRateHeavy; }
            set { suffocateRateLight = value; Save(); }
        }

        public float BreathRate
        {
            get { Load(); return breathRate; }
            set { breathRate = value; Save(); }
        }

        public IPBan[] IPBans
        {
            get { Load(); return ipBans; }
            set { ipBans = value; Save(); }
        }

        /*
        public bool DummySlotDamagePatch
        {
            get { Load(); return dummySlotDamagePatch; }
            set { dummySlotDamagePatch = value; Save(); }
        }
        */

        public double ClassChangeDelayInDays
        {
            get { Load(); return classChangeDelayInDays; }
            set { classChangeDelayInDays = value; Save(); }
        }

        public void Save()
        {
            sapi?.StoreModConfig(this, "civmods/server.json");
        }

        public void Load()
        {
            try
            {
                var conf = sapi?.LoadModConfig<CivModsServerConfig>("civmods/server.json") ?? new CivModsServerConfig();

                suffocateRateLight = conf.suffocateRateLight;
                suffocateRateHeavy = conf.suffocateRateHeavy;
                breathRate = conf.breathRate;
                ipBans = conf.ipBans;
                classChangeDelayInDays = conf.classChangeDelayInDays;

                /*
                if (dummySlotDamagePatch != conf.dummySlotDamagePatch)
                {
                    dummySlotDamagePatch = conf.dummySlotDamagePatch;
                    sapi?.ModLoader.GetModSystem<HarmonyPatcher>().RePatch(sapi);
                }
                */
                
            }
            catch (Exception ex)
            {
                sapi?.Logger.Error("Malformed ModConfig file civmods/server.json, Exception: \n {0}", ex.StackTrace);
            }
        }

        public void Dispose()
        {
            sapi = null;
        }
    }
}