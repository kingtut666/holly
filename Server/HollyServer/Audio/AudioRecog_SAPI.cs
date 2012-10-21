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
   
    public class AudioRecog_SAPI : AudioRecog

    {
        Dictionary<string, SrgsDocument> mDocs;
        List<AudioInstance> engines;

        public AudioRecog_SAPI()
        {
            engines = new List<AudioInstance>();
            //mGrammar = CreateColorGrammar();
            mDocs = new Dictionary<string, SrgsDocument>();
            AddRecognizer(false);
            caps_rules = new Dictionary<EDeviceCapabilities, SrgsRule>();
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
                        Grammar todel = null;
                        foreach(Grammar g in ai.engine.Grammars){
                            if(g.Name == name){
                                todel = g;
                                break;
                            }
                        }
                        ai.engine.UnloadGrammar(todel);
                        ai.NeedNewGrammar = false;
                    }
                    else ai.NeedNewGrammar = true;
                }
            }
        }
        void AddGrammar(string name, SrgsDocument doc)
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
                        ai.engine.LoadGrammar(CreateGrammar(name, doc));
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
        
        Grammar CreateGrammar(string name, SrgsDocument doc)
        {
            Grammar g = new Grammar(doc);
            g.Name = name;
            return g;
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

        AudioInstance AddRecognizer(bool active)
        {
            SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine(new CultureInfo("en-GB"));
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
                Dictionary<string, SrgsDocument> docs = new Dictionary<string, SrgsDocument>();
                lock (mDocs)
                {
                    foreach (string nm in mDocs.Keys)
                    {
                        docs.Add(nm, mDocs[nm]);
                    }
                }
                foreach (string nm in docs.Keys)
                {
                    ai_todo.engine.LoadGrammar(CreateGrammar(nm, docs[nm]));
                }
                ai_todo.NeedNewGrammar = false;
            }
            ai_todo.audioStream = new MemoryStream();
            ai_todo.ID = newID;

            return ai_todo;
        }
        override public void RunRecognition(Stream s, string ID)
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
        override public void RunRecognition(string file, string ID)
        {
            //find an unused recognizer
            AudioInstance ai_todo = getFreeAI(ID);
            ai_todo.engine.SetInputToWaveFile(file);
            ai_todo.engine.RecognizeAsync(RecognizeMode.Single);
        }
        
        void recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            int senderHash = sender.GetHashCode();
            string ID = "";
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
                        ID = ai.ID;
                    }
                }
            }
            OnRecognitionComplete(ID);
        }
        void recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            //Form1.updateLog("Hypothesis: " + e.Result.Text + " (conf=" + e.Result.Confidence.ToString() + ")[" + sender.GetHashCode() + "]");
        }
        void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Form1.updateLog("RecogSpeech_SAPI: " + e.Result.Text + " (conf=" + e.Result.Confidence.ToString() + ")[" + sender.GetHashCode() + "]",
                ELogLevel.Debug, ELogType.SpeechRecog);
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

            OnRecognitionSuccessful(ID, e.Result);
        }

        protected void OnRecognitionSuccessful(string ID, RecognitionResult res)
        {
            RecognitionSuccess ret = new RecognitionSuccess(this, res);
            base.OnRecognitionSuccessful(ID, ret);
        }

        public override bool AddControllable(IControllable c)
        {
            string nm = c.GetName();
            SrgsDocument srgs = c.CreateGrammarDoc_SRGS();
            if (nm != null && nm != "" && srgs != null)
            {
                AddGrammar(nm, srgs);
                return true;
            }
            return false;
        }

        public override string GetName()
        {
            return AudioRecog_SAPI.Name;
        }
        public static string Name
        {
            get
            {
                return "SAPI";
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
