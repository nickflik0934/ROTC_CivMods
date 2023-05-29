using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace CivMods
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class CivModsServerConfig
    {
        private ICoreServerAPI sapi;

        //[JsonProperty]
        //private float suffocateRateLight = 0.01f;

        //[JsonProperty]
        //private float suffocateRateHeavy = 2.00f;

        //[JsonProperty]
        //private float breathRate = 0.25f;

        [JsonProperty]
        private IPBan[] ipBans = new IPBan[0];

        [JsonProperty]
        private int ottTimeout = 120;
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
            string saveFileDirectory = this.SaveFileDirectory;
            if (!Directory.Exists(saveFileDirectory))
            {
                Directory.CreateDirectory(saveFileDirectory);
            }
            if (!File.Exists(this.SaveFileName))
            {
                sapi.World.Config.SetString("CivModsOTTUsedFile", sapi.WorldManager.SaveGame.WorldName);
            }
            LoadPlayerData();
        }

        public IPBan[] IPBans
        {
            get { Load(); return ipBans; }
            set { ipBans = value; Save(); }
        }

        public double ClassChangeDelayInDays
        {
            get { Load(); return classChangeDelayInDays; }
            set { classChangeDelayInDays = value; Save(); }
        }

        public int OTTTimeout
        {
            get { Load(); return ottTimeout; }
            set { ottTimeout = value; Save(); }
        }

        public void addOTT(OTTUse ott)
        {
            // Basically List<T>.Add for arrays
            this.ottUsed = this.ottUsed.Concat(new OTTUse[] { ott }).ToArray();
            SaveOTT();
        }

        public OTTUse[] getOTTUsed()
        {
            return this.ottUsed;
        }

        private OTTUse[] ottUsed;

        public string SaveFileDirectory
        {
            get {
                return Path.Combine(GamePaths.Saves, "CivMods");
            }
        }

        public string SaveFileName
        {
            get
            {
                string str = new string(Path.GetInvalidFileNameChars());
                Regex regex = new Regex(string.Format("[{0}]", Regex.Escape(str)));
                string text = this.sapi.World.Config.GetString("CivModsOTTUsedFile", null) ?? this.sapi.WorldManager.SaveGame.WorldName;
                text = regex.Replace(text, "");
                return Path.Combine(this.SaveFileDirectory, text + ".json");
            }
        }

        public void SaveOTT()
        {
            JsonSerializer jsonSerializer = new JsonSerializer();
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializer.Formatting = Formatting.Indented;

            using (StreamWriter streamWriter = new StreamWriter(this.SaveFileName))
            using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
            {
                jsonSerializer.Serialize(jsonWriter, this.ottUsed);
            }
        }

        public void LoadPlayerData()
        {
            if (File.Exists(this.SaveFileName))
            {
                try
                {
                    using (StreamReader streamReader = new StreamReader(this.SaveFileName))
                    using (JsonReader jsonReader = new JsonTextReader(streamReader))
                    {
                        JsonSerializer jsonSerializer = new JsonSerializer();
                        this.ottUsed = jsonSerializer.Deserialize<OTTUse[]>(jsonReader);
                    }

                    return;
                }
                catch (Exception ex)
                {
                    sapi?.Logger.Error("Malformed Data file for ott use, Exception: \n {0}", ex.StackTrace);
                }
            }
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

                ipBans = conf.ipBans;
                classChangeDelayInDays = conf.classChangeDelayInDays;
                ottTimeout = conf.ottTimeout;

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