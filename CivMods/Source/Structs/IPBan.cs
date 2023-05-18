using Newtonsoft.Json;
using System;
using System.Linq;
using Vintagestory.API.Util;

namespace CivMods
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal struct IPBan
    {
        private uint ip;
        
        [JsonProperty]
        internal string Reason;
        
        [JsonProperty]
        internal DateTime UntilDate;

        [JsonProperty]
        internal string IP
        {
            get
            {
                return ip.Int2IP();
            }
            set
            {
                Valid = value.IP2Int(out ip);
            }
        }

        [JsonProperty]
        internal byte range;

        internal bool Valid;

        public IPBan(string ip, string reason, DateTime untilDate, byte range = 4)
        {
            this.ip = 0;
            
            Valid = true;

            Reason = reason;
            UntilDate = untilDate;

            Valid = ip.IP2Int(out this.ip);
            this.range = range;
        }

        public bool IsIpBanned(uint ip)
        {
            bool banned = false;

            var oursbytes = this.ip.Int2Bytes();
            var thembytes = ip.Int2Bytes();

            for (int i = 0; i < range; i++)
            {
                banned = oursbytes[i] == thembytes[i];
            }

            return banned;
        }

        public string GetString()
        {
            return string.Format(@"{0} banned for '{1}' until {2} at {3}", IP, Reason, UntilDate.ToShortDateString(), UntilDate.ToShortTimeString());
        }

        public override int GetHashCode()
        {
            return IP.GetHashCode();
        }
    }
}