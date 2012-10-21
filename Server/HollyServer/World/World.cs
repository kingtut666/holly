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
        SrgsRuleRef SrgsActionsFromCapabilities(DeviceCapabilities caps, SrgsDocument doc)
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
                r = new SrgsRule(AudioRecog_SAPI.SrgsCleanupID("caps_" + caps.CapsAsIntString), actions);
                doc.Rules.Add(r);
                caps_rules.Add(caps.Caps, r);
            }
            //return new SrgsRuleRef(r, "action");
            return new SrgsRuleRef(r);
        }
        Dictionary<EDeviceCapabilities, string> caps_rules_jsgf;
        string JSGFActionsFromCapabilities(DeviceCapabilities caps, CMUSphinx_GrammarDict cgd)
        {
            string capsName = "<caps_" + caps.CapsAsIntString + ">";
            if (caps_rules_jsgf.Keys.Contains(caps.Caps))
            {
                return caps_rules_jsgf[caps.Caps];
            }
            else
            {
                StringBuilder b = new StringBuilder();

                cgd.JSGFRuleStart(capsName, b);

                List<string> capsAsString = caps.Actions;
                if (capsAsString == null || capsAsString.Count == 0)
                {
                    cgd.JSGFRuleCancel(capsName, b);
                    return null;
                }
                cgd.JSGFRuleAddChoicesStart(b, capsAsString);
                cgd.JSGFRuleAddChoicesEnd(b);
                cgd.JSGFRuleEnd(capsName, b);

                caps_rules_jsgf.Add(caps.Caps, capsName);
            }
            //return new SrgsRuleRef(r, "action");
            return capsName;
        }
        Dictionary<EDeviceCapabilities, Tuple<CMUSphinx_FSGState, CMUSphinx_FSGState>> caps_rules_fsg;
        bool FSGActionsFromCapabilities(DeviceCapabilities caps, CMUSphinx_GrammarDict cgd, 
            ref CMUSphinx_FSGState startState, ref CMUSphinx_FSGState endState)
        {
            string capsName = "<caps_" + caps.CapsAsIntString + ">";
            if (caps_rules_fsg.Keys.Contains(caps.Caps))
            {
                startState = caps_rules_fsg[caps.Caps].Item1;
                endState = caps_rules_fsg[caps.Caps].Item2;
                return true;
            }
            List<string> capsAsString = caps.Actions;
            if (capsAsString == null || capsAsString.Count == 0)
            {
                startState = null;
                endState = null;
                return false;
            }
            CMUSphinx_FSGState start = cgd.FSGCreateOrphanState();
            CMUSphinx_FSGState end = cgd.FSGCreateOrphanState();
            foreach (string s in capsAsString)
            {
                cgd.FSGLinkStates(start, end, s);
            }
            caps_rules_fsg.Add(caps.Caps, new Tuple<CMUSphinx_FSGState,CMUSphinx_FSGState>(start, end));
            startState = start;
            endState = end;
            //return new SrgsRuleRef(r, "action");
            return true;
        }
        
        SrgsDocument currentSrgsDoc = null;
        public SrgsDocument CreateGrammarDoc_SRGS()
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

                SrgsOneOf end_courtesy = new SrgsOneOf(new string[] { "please" });
                SrgsOneOf actionItemLocation_choices = new SrgsOneOf();
                foreach (Room rm in ListRooms())
                {
                    if (rm.Name == "") continue;
                    Dictionary<string, SrgsRuleRef> actionsPerDevice = new Dictionary<string, SrgsRuleRef>();
                    foreach (Device d in rm.ListDevices())
                    {
                        SrgsRuleRef caps_ruleref = SrgsActionsFromCapabilities(d.Capabilities, currentSrgsDoc);
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
                        SrgsRule ai_gb = new SrgsRule(AudioRecog_SAPI.SrgsCleanupID(rm.Name + "_" + item));
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
                    SrgsRule ail_gb = new SrgsRule(AudioRecog_SAPI.SrgsCleanupID(rm.Name + "__ail"), action_items);
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
        static CMUSphinx_GrammarDict currentCMUDoc = null;
        public CMUSphinx_GrammarDict CreateGrammarDoc_JSGF()
        {
            if (currentCMUDoc != null) return currentCMUDoc;
            caps_rules_jsgf = new Dictionary<EDeviceCapabilities, string>();
            CMUSphinx_GrammarDict ret = new CMUSphinx_GrammarDict();
            ret.GrammarName = GetName();
            StringBuilder bld = new StringBuilder();
            StringBuilder actionItemLocs = new StringBuilder();

            ret.JSGFRuleStart("<ACTIONITEMLOCS>", actionItemLocs);
            List<string> actionItemLocation_choices = new List<string>();
            foreach (Room rm in ListRooms())
            {
                if (rm.Name == "") continue;
                Dictionary<string, string> actionsPerDevice = new Dictionary<string, string>();
                foreach (Device d in rm.ListDevices())
                {
                    string caps_ruleref = JSGFActionsFromCapabilities(d.Capabilities, ret);
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
                StringBuilder bld_ail_gb = new StringBuilder();
                string name_ail_gb = "<"+AudioRecog_SAPI.SrgsCleanupID(rm.Name + "__ail")+">";
                ret.JSGFRuleStart(name_ail_gb, bld_ail_gb);

                List<string> action_items = new List<string>();
                bool action_items_valid = false;
                foreach (string item in actionsPerDevice.Keys)
                {
                    if (item == "") continue;
                    StringBuilder bld_ai_gb = new StringBuilder();
                    string name_ai_gb = "<" + AudioRecog_SAPI.SrgsCleanupID(rm.Name + "_" + item) + ">";

                    ret.JSGFRuleStart(name_ai_gb, bld_ai_gb);
                    ret.JSGFRuleAddToken(bld_ai_gb, actionsPerDevice[item]);
                    ret.JSGFRuleAddToken(bld_ai_gb, item);
                    ret.JSGFRuleEnd(name_ai_gb, bld_ai_gb);

                    action_items.Add(name_ai_gb);
                    action_items_valid = true;
                }
                if (!action_items_valid)
                {
                    ret.JSGFRuleCancel(name_ail_gb, bld_ail_gb);
                    continue;
                }
                ret.JSGFRuleAddChoicesStart(bld_ail_gb, action_items);
                ret.JSGFRuleAddChoicesEnd(bld_ail_gb);
                if (rm != CurrentLocation)
                {
                    ret.JSGFRuleAddToken(bld_ail_gb, "in the " + rm.Name);
                    ret.JSGFRuleUnaryOp(bld_ail_gb, true, false);
                }
                else
                {
                    ret.JSGFRuleAddChoicesStart(bld_ail_gb, new List<string>(new string[] { "in the " + rm.Name, "here" }));
                    ret.JSGFRuleAddChoicesEnd(bld_ail_gb);
                    ret.JSGFRuleUnaryOp(bld_ail_gb, true, false);
                }
                ret.JSGFRuleEnd(name_ail_gb, bld_ail_gb);
                actionItemLocation_choices.Add(name_ail_gb);
            }
            ret.JSGFRuleAddChoicesStart(actionItemLocs, actionItemLocation_choices);
            ret.JSGFRuleAddChoicesEnd(actionItemLocs);
            ret.JSGFRuleEnd("<ACTIONITEMLOCS>", actionItemLocs);


            ret.JSGFRuleStart("<ROOT>", bld);
            ret.JSGFRuleAddToken(bld, "Holly");
            ret.JSGFRuleAddToken(bld, "<ACTIONITEMLOCS>");
            ret.JSGFRuleAddToken(bld, "please");
            ret.JSGFRuleEnd("<ROOT>", bld);

            ret.JSGFSetRootRule("<ROOT>");

            ret.BuildJSGFGrammarAndDict();
            
            currentCMUDoc = ret;
            return ret;
        }
        public CMUSphinx_GrammarDict CreateGrammarDoc_FSG()
        {
            if (currentCMUDoc != null) return currentCMUDoc;
            caps_rules_fsg = new Dictionary<EDeviceCapabilities,Tuple<CMUSphinx_FSGState,CMUSphinx_FSGState>>();
            CMUSphinx_GrammarDict ret = new CMUSphinx_GrammarDict();
            ret.GrammarName = GetName();

            CMUSphinx_FSGState root = ret.FSGCreate(GetName());
            CMUSphinx_FSGState holly = ret.FSGTransitionToNewState(root, "Holly");
            // HOLLY <ACTION> (<DEVICECLASS> [(<ROOM> | HERE)] | <DEVICENAME> ) PLEASE


            //only add to this list if the action only has a single output, "Please"
            CMUSphinx_FSGState preSinglePlease = ret.FSGCreateOrphanState();
            CMUSphinx_FSGState postSinglePlease = ret.FSGCreateOrphanState();
            ret.FSGLinkStates(preSinglePlease, postSinglePlease, "please");
            foreach (Room rm in ListRooms())
            {
                if (rm.Name == "") continue;
                CMUSphinx_FSGState actionDevicePreRoom = null;

                foreach (Device d in rm.ListDevices())
                {
                    CMUSphinx_FSGState startAction = null, endAction = null;
                    bool caps_valid = FSGActionsFromCapabilities(d.Capabilities, ret, ref startAction, ref endAction);
                    if (!caps_valid) continue;
                    ret.FSGGroupStates(holly, startAction); //Action immediately follows Holly
                    string cl = SoundsLike.Get(d.Class, false);
                    if (cl != "")
                    {
                        if(actionDevicePreRoom == null)
                            actionDevicePreRoom = ret.FSGCreateOrphanState();
                        ret.FSGLinkStates(startAction, actionDevicePreRoom, cl);
                    }
                    if (d.FriendlyName != "")
                    {
                        ret.FSGLinkStates(endAction, preSinglePlease, d.FriendlyName);
                    }
                }
                if (actionDevicePreRoom == null) continue; //no device classes in room
  
                if (rm != CurrentLocation)
                {
                    ret.FSGLinkStates(actionDevicePreRoom, preSinglePlease, "in the "+rm.Name);
                }
                else
                {
                    ret.FSGLinkStates(actionDevicePreRoom, preSinglePlease, "in the " + rm.Name);
                    ret.FSGLinkStates(actionDevicePreRoom, preSinglePlease, "here");
                }
                ret.FSGLinkStates(actionDevicePreRoom, postSinglePlease, "please");
            }
            ret.FSGGroupStates(ret.FSGGetEndState(), postSinglePlease);

            ret.BuildFSGGrammarAndDict();

            currentCMUDoc = ret;
            return ret;
        }
        
        public string GetName()
        {
            return "The_World";
        }
        public bool OnSpeechRecognised(string ID, RecognitionSuccess result)
        {
            string room = "";
            if (result.getSemanticValuesAsString("room") != null) room = result.getSemanticValuesAsString("room");
            string item = "";
            if (result.getSemanticValuesAsString("item") != null) item = result.getSemanticValuesAsString("item");
            string action = "";
            if (result.getSemanticValuesAsString("action") != null) action = result.getSemanticValuesAsString("action");

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
            Device living_kinect = new Device("192.168.1.190", "living room kinect", EDeviceClass.Kinect, EDeviceProtocol.AudioProto,
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
            Dictionary<string, string> living_xbmc_args = new Dictionary<string, string>();
            living_xbmc_args.Add("port", "8080");
            living_xbmc_args.Add("user", "xbmc");
            living_xbmc_args.Add("pass", "xbmc");
            Device living_xbmc = new Device("192.168.1.190", "living room xbmc", EDeviceClass.XBMC, EDeviceProtocol.XBMC,
                new DeviceCapabilities("XBMC"), living_xbmc_args);

            study.AddDevice(study_xbmc);
            study.AddDevice(study_kinect);
            study.AddDevice(study_light);
            bedroom.AddDevice(bedroom_xbmc);
            bedroom.AddDevice(bedroom_kinect);
            bedroom.AddDevice(bedroom_light);
            livingroom.AddDevice(living_xbmc);
            livingroom.AddDevice(living_kinect);

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
