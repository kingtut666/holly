using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Speech.Recognition.SrgsGrammar;
using System.Globalization;

namespace HollyServer
{
    public class AlarmClock : IControllable
    {
        
        public AlarmClock()
        {
            timers = new Dictionary<string, Tuple<Timer, bool>>();
        }
        Dictionary<string, Tuple<Timer,bool>> timers;
        static TimeSpan zero = new TimeSpan(0);
        public void CreateAlarm(DateTime dt){
            CreateAlarm(dt.ToString("YYMMddhhmmss"), dt, zero);
        }
        public void CreateAlarm(string ID, DateTime dt, TimeSpan recurring) //TODO: Recurring
        {
            Form1.updateLog("Creating timer for: " + dt.ToShortTimeString(), ELogLevel.Info,
                ELogType.AlarmClock);
            TimeSpan ts = dt - DateTime.Now;
            bool isRecurring = (recurring!=null && recurring.CompareTo(zero)!=0);
            Timer t = new Timer(new TimerCallback(Tick), ID, ts, recurring);
            lock(timers){
                timers.Add(ID, new Tuple<Timer, bool>(t, isRecurring));
            }
        }

        string lastAlarm = null;
        void Tick(object state)
        {
            string ID = state as string;
            bool match = false;
            bool removed = false;
            lock (timers)
            {
                if (timers.ContainsKey(ID))
                {
                    match = true;
                    if (!timers[ID].Item2)
                    { //!recurring
                        timers.Remove(ID);
                        removed = true;
                    }
                    else
                        lastAlarm = ID;
                }
            }
            if(match) OnControllableEvent(this, new IControllableEventArgs(ID, "tick"));
            if(match && removed) OnControllableEvent(this, new IControllableEventArgs(ID, "removed"));
        }

        public string GetName()
        {
            return "Alarm Clock";
        }

        SrgsDocument currentSrgsDoc = null;
        public System.Speech.Recognition.SrgsGrammar.SrgsDocument CreateGrammarDoc()
        {
            if (currentSrgsDoc != null) return currentSrgsDoc;

            SrgsDocument doc = new SrgsDocument();
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");

            SrgsOneOf is_awake = new SrgsOneOf(new string[] { "i'm awake", "turn off the alarm", "i am awake"});

            SrgsRule root_rule = new SrgsRule("rootrule");
            root_rule.Add(new SrgsItem("Holly"));
            SrgsItem cmd = new SrgsItem(is_awake);
            cmd.Add(new SrgsNameValueTag("command", "turn off alarm"));
            root_rule.Add(cmd);
            root_rule.Add(new SrgsItem("please"));
            doc.Rules.Add(root_rule);
            doc.Root = root_rule;

            currentSrgsDoc = doc;
            return currentSrgsDoc;
        }

        public bool OnSpeechRecognised(string ID, System.Speech.Recognition.RecognitionResult result)
        {
            string matched = "";
            string cmd = "";
            if (result.Semantics.ContainsKey("command")) cmd = result.Semantics["command"].Value.ToString();

            if (cmd == "turn off alarm")
            {
                if (lastAlarm == null) return false;
                lock (timers)
                {
                    if (timers.ContainsKey(lastAlarm))
                    {
                        timers[lastAlarm].Item1.Dispose();
                        timers.Remove(lastAlarm);
                        matched = lastAlarm;
                        lastAlarm = null;
                    }
                }
            }
            if (matched != "")
            {
                OnControllableEvent(this, new IControllableEventArgs(matched, "removed"));
                return true;
            }
            return false;
        }

        public event IControllableEventDelegate ControllableEvent;
        void OnControllableEvent(IControllable from, IControllableEventArgs Event)
        {
            if (ControllableEvent != null)
            {
                ControllableEvent(from, Event);
            }
        }

    }
}
