using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json.Serialization;
using System.Net;
using Newtonsoft.Json;

namespace HollyServer.XBMCProto
{
    public class JSONParser
    {
        string mMethod;
        WebClient mClient;
        List<Tuple<string,object,string>> mParams = new List<Tuple<string,object,string>>();
        public JSONParser(string method, WebClient clnt){
            mClient = clnt;
            mMethod = method;
        }
        public void AddParameter(string name, int val){
            mParams.Add(new Tuple<string, object, string>(name, val, "int"));
        }
        public void AddParameter(string name, string val, string type)
        {
            mParams.Add(new Tuple<string, object, string>(name, val, type));
        }
        public void AddParameter(string name, bool val)
        {
            mParams.Add(new Tuple<string, object, string>(name, val, "boolean"));
        }
        string Execute()
        {
            StringBuilder bld = new StringBuilder();
            StringWriter sw = new StringWriter(bld);

            sw.Write("{\"jsonrpc\":\"2.0\",\"method\":\""+mMethod+"\",\"id\":\""+ID+"\"");
            if (mParams.Count > 0)
            {
                //todo: locking maybe
                sw.Write(",\"params\":{");
                int i = 0;
                foreach (Tuple<string, object, string> p in mParams)
                {
                    sw.Write("\"" + p.Item1 + "\":");
                    switch (p.Item3)
                    {
                        case "int":
                            sw.Write((int)p.Item2);
                            break;
                        case "string":
                            sw.Write("\"");
                            sw.Write((string)p.Item2);
                            sw.Write("\"");
                            break;
                        case "object":
                            sw.Write("{");
                            sw.Write((string)p.Item2);
                            sw.Write("}");
                            break;
                        case "boolean":
                            sw.Write((string)p.Item2);
                            break;
                    }
                    i++;
                    if (i != mParams.Count) sw.Write(",");
                }
                sw.Write("}");
            }
            sw.Write("}");
            string query = bld.ToString();

            string ret = mClient.UploadString("", query);
            return ret;
        }

        public JSONRPCQueryResultRes Execute_Obj()
        {
            string s_res = Execute();
            JSONRPCQueryResult_Obj res = JsonConvert.DeserializeObject<JSONRPCQueryResult_Obj>(s_res);

            if (res == null) return null;
            return res.result;
        }
        public string Execute_Str()
        {
            string s_res = Execute();
            JSONRPCQueryResult_Str res = JsonConvert.DeserializeObject<JSONRPCQueryResult_Str>(s_res);

            if (res == null) return null;
            return res.result;
        }
        public string Execute_None()
        {
            string s_res = Execute();
            return "OK";
            //JSONRPCQueryResult_Str res = JsonConvert.DeserializeObject<JSONRPCQueryResult_Str>(s_res);

            //if (res == null) return null;
            //return res.result;
        }
        string ID
        {
            get {
                return mMethod.Replace(".", "");
            }
        }

    }
}
