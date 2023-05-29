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
        [JsonProperty]
        public string uid;

        public OTTUse(string uid)
        {
            this.uid = uid;
        }
    }
}