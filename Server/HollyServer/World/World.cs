using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Speech.Recognition.SrgsGrammar;
using System.Threading;
using System.Globalization;
using System.Xml;

namespace HollyServer
{
    public class TheWorld : IControllable
    {
        List<Room> mRooms;
        Room mCurLoc;

        public TheWorld()
        {
            mRooms = new List<Room>();
        }
        public void AddRoom(Room room)
        {
            mRooms.Add(room);
        }
        public List<Room> ListRooms()
        {
            return mRooms; //TODO: Prob not safe
        }
        public Room CurrentLocation
        {
            get { return mCurLoc; }
            set
            {
                if (!mRooms.Contains(value)) AddRoom(value);
                mCurLoc = value;
            }
        }
        public List<Device> ListDevicesByCaps(Room rm, EDeviceCapabilities caps)
        {
            List<Device> ret = new List<Device>();
            if (rm == null)
            {
                foreach (Room r in mRooms)
                {
                    ret.AddRange(ListDevicesByCaps(r, caps));
                }
            }
            else
            {
                foreach (Device d in rm.ListDevices())
                {
                    if ((d.Capabilities.Caps & caps) == caps) ret.Add(d);
                }
            }

            return ret;
        }

        Dictionary<EDeviceCapabilities, SrgsRule> caps_rules;
        SrgsRuleRef ActionsFromCapabilities(DeviceCapabilities caps, SrgsDocument doc)
        {
            SrgsRule r;
            if (caps_rules.Keys.Contains(caps.Caps))
            {
                r = caps_rules[caps.Caps];
            }
            else
            {
                List<string> capsAsString = caps.Actions;
                if (capsAsString == null || capsAsString.Count == 0) return null;
                SrgsOneOf actions = new SrgsOneOf();
                foreach (string s in capsAsString)
                {
                    SrgsItem si = new SrgsItem(s);
                    //si.Add(new SrgsSemanticInterpretationTag(" out = \"" + s + "\";"));
                    si.Add(new SrgsNameValueTag("action", s));
                    actions.Add(si);
                }
                r = new SrgsRule(AudioRecog.SrgsCleanupID("caps_" + caps.CapsAsIntString), actions);
                doc.Rules.Add(r);
                caps_rules.Add(caps.Caps, r);
            }
            //return new SrgsRuleRef(r, "action");
            return new SrgsRuleRef(r);
        }
        
        SrgsDocument currentSrgsDoc = null;

        public SrgsDocument CreateGrammarDoc()
        {
            //reset caches
            caps_rules = new Dictionary<EDeviceCapabilities, SrgsRule>();
            if (currentSrgsDoc == null)
            {

                currentSrgsDoc = new SrgsDocument();

                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
                //Grammar: Holly, [<courtesy>] <action> (<class>|<device>) [<location>] <courtesy>
                // Courtesy: Please, Thank You, Thanks, Cheers
                // Action: capabilities
                // Class: classes
                // Device: device friendly names
                // Location: rooms, here, this

                SrgsOneOf start_courtesy = new SrgsOneOf(new string[] { "please" });
                SrgsOneOf end_courtesy = new SrgsOneOf(new string[] { "thanks", "thank you", "cheers", "please" });
                SrgsOneOf actionItemLocation_choices = new SrgsOneOf();
                foreach (Room rm in ListRooms())
                {
                    if (rm.Name == "") continue;
                    Dictionary<string, SrgsRuleRef> actionsPerDevice = new Dictionary<string, SrgsRuleRef>();
                    foreach (Device d in rm.ListDevices())
                    {
                        SrgsRuleRef caps_ruleref = ActionsFromCapabilities(d.Capabilities, currentSrgsDoc);
                        if (caps_ruleref == null) continue;
                        string cl = SoundsLike.Get(d.Class, false);
                        if (cl != "" && !actionsPerDevice.ContainsKey(cl))
                        {
                            actionsPerDevice.Add(cl, caps_ruleref);
                        }
                        if (d.FriendlyName != "")
                        {
                            if (!actionsPerDevice.ContainsKey(d.FriendlyName))
                                actionsPerDevice.Add(d.FriendlyName, caps_ruleref);
                        }
                    }
                    //actionsperdevice.Value + actionsperdevice.Key+room
                    if (actionsPerDevice.Count == 0) continue; //nothing in the room
                    SrgsOneOf action_items = new SrgsOneOf();
                    bool action_items_valid = false;
                    foreach (string item in actionsPerDevice.Keys)
                    {
                        if (item == "") continue;
                        SrgsRule ai_gb = new SrgsRule(AudioRecog.SrgsCleanupID(rm.Name + "_" + item));
                        ai_gb.Add(actionsPerDevice[item]);
                        ai_gb.Add(new SrgsItem(item));
                        currentSrgsDoc.Rules.Add(ai_gb);
                        //SrgsItem ai_gb_item = new SrgsItem(new SrgsRuleRef(ai_gb, "item"));
                        SrgsItem ai_gb_item = new SrgsItem(new SrgsRuleRef(ai_gb));
                        ai_gb_item.Add(new SrgsNameValueTag("item", item));
                        action_items.Add(ai_gb_item);
                        action_items_valid = true;
                    }
                    if (!action_items_valid) continue;
                    SrgsRule ail_gb = new SrgsRule(AudioRecog.SrgsCleanupID(rm.Name + "__ail"), action_items);
                    if (rm != CurrentLocation)
                    {
                        //SrgsItem loc1 = new SrgsItem("in the " + rm.Name);
                        SrgsItem loc1 = new SrgsItem(0, 1, "in the " + rm.Name);
                        loc1.Add(new SrgsNameValueTag("room", rm.Name));
                        ail_gb.Add(loc1);
                    }
                    else
                    {
                        SrgsOneOf loc = new SrgsOneOf(new string[] { "in the " + rm.Name, "here" });
                        SrgsItem loc_item = new SrgsItem(0, 1, loc);
                        //SrgsItem loc_item = new SrgsItem(loc);
                        loc_item.Add(new SrgsNameValueTag("room", rm.Name));
                        ail_gb.Add(loc_item);
                    }
                    currentSrgsDoc.Rules.Add(ail_gb);
                    //SrgsItem ail_gb_item = new SrgsItem(new SrgsRuleRef(ail_gb, "room"));
                    SrgsItem ail_gb_item = new SrgsItem(new SrgsRuleRef(ail_gb));
                    //ail_gb_item.Add(new SrgsNameValueTag("room", rm.Name));
                    actionItemLocation_choices.Add(ail_gb_item);
                }
                SrgsRule root_rule = new SrgsRule("rootrule");
                root_rule.Add(new SrgsItem("Holly"));
                root_rule.Add(new SrgsItem(0, 1, start_courtesy));
                root_rule.Add(actionItemLocation_choices);
                root_rule.Add(end_courtesy);
                currentSrgsDoc.Rules.Add(root_rule);
                currentSrgsDoc.Root = root_rule;

                XmlWriter xmlout = XmlWriter.Create(@"C:\Users\ian\Desktop\grammar.xml");
                currentSrgsDoc.WriteSrgs(xmlout);
                xmlout.Close();
            }
            return currentSrgsDoc;
        }
        public string GetName()
        {
            return "The World";
        }
        public bool OnSpeechRecognised(string ID, System.Speech.Recognition.RecognitionResult result)
        {
            string room = "";
            if (result.Semantics.ContainsKey("room")) room = result.Semantics["room"].Value.ToString();
            string item = "";
            if (result.Semantics.ContainsKey("item")) item = result.Semantics["item"].Value.ToString();
            string action = "";
            if (result.Semantics.ContainsKey("action")) action = result.Semantics["action"].Value.ToString();

            Command c = new Command(room, item, action, this);
            Form1.server.protos.Execute(c);
            return true;
        }


        public event IControllableEventDelegate ControllableEvent;
        void OnControllableEvent(IControllable from, IControllableEventArgs Event)
        {
            if (ControllableEvent != null)
            {
                ControllableEvent(from, Event);
            }
        }







        static public TheWorld CreateTestWorld()
        {
            Room livingroom = new Room("living room");
            Room study = new Room("study");
            Room bedroom = new Room("bedroom");
            Room corridor = new Room("corridor");
            corridor.AddDoorway(livingroom);
            corridor.AddDoorway(study);
            corridor.AddDoorway(bedroom);

            Device study_light = new Device("R1D1", "study light", EDeviceClass.Light, EDeviceProtocol.LWRF,
                new DeviceCapabilities("Dimmer"), null);
            Device bedroom_light = new Device("R2D1", "bedroom light", EDeviceClass.Light, EDeviceProtocol.LWRF,
                new DeviceCapabilities("Dimmer"), null);
            Device study_kinect = new Device("192.168.1.32", "study kinect", EDeviceClass.Kinect, EDeviceProtocol.AudioProto,
                new DeviceCapabilities("AudioIn"), null);
            Device bedroom_kinect = new Device("192.168.1.191", "bedroom kinect", EDeviceClass.Kinect, EDeviceProtocol.AudioProto,
                new DeviceCapabilities("AudioIn"), null);
            Dictionary<string,string> study_xbmc_args = new Dictionary<string,string>();
            study_xbmc_args.Add("port", "8080");
            study_xbmc_args.Add("user", "xbmc");
            study_xbmc_args.Add("pass", "test");
            Device study_xbmc = new Device("192.168.1.130", "study xbmc", EDeviceClass.XBMC, EDeviceProtocol.XBMC,
                new DeviceCapabilities("XBMC"), study_xbmc_args);
            Dictionary<string, string> bedroom_xbmc_args = new Dictionary<string, string>();
            bedroom_xbmc_args.Add("port", "8080");
            bedroom_xbmc_args.Add("user", "xbmc");
            bedroom_xbmc_args.Add("pass", "xbmc");
            Device bedroom_xbmc = new Device("192.168.1.191", "bedroom xbmc", EDeviceClass.XBMC, EDeviceProtocol.XBMC,
                new DeviceCapabilities("XBMC"), bedroom_xbmc_args);

            study.AddDevice(study_xbmc);
            study.AddDevice(study_kinect);
            study.AddDevice(study_light);
            bedroom.AddDevice(bedroom_xbmc);
            bedroom.AddDevice(bedroom_kinect);
            bedroom.AddDevice(bedroom_light);

            TheWorld ret = new TheWorld();
            ret.AddRoom(livingroom);
            ret.AddRoom(study);
            ret.AddRoom(bedroom);
            ret.AddRoom(corridor);

            ret.CurrentLocation = study;

            return ret;
        }





    }
}
