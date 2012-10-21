using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Speech.Recognition;
using System.Speech.AudioFormat;
using System.Reflection;
using System.IO;
using KingTutUtils;

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

    public class RecognitionSuccess
    {
        //TODO: Abstract out Audio as well
        public RecognitionSuccess()
        {
            Semantics = new Dictionary<string, string>();
            Confidence = 1.0F;
            WordConfidence = new Dictionary<int, Tuple<string, float>>();
            Text = "";
            Audio = null;
            GrammarName = "";
            EngineName = "";
            Engine = null;
        }
        public RecognitionSuccess(AudioRecog r, RecognitionResult rres)
        {
            EngineName = "SAPI";
            Engine = r;
            Semantics = new Dictionary<string, string>();
            if (rres.Semantics != null)
            {
                foreach (KeyValuePair<String, SemanticValue> s in rres.Semantics)
                {
                    Semantics.Add(s.Key, s.Value.Value.ToString()); //need the ToString() as this may be an int etc
                }
            }
            Audio = rres.Audio;
            Text = rres.Text;
            GrammarName = rres.Grammar.Name;
            Confidence = rres.Confidence;
            WordConfidence = new Dictionary<int, Tuple<string, float>>();
            int i = 0;
            foreach (System.Speech.Recognition.RecognizedWordUnit wd in rres.Words)
            {
                WordConfidence.Add(i, new Tuple<string, float>(wd.Text, wd.Confidence));
                i++;
            }
        }


        public AudioRecog Engine;

        //Confidence of each word in the match
        public Dictionary<int, Tuple<string, float>> WordConfidence;

        //Confidence 0-1
        public float Confidence;

        //Matched Audio
        public RecognizedAudio Audio;
        Stream mStream = null;
        public void SetAudioStream(Stream s)
        {
            mStream = s;
        }
        public void WriteToWaveStream(Stream s)
        {
            if (Audio != null)
            {
                Audio.WriteToWaveStream(s);
                return;
            }
            if (mStream != null)
            {
                WavUtils.SaveWav(mStream, 16, s);
            }
        }

        //matched string
        string mText;
        public string Text
        {
            get { return mText; }
            set { mText = value.ToUpper(); }
        }

        //Grammar on which match occurred
        public string GrammarName;

        public string EngineName;

        //semantics
        Dictionary<string, string> Semantics;
        public int getSemanticValueAsInt(string key)
        {
            if (!Semantics.ContainsKey(key)) return -1;
            int ret;
            if (!Int32.TryParse(Semantics[key], out ret)) return -1;
            return ret;
        }
        public string getSemanticValuesAsString(string key)
        {
            if (!Semantics.ContainsKey(key)) return null;
            return Semantics[key];
        }
    }



    public class AudioRecogHolder
    {
        class RecognitionAttempt_Engine
        {
            public AudioRecog Engine;
            public DateTime TimeStarted;
            public DateTime TimeRecog;
            public DateTime TimeComplete;
            public RecognitionSuccess Success;
            public bool isComplete; 
            public bool isRunning;

            public RecognitionAttempt_Engine(AudioRecog eng)
            {
                Engine = eng;
                isRunning = false;
                isComplete = false;
                Success = null;
            }
            public void RunRecognition(Stream s, string ID)
            {
                try
                {
                    TimeStarted = DateTime.Now;
                    isRunning = true;
                    Engine.RunRecognition(s, ID);
                }
                catch (Exception e)
                {
                    Form1.updateLog("ERR: RecognitionAttempt_Engine.RunRecog(stream): " + e.Message, ELogLevel.Warning, ELogType.SpeechRecog);
                }
            }
            public void RunRecognition(string file, string ID)
            {
                try {
                TimeStarted = DateTime.Now;
                isRunning = true;
                Engine.RunRecognition(file, ID);
                                }
                catch (Exception e)
                {
                    Form1.updateLog("ERR: RecognitionAttempt_Engine.RunRecog(file): " + e.Message, ELogLevel.Warning, ELogType.SpeechRecog);
                }
            }
        }

        class RecognitionAttempt
        {
            public string ID;
            public Dictionary<AudioRecog, RecognitionAttempt_Engine> AttemptByEngine; //engine.name, rae
            public bool isComplete
            {
                get
                {
                    foreach (RecognitionAttempt_Engine e in AttemptByEngine.Values)
                    {
                        if (e.isComplete == false) return false;
                    }
                    return true;
                }
            }
            public bool WasSuccessful
            {
                get
                {
                    if (Result == null) return false;
                    return true;
                }
            }
            RecognitionSuccess best = null;
            public RecognitionSuccess Result
            {
                //TODO: Abstract this out, and make it runtime configurable
                get
                {
                    if (!isComplete) return null;
                    if (best != null) return best;
                    AudioRecog cmu = null;
                    AudioRecog sapi = null;
                    foreach (AudioRecog a in AttemptByEngine.Keys)
                    {
                        if (a.GetName() == AudioRecog_SAPI.Name) sapi = a;
                        else if (a.GetName() == AudioRecog_CMUSphinx.Name) cmu = a;
                        else
                        {
                            Form1.updateLog("WARN: Unknown AudioRecof in RecognitionAttempt.Result:" + a.GetName(),
                                ELogLevel.Warning, ELogType.SpeechRecog);
                        }
                    }
                    RecognitionSuccess cmuS = AttemptByEngine[cmu].Success;
                    RecognitionSuccess sapiS = AttemptByEngine[sapi].Success;

                    if (sapiS == null && cmuS == null)
                    {
                        Form1.updateLog("Speech recog failed", ELogLevel.Debug, ELogType.SpeechRecog);
                        return null;
                    }

                    if (cmuS == null) Form1.updateLog("Sphinx: Failed", ELogLevel.Info, ELogType.SpeechRecog);
                    else Form1.updateLog("Sphinx: " + cmuS.Text + " (conf=" + cmuS.Confidence + ")",
                        ELogLevel.Info, ELogType.SpeechRecog);
                    TimeSpan cmuRuntimeTS = (AttemptByEngine[cmu].TimeComplete - AttemptByEngine[cmu].TimeStarted);
                    string cmuRuntime = cmuRuntimeTS.ToString(@"s\.fff");
                    string cmuStart = AttemptByEngine[cmu].TimeStarted.ToString("HH:mm:ss.fff");
                    string cmuEnd = AttemptByEngine[cmu].TimeComplete.ToString("HH:mm:ss.fff");
                    string cmuRecog = AttemptByEngine[cmu].TimeRecog.ToString("HH:mm:ss.fff");
                    string cmuRecogtime = (AttemptByEngine[cmu].TimeRecog - AttemptByEngine[cmu].TimeStarted).ToString(@"s\.fff");
                    Form1.updateLog("  Runtime " + cmuRuntime + "s (Start: " + cmuStart + " End: " + cmuEnd + ")",
                        ELogLevel.Debug, ELogType.SpeechRecog);
                    if (cmuS != null)
                    {
                        Form1.updateLog("  Recognition took " + cmuRecogtime +
                        "s (Start: " + cmuStart +
                        " Recog: " + cmuRecog + ")",
                        ELogLevel.Debug, ELogType.SpeechRecog);
                    }


                    if (sapiS == null) Form1.updateLog("SAPI:   Failed", ELogLevel.Info, ELogType.SpeechRecog);
                    else Form1.updateLog("SAPI:   " + sapiS.Text + " (conf=" + sapiS.Confidence + ")",
                        ELogLevel.Info, ELogType.SpeechRecog);
                    Form1.updateLog("  Runtime " +
                        (AttemptByEngine[sapi].TimeComplete - AttemptByEngine[sapi].TimeStarted).ToString(@"s\.fff") +
                        "s (Start: " + AttemptByEngine[sapi].TimeStarted.ToString("HH:mm:ss.fff") +
                        " End: " + AttemptByEngine[sapi].TimeComplete.ToString("HH:mm:ss.fff") + ")",
                        ELogLevel.Debug, ELogType.SpeechRecog);
                    if (sapiS != null)
                    {
                        Form1.updateLog("  Recognition took " +
                        (AttemptByEngine[sapi].TimeRecog - AttemptByEngine[sapi].TimeStarted).ToString(@"s\.fff") +
                        "s (Start: " + AttemptByEngine[sapi].TimeStarted.ToString("HH:mm:ss.fff") +
                        " Recog: " + AttemptByEngine[sapi].TimeRecog.ToString("HH:mm:ss.fff") + ")",
                        ELogLevel.Debug, ELogType.SpeechRecog);
                    }

                    //if SAPI and !Sphinx, SAPI false +ve unless SAPI is >0.9
                    RecognitionSuccess ret;
                    if (sapiS != null && cmuS == null)
                    {
                        if (sapiS.Confidence < 0.9)
                        {
                            Form1.updateLog("   -> False Positive by SAPI", ELogLevel.Info, ELogType.SpeechRecog);
                            ret = null;
                        }
                        else
                        {
                            Form1.updateLog("   -> false Negative by Sphinx, maybe, but we trust Sphinx", ELogLevel.Info, ELogType.SpeechRecog);
                            best = sapiS;
                            best.Confidence = 0.50f;
                            ret = null;
                        }
                    }
                    //if !SAPI and Sphinx, SAPI false -ve unless Sphinx is >-5000
                    else if (sapiS == null && cmuS != null)
                    {
                        if (cmuS.Confidence < -5000)
                        {
                            Form1.updateLog("   -> False Positive by Sphinx", ELogLevel.Info, ELogType.SpeechRecog);
                            ret = null;
                        }
                        else
                        {
                            Form1.updateLog("   -> false Negative by SAPI", ELogLevel.Info, ELogType.SpeechRecog);
                            best = cmuS;
                            //normalize Confidence - this should actually do some math, but Spinx confidence is pretty broken
                            best.Confidence = 0.80f;
                            ret = best;
                        }
                    }
                    else
                    {
                        //if SAPI and Sphinx, buf different text, probable real, but use SAPI words
                        if (cmuS.Text != sapiS.Text)
                        {
                            Form1.updateLog("   -> Different text. Picking SAPI", ELogLevel.Info, ELogType.SpeechRecog);
                            best = sapiS;
                            ret = best;
                        }
                        //if SAPI and Sphinx, same text, go SAPI but bump confidence to 0.95
                        else
                        {
                            Form1.updateLog("   -> Woot, good match", ELogLevel.Info, ELogType.SpeechRecog);
                            best = sapiS;
                            best.Confidence = 0.95f;
                            ret = best;
                        }
                    }
                    Form1.updateLog("---------------------------", ELogLevel.Info, ELogType.SpeechRecog);
                    //clean up result contents
                    if (ret != null)
                    {
                        ret.SetAudioStream(fullStream);
                    }

                    return ret;
                }
            }
            Stream fullStream = null;

            public RecognitionAttempt(string id, List<AudioRecog> engines)
            {
                ID = id;
                AttemptByEngine = new Dictionary<AudioRecog,RecognitionAttempt_Engine>();
                foreach (AudioRecog r in engines)
                {
                    AttemptByEngine.Add(r, new RecognitionAttempt_Engine(r));
                }
            }
            public void RunRecognition(Stream s)
            {
                fullStream = s;
                foreach (RecognitionAttempt_Engine e in AttemptByEngine.Values)
                {
                    e.RunRecognition(s, ID);
                }
            }
            public void RunRecognition(string file)
            {
                foreach (RecognitionAttempt_Engine e in AttemptByEngine.Values)
                {
                    e.RunRecognition(file, ID);
                }
            }
            public void Completed(AudioRecog engine)
            {
                if (!AttemptByEngine.ContainsKey(engine))
                {
                    Form1.updateLog("ERR: Complete for unknown engine", ELogLevel.Error, ELogType.SpeechRecog);
                    return;
                }
                AttemptByEngine[engine].TimeComplete = DateTime.Now;
                AttemptByEngine[engine].isComplete = true;
                AttemptByEngine[engine].isRunning = false;
            }
            public void Recognised(AudioRecog engine, RecognitionSuccess success)
            {
                if (!AttemptByEngine.ContainsKey(engine))
                {
                    Form1.updateLog("ERR: Complete for unknown engine", ELogLevel.Error, ELogType.SpeechRecog);
                    return;
                }
                AttemptByEngine[engine].TimeRecog = DateTime.Now;
                AttemptByEngine[engine].Success = success;
            }

        
        }

        static List<AudioRecog> RecogEngines = null;
        Dictionary<string, RecognitionAttempt> attempts;
        int running = 0;

        public AudioRecogHolder()
        {
            if (RecogEngines == null) RecogEngines = new List<AudioRecog>();
            attempts = new Dictionary<string, RecognitionAttempt>();
            mControllables = new HashSet<string>();
        }
        public bool RegisterRecogEngine(AudioRecog rec)
        {
            RecogEngines.Add(rec);
            rec.RecognitionSuccessful += new AudioRecog.RecognitionSuccessfulDelegate(rec_RecognitionSuccessful);
            rec.RecognitionComplete += new AudioRecog.RecognitionCompleteDelegate(rec_RecognitionComplete);
            return true;
        }
        HashSet<string> mControllables;
        public bool AddControllable(IControllable c)
        {
            if (!mControllables.Add(c.GetName())) return true; //duplicate
            foreach (AudioRecog r in RecogEngines)
            {
                if (!r.AddControllable(c))
                    return false;
            }
            return true;
        }
        public void RunRecognition(Stream s, string ID)
        {
            //Currently, Sphinx can only handle one at a time. So if a recog is running, bin this one.
            if (running > 0) return;


            try
            {
                RecognitionAttempt a = new RecognitionAttempt(ID, RecogEngines);
                attempts.Add(ID, a);
                a.RunRecognition(s);
            }
            catch (Exception e)
            {
                Form1.updateLog("ERR: AudioRecogHolder.RunRecog(file): " + e.Message, ELogLevel.Warning, ELogType.SpeechRecog);
            }
        }
        public void RunRecognition(string file, string ID)
        {
            //Currently, Sphinx can only handle one at a time. So if a recog is running, bin this one.
            if (running > 0) return;
            running++;

            try {
            RecognitionAttempt a = new RecognitionAttempt(ID, RecogEngines);
            attempts.Add(ID, a);
            a.RunRecognition(file);
                                            }
                catch (Exception e)
                {
                    Form1.updateLog("ERR: AudioRecogHolder.RunRecog(file): " + e.Message, ELogLevel.Warning, ELogType.SpeechRecog);
                }
        }

        void rec_RecognitionComplete(object sender, RecognitionCompleteEventArgs e)
        {
            if (!attempts.ContainsKey(e.ID))
            {
                Form1.updateLog("ERR: Received complete for non-existent query: " + e.ID, 
                    ELogLevel.Error, ELogType.SpeechRecog);
                return;
            }
            attempts[e.ID].Completed(e.engine);
            if (attempts[e.ID].isComplete)
            {
                RecognitionAttempt a = attempts[e.ID];
                attempts.Remove(e.ID);
                running--;
                if (a.Result != null) OnRecognitionSuccessful(a.Result.Engine, e.ID, a.Result);
            }
        }
        void rec_RecognitionSuccessful(object sender, RecognitionSuccessfulEventArgs e)
        {
            if (!attempts.ContainsKey(e.ID))
            {
                Form1.updateLog("ERR: Received recognition for non-existent query: " + e.ID,
                    ELogLevel.Error, ELogType.SpeechRecog);
                return;
            }
            attempts[e.ID].Recognised(e.engine, e.res);
        }

        public delegate void RecognitionSuccessfulDelegate(object sender, RecognitionSuccessfulEventArgs e);
        public event RecognitionSuccessfulDelegate RecognitionSuccessful;
        protected void OnRecognitionSuccessful(AudioRecog engine, string ID, RecognitionSuccess res)
        {
            if (RecognitionSuccessful != null)
            {
                RecognitionSuccessful(this, new RecognitionSuccessfulEventArgs(engine, ID, res));
            }
        }

    }

    abstract public class AudioRecog
    {
        abstract public string GetName();
        abstract public void RunRecognition(Stream s, string ID);
        abstract public void RunRecognition(string file, string ID);

        abstract public bool AddControllable(IControllable c);

        public delegate void RecognitionCompleteDelegate(object sender, RecognitionCompleteEventArgs e);
        public event RecognitionCompleteDelegate RecognitionComplete;
        protected void OnRecognitionComplete(string ID)
        {
            if (RecognitionComplete != null)
            {
                RecognitionComplete(this, new RecognitionCompleteEventArgs(this, ID));
            }
        }

        public delegate void RecognitionSuccessfulDelegate(object sender, RecognitionSuccessfulEventArgs e);
        public event RecognitionSuccessfulDelegate RecognitionSuccessful;
        protected void OnRecognitionSuccessful(string ID, RecognitionSuccess res)
        {
            if (RecognitionSuccessful != null)
            {
                RecognitionSuccessful(this, new RecognitionSuccessfulEventArgs(this, ID, res));
            }
        }

    }
}
