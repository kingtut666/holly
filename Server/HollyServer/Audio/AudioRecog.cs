using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Speech.Recognition;
using System.Speech.AudioFormat;
using System.Globalization;
using System.Threading;
using System.Reflection;
using System.Speech.Recognition.SrgsGrammar;
using System.Text.RegularExpressions;
using System.Xml;

namespace HollyServer
{
    public class AudioInstance
    {
        public string ID;
        public SpeechRecognitionEngine engine;
        public Stream audioStream;
        public bool NeedNewGrammar;
        public bool Active;
    }

    public class SoundsLike : Attribute
    {
        public string Text;
        public SoundsLike(string t)
        {
            Text = t;
        }
        public static string Get(Enum en, bool nullIfNotPresent)
        {
            Type type = en.GetType();
            MemberInfo[] memInfo = type.GetMember(en.ToString());

            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(SoundsLike), false);
                if (attrs != null && attrs.Length > 0)
                    return ((SoundsLike)attrs[0]).Text;
            }
            if (nullIfNotPresent) return null;
            return en.ToString();
        }
    }

    public class AudioRecog
    {
        Dictionary<string, SrgsDocument> mDocs;
        List<AudioInstance> engines;

        public AudioRecog()
        {
            engines = new List<AudioInstance>();
            //mGrammar = CreateColorGrammar();
            mDocs = new Dictionary<string, SrgsDocument>();
            AddRecognizer(false);
        }
        public void RemoveGrammar(string name)
        {
            lock (mDocs)
            {
                if (mDocs.ContainsKey(name)) mDocs.Remove(name);
            }

            lock (engines)
            {
                foreach (AudioInstance ai in engines)
                {
                    if (!ai.Active)
                    {
                        //ai.engine.UnloadAllGrammars();
                        lock (mDocs)
                        {
                            Grammar todel = null;
                            foreach(Grammar g in ai.engine.Grammars){
                                if(g.Name == name){
                                    todel = g;
                                    break;
                                }
                            }
                            ai.engine.UnloadGrammar(todel);
                        }
                        ai.NeedNewGrammar = false;
                    }
                    else ai.NeedNewGrammar = true;
                }
            }
        }
        public void AddGrammar(string name, SrgsDocument doc)
        {
            lock (mDocs)
            {
                if (mDocs.ContainsKey(name)) mDocs[name] = doc;
                mDocs.Add(name, doc);
            }

            lock (engines)
            {
                foreach (AudioInstance ai in engines)
                {
                    if (!ai.Active)
                    {
                        //ai.engine.UnloadAllGrammars();
                        lock (mDocs)
                        {
                            ai.engine.LoadGrammar(CreateGrammar(name, doc));
                            //foreach (string nm in mDocs.Keys)
                            //{
                            //    ai.engine.LoadGrammar(CreateGrammar(nm, mDocs[nm]));
                            //}
                        }
                        ai.NeedNewGrammar = false;
                    }
                    else ai.NeedNewGrammar = true;
                }
            }
        }

        static string disallowedSrgsChars = @"[?*+|()^$/;.=<>\[\]{}\\ \t\r\n]";
        public static string SrgsCleanupID(string id)
        {
            Regex r = new Regex(disallowedSrgsChars);
            return r.Replace(id, "_");
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
                r = new SrgsRule(SrgsCleanupID("caps_" + caps.CapsAsIntString), actions);
                doc.Rules.Add(r);
                caps_rules.Add(caps.Caps, r);
            }
            //return new SrgsRuleRef(r, "action");
            return new SrgsRuleRef(r);
        }
        
        

        SrgsDocument currentSrgsDoc;
        Grammar CreateGrammar(string name, SrgsDocument doc)
        {
            Grammar g = new Grammar(doc);
            g.Name = name;
            return g;
        }
        SrgsDocument CreateGrammarDoc(TheWorld theWorld){
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
                foreach (Room rm in theWorld.ListRooms())
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
                        SrgsRule ai_gb = new SrgsRule(SrgsCleanupID(rm.Name + "_" + item));
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
                    SrgsRule ail_gb = new SrgsRule(SrgsCleanupID(rm.Name + "__ail"), action_items);
                    if (rm != theWorld.CurrentLocation)
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


                /*
                SrgsRule root_rule = new SrgsRule("rootrule");
                root_rule.Add(new SrgsItem("Holly"));
                SrgsOneOf actions = new SrgsOneOf(new string[]{"turn on", "turn off"});
                root_rule.Add(actions);
                SrgsOneOf items = new SrgsOneOf(new string[]{ "the lights", "the light" });
                root_rule.Add(items);
                SrgsOneOf location = new SrgsOneOf(new string[]{ "in the bedroom", "in the study", "here"});
                root_rule.Add(new SrgsItem(0, 1, location));
                root_rule.Add(new SrgsItem("please"));
                doc.Rules.Add(root_rule);
                doc.Root = root_rule;
                 * */

                XmlWriter xmlout = XmlWriter.Create(@"C:\Users\ian\Desktop\grammar.xml");
                currentSrgsDoc.WriteSrgs(xmlout);
                xmlout.Close();
            }
            return currentSrgsDoc;
        }

        private Grammar CreateColorGrammar()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
            // Create a set of color choices.
            Choices colorChoice = new Choices(new string[] { "red", "green", "blue" });
            Grammar grammar = new Grammar(colorChoice);
            grammar.Name = "backgroundColor";
            return grammar;
        }

        public AudioInstance AddRecognizer(bool active)
        {
            SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine();
            try
            {
                //dumpSupported(recognizer);
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
                //recognizer.LoadGrammar(CreateGrammar(mTheWorld));

                // Configure the input to the recognizer.
                recognizer.SetInputToNull();

                // Attach event handlers.
                recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
                recognizer.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(recognizer_RecognizeCompleted);
                recognizer.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(recognizer_SpeechHypothesized);

                // Perform recognition of the whole file.
                //Form1.updateLog("Starting asynchronous recognition...");

                AudioInstance ai = new AudioInstance();
                ai.audioStream = null;
                ai.ID = null;
                ai.engine = recognizer;
                ai.Active = false;
                ai.NeedNewGrammar = true;
                //Console.WriteLine("Entering lock: AddRecognizer");
                lock (engines)
                {
                    engines.Add(ai);
                    if (active) ai.Active = true;
                }
                //Console.WriteLine("Exitting lock: AddRecognizer");
                return ai;
            }
            catch (Exception e)
            {
                Form1.updateLog("AddStream exception: " + e.ToString(), ELogLevel.Error,
                    ELogType.Audio | ELogType.SpeechRecog);
            }
            return null;
        }

        AudioInstance getFreeAI(string newID)
        {
            AudioInstance ai_todo = null;
            //Form1.updateLog("RunRecognition: #engines=" + engines.Count + " ");
            //int i = 0;
            lock (engines)
            {
                foreach (AudioInstance ai in engines)
                {
                    //Form1.updateLog("   Engines[" + i.ToString() + "]=(" + (ai.audioStream == null ? "null" : ai.audioStream.ToString()) + ",[" + ai.engine.GetHashCode() + "])");
                    //i++;
                    if (!ai.Active)
                    {
                        //Form1.updateLog(" ###Doing Recog on " + ai.engine.GetHashCode());
                        ai_todo = ai;
                        ai_todo.Active = true;
                        break;
                    }
                }
            }
            //Console.WriteLine("Exitting lock: RunRecognition");
            if (ai_todo == null)
            {
                AudioInstance ai = AddRecognizer(true);
                //Form1.updateLog(" ###Doing Recog on new " + ai.engine.GetHashCode());
                ai_todo = ai;
            }
            if (ai_todo.NeedNewGrammar)
            {
                ai_todo.engine.UnloadAllGrammars();
                //ai_todo.engine.LoadGrammar(CreateGrammar("the world", currentSrgsDoc));
                lock (mDocs)
                {
                    foreach (string nm in mDocs.Keys)
                    {
                        ai_todo.engine.LoadGrammar(CreateGrammar(nm, mDocs[nm]));
                    }
                }
                ai_todo.NeedNewGrammar = false;
            }
            ai_todo.audioStream = new MemoryStream();
            ai_todo.ID = newID;

            return ai_todo;
        }
        public void RunRecognition(Stream s, string ID)
        {
            //save stream to file
            DateTime timestamp = DateTime.Now;
            //WavFile.SaveWav(s, 16, timestamp.ToString("HHmmssff") + ".wav");

            AudioInstance ai_todo = getFreeAI(ID);
            ai_todo.audioStream = s;
            s.Seek(0, SeekOrigin.Begin);
            ai_todo.engine.SetInputToAudioStream(s,
                            new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
            ai_todo.engine.RecognizeAsync(RecognizeMode.Single);
        }
        public void RunRecognition(string file, string ID)
        {
            //find an unused recognizer
            AudioInstance ai_todo = getFreeAI(ID);
            ai_todo.engine.SetInputToWaveFile(file);
            ai_todo.engine.RecognizeAsync(RecognizeMode.Single);
        }
        
        void recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            int senderHash = sender.GetHashCode();
            lock (engines)
            {
                foreach (AudioInstance ai in engines)
                {
                    if (ai.engine.GetHashCode() == senderHash)
                    {
                        //ai.engine.RecognizeAsyncCancel();
                        ai.engine.SetInputToNull();
                        ai.audioStream = null;
                        ai.Active = false;
                    }
                }
            }
        }
        void recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            //Form1.updateLog("Hypothesis: " + e.Result.Text + " (conf=" + e.Result.Confidence.ToString() + ")[" + sender.GetHashCode() + "]");
        }
        void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Form1.updateLog("RecogSpeech: " + e.Result.Text + " (conf=" + e.Result.Confidence.ToString() + ")[" + sender.GetHashCode() + "]",
                ELogLevel.Info, ELogType.SpeechRecog);
            //find the ai
            int senderHash = sender.GetHashCode();
            string ID = "";
            lock (engines)
            {
                foreach (AudioInstance ai in engines)
                {
                    if (ai.engine.GetHashCode() == senderHash)
                    {
                        ID = ai.ID;
                    }
                }
            }


            string path = @"C:\Users\ian\Desktop\audio\" + e.Result.Text+ "." + DateTime.Now.ToString("HHmmssff") + ".wav";
             using (Stream outputStream = new FileStream(path, FileMode.Create))
                {
                  e.Result.Audio.WriteToWaveStream(outputStream);
                  outputStream.Close();
                }

            

            OnRecognitionSuccessful(ID, e.Result);
        }

        public delegate void RecognitionSuccessfulDelegate(string ID, RecognitionResult res);
        public event RecognitionSuccessfulDelegate RecognitionSuccessful;
        void OnRecognitionSuccessful(string ID, RecognitionResult res)
        {
            if (RecognitionSuccessful != null)
            {
                RecognitionSuccessful(ID, res);
            }
        }

        void dumpSupported(SpeechRecognitionEngine rec)
        {
            RecognizerInfo info = rec.RecognizerInfo;
            string AudioFormats = "";
            foreach (SpeechAudioFormatInfo fmt in info.SupportedAudioFormats)
            {
                AudioFormats += "  EF="+fmt.EncodingFormat.ToString();
                AudioFormats += " Bps=" + fmt.AverageBytesPerSecond.ToString();
                AudioFormats += " bps=" + fmt.BitsPerSample.ToString();
                AudioFormats += " BA=" + fmt.BlockAlign.ToString();
                AudioFormats += " CC=" + fmt.ChannelCount.ToString();
                AudioFormats += " Sps=" + fmt.SamplesPerSecond.ToString() + "\n";
            }
            string AdditionalInfo = "";
            foreach (string key in info.AdditionalInfo.Keys)
            {
                AdditionalInfo += String.Format("      {0}: {1}\n", key, info.AdditionalInfo[key]);
            }
            Form1.updateLog(String.Format(
                               "Name:                 {0 }\n" +
                               "Description:          {1} \n" +
                           "SupportedAudioFormats:\n" +
                           "{2} " +
                           "Culture:              {3} \n" +
                           "AdditionalInfo:       \n" +
                           " {4}\n",
                           info.Name.ToString(),
                           info.Description.ToString(),
                           AudioFormats,
                           info.Culture.ToString(),
                           AdditionalInfo), ELogLevel.Debug, ELogType.SpeechRecog);
        }
    }
}
