using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace HollyServer.XBMCProto
{
    [JsonObject(MemberSerialization.OptIn)]
    public class JSONRPCQueryResultLimits
    {
        [JsonProperty]
        public int end;
        [JsonProperty]
        public int start;
        [JsonProperty]
        public int total;
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class JSONRPCQueryResultRes
    {
        [JsonProperty]
        public JSONRPCQueryResultLimits limits;
        [JsonProperty]
        public List<Movie> movies;
        [JsonProperty]
        public List<TVShow> tvshows;
        [JsonProperty]
        public List<Season> seasons;
        [JsonProperty]
        public List<Episode> episodes;
        [JsonProperty]
        public List<Genre> genres;
    }

    public class JSONRPCQueryResult_Obj
    {
        [JsonProperty]
        public string id;
        [JsonProperty]
        public string jsonrpc;
        [JsonProperty]
        public JSONRPCQueryResultRes result;
    }
    public class JSONRPCQueryResult_Str
    {
        [JsonProperty]
        public string id;
        [JsonProperty]
        public string jsonrpc;
        [JsonProperty]
        public string result;
    }
}
