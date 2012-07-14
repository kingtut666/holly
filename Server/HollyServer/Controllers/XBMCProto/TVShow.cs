using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace HollyServer.XBMCProto
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Movie
    {
        [JsonProperty]
        public string label;
        [JsonProperty]
        public int movieid;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TVShow
    {
        [JsonProperty]
        public string label;
        [JsonProperty]
        public int tvshowid;
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class Season
    {
        [JsonProperty]
        public string label;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Episode
    {
        [JsonProperty]
        public int episodeid;
        [JsonProperty]
        public string label;
    }
}
