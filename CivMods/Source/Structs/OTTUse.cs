using Newtonsoft.Json;
using System;
using System.Linq;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace CivMods
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal struct OTTUse
    {
        private string uid;

        [JsonProperty]
        internal string playerUID
        {
            get
            {
                return uid;
            }
            set
            {
                uid = value;
            }
        }

        public OTTUse(string uid)
        {
            this.uid = uid;
        }
    }
}