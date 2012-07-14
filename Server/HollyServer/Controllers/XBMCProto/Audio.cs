using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace HollyServer.XBMCProto
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Genre
    {
        [JsonProperty]
        public string label;
        [JsonProperty]
        public int genreid;
    }
}
