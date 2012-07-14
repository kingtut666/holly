using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Speech.Recognition.SrgsGrammar;

namespace HollyServer
{

    public enum ELogLevel { Debug, Info, Warning, Error, Fatal }
    [Flags]
    public enum ELogType
    {
        None=0x0000, Net =0x0001, Audio     =0x0002, SpeechRecog=0x0004, SpeechOut=0x0008, 
        XBMC=0x0010, File=0x0020, AlarmClock=0x0040, LWRF       =0x0080
    }

    public partial class Form1 : Form
    {
        public static Form1 server;
        string s_port = "31337";
        AudioProto p;
        AudioRecog r;
        LWRF lwrf;
        static public SpeechOut Talker;
        TheWorld theWorld;
        public DeviceProtocolMappings protos;
        List<IControllable> mControllables;
        AlarmClock clk;
        Dictionary<string, Tuple<bool, bool, string>> clkActions;
        AudioForward af;

        delegate void updateLog_Callback(string txt, ELogLevel level, ELogType whom);
        public static void updateLog(string txt, ELogLevel level, ELogType whom)
        {
            if (level == ELogLevel.Debug) return;
            if (Form1.server.txtLog.InvokeRequired)
            {
                Form1.updateLog_Callback d = new updateLog_Callback(updateLog);
                Form1.server.Invoke(d, new object[] { txt, level, whom });
            }
            else
            {
                Form1.server.txtLog.AppendText(DateTime.Now.ToString("yyMMdd HH:mm:ss: [") + 
                    level.ToString()+"] " +
                    txt + "\n");
            }
        }

        public Form1()
        {
            InitializeComponent();
            
            Form1.server = this;
            r = new AudioRecog();
            r.RecognitionSuccessful += new AudioRecog.RecognitionSuccessfulDelegate(r_RecognitionSuccessful);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
            Talker = new SpeechOut();
            Talker.AddEndpoint("beast", null);
            lwrf = new LWRF();
            af = new AudioForward();
            clkActions = new Dictionary<string, Tuple<bool, bool,string>>();

            mControllables = new List<IControllable>();


            //Controllables
            theWorld = TheWorld.CreateTestWorld();
            mControllables.Add(theWorld);
            clk = new AlarmClock();
            clk.ControllableEvent += new IControllableEventDelegate(clk_ControllableEvent);
            mControllables.Add(clk);
            List<Device> devs = theWorld.ListDevicesByCaps(null, EDeviceCapabilities.Special_XBMC);
            if (devs != null)
            {
                foreach (Device d in devs)
                {
                    XBMC xbmc = new XBMC(d.ID, d.Args["port"], d.Args["user"], d.Args["pass"]);
                    xbmc.ControllableEvent += new IControllableEventDelegate(xbmc_ControllableEvent);
                    mControllables.Add(xbmc);
                    d.Instance = xbmc;
                }
            }

            

            protos = new DeviceProtocolMappings();
            protos.SetWorld(theWorld);

            List<string> done = new List<string>();
            foreach (IControllable ic in mControllables)
            {
                string nm = ic.GetName();
                if (!done.Contains(nm))
                {
                    SrgsDocument srgs = ic.CreateGrammarDoc();
                    if (nm != null && nm != "" && srgs != null) r.AddGrammar(nm, srgs);
                    done.Add(nm);
                }
            }

        }

        void xbmc_ControllableEvent(IControllable from, IControllableEventArgs e)
        {
            XBMCEventArgs xe = e as XBMCEventArgs;

            Device audio_src = null;
            //find all the AudioIn sources
            List<Device> devs = theWorld.ListDevicesByCaps(null, EDeviceCapabilities.Special_Audio_In);
            //find one which matches this message source
            foreach (Device d in devs)
            {
                if (xe.Name.StartsWith(d.ID)) audio_src = d;
            }
            if (audio_src == null)
            {
                Form1.updateLog("ERR: XBMC command couldn't find audio in", ELogLevel.Warning,
                    ELogType.XBMC | ELogType.Audio);
                return;
            }
            devs = theWorld.ListDevicesByCaps(audio_src.Parent, EDeviceCapabilities.Special_XBMC);
            if (devs == null || devs.Count < 1)
            {
                Form1.updateLog("ERR: couldn't identify XBMC endpoint", ELogLevel.Error, 
                    ELogType.XBMC);
                return;
            }
            XBMC x = devs[0].Instance as XBMC;


            if (e.Action == "play show")
            {
                x.PlayTV(xe.uniqueID, xe.season, xe.episode, false);
            }
            else if (e.Action == "resume show")
            {
                x.PlayTV(xe.uniqueID, xe.season, xe.episode, true);
            }
            else if (e.Action == "play movie")
            {
                x.PlayMovie(xe.uniqueID, false);
            }
            else if (e.Action == "resume movie")
            {
                x.PlayMovie(xe.uniqueID, true);
            }
            else if (e.Action == "stop media")
            {
                x.Stop();
            }
            else if (e.Action == "resume media")
            {
                x.Resume();
            }
            else if (e.Action == "pause media")
            {
                x.Pause();
            }
            else if (e.Action == "play me some")
            {
                x.PlayGenre(xe.uniqueID);
            }
        }

        void clk_ControllableEvent(IControllable from, IControllableEventArgs Event)
        {
            //TODO: Should only play in current location
            //TODO: Should play an alarm
            if (Event.Action == "tick")
            {
                Form1.updateLog("Clock strikes", ELogLevel.Debug, ELogType.AlarmClock);
                if(!clkActions.ContainsKey(Event.Name)){
                    Form1.updateLog("ERR: Unknown alarm: " + Event.Name, ELogLevel.Error, ELogType.AlarmClock);
                    return;
                }
                try
                {
                    List<FIFOStream> fouts = p.GetOutputStreams(null);
                    if (fouts == null || fouts.Count == 0)
                    {
                        Form1.updateLog("ERR: Couldn't find stream for remote: " + txtRemoteID.Text, 
                            ELogLevel.Error, ELogType.AlarmClock | ELogType.Audio);
                        return;
                    }
                    if (clkActions[Event.Name].Item1)
                    {
                        foreach (FIFOStream fout in fouts)
                        {
                            AudioOut.PlayWav(fout, @"C:\Users\ian\Desktop\chimes.wav");
                        }
                    }
                    if(clkActions[Event.Name].Item2) Talker.Say("", "The time is " + DateTime.Now.ToString("h m tt"));
                    if (clkActions[Event.Name].Item3!="") Talker.Say("", clkActions[Event.Name].Item3);
                }
                catch (Exception e)
                {
                    Form1.updateLog("ERR: Exception in clk_ControllableEvent: " + e.Message, 
                        ELogLevel.Error, ELogType.AlarmClock);
                    Form1.updateLog(e.StackTrace, ELogLevel.Error, ELogType.AlarmClock);
                }
            }
            else if (Event.Action == "removed")
            {
                clkActions.Remove(Event.Name);
            }
        }

        void r_RecognitionSuccessful(string ID, System.Speech.Recognition.RecognitionResult res)
        {
            Stream s = new MemoryStream();
            res.Audio.WriteToAudioStream(s);
            double avg = AudioProtoConnection.AnalyseStream(s);

            double noise = p.GetNoiseLevel(ID);

            if (res.Confidence < 0.90)
            {
                Form1.updateLog("   Confidence too low, skipping", ELogLevel.Info, ELogType.SpeechRecog);
                return;
            }
            Form1.updateLog("Recog hit: (conf=" + res.Confidence.ToString() + ",volume=" +
                avg.ToString() + ",noise="+noise.ToString()+") " + res.Text, ELogLevel.Info, ELogType.SpeechRecog);
            if (avg < noise+50)
            {
                Form1.updateLog("     Volume too low (avg="+avg.ToString()+" noise="+noise.ToString()+"), skipping", ELogLevel.Info, ELogType.SpeechRecog);
                return;
            }
            if (p != null)
            {
                p.RecogSuccessful(ID);

                //play beep
                List<FIFOStream> fouts = p.GetOutputStreams(ID);
                if (fouts == null || fouts.Count == 0)
                {
                    Form1.updateLog("ERR: Couldn't find stream for remote: " + txtRemoteID.Text, 
                        ELogLevel.Error, ELogType.SpeechRecog | ELogType.Audio);
                    return;
                }
                foreach (FIFOStream fout in fouts)
                {
                    AudioOut.PlayWav(fout, @"C:\Users\ian\Desktop\beep3.wav");
                }
            }
            foreach (IControllable c in mControllables)
            {
                if (res.Grammar.Name == c.GetName())
                    if (c.OnSpeechRecognised(ID, res)) break; //first one to action
            }
            
        }

        private void butListen_Click(object sender, EventArgs e)
        {
            updateLog("Startup, listening on port " + s_port, ELogLevel.Info, ELogType.Net);

            p = new AudioProto();
            p.Listen(s_port);
            p.NewAudioStream += new AudioProto.NewAudioStreamDelegate(p_NewAudioStream);
        }

  
        void p_NewAudioStream(Stream stream, string ID)
        {

            //Loud enough, send it
            stream.Seek(0, SeekOrigin.Begin);
            r.RunRecognition(stream, ID);
        }

        private void butStart_Click(object sender, EventArgs e)
        {
            p.Start("");
        }

        private void butStop_Click(object sender, EventArgs e)
        {
            p.Stop("");
        }


        byte[] wavheader = {
	0x52, 0x49, 0x46, 0x46, // ChunkID = "RIFF"
	0x00, 0x00, 0x00, 0x00, // Chunksize (will be overwritten later)
	0x57, 0x41, 0x56, 0x45, // Format = "WAVE"
	0x66, 0x6d, 0x74, 0x20, // Subchunk1ID = "fmt "
	0x10, 0x00, 0x00, 0x00, // Subchunk1Size = 16
	0x01, 0x00, 0x01, 0x00, // AudioFormat = 1 (linear quantization) | NumChannels = 1
	0x80, 0x3e, 0x00, 0x00, // SampleRate = 16000 Hz
	0x00, 0xfa, 0x00, 0x00, // ByteRate = SampleRate * NumChannels * BitsPerSample/8 = 64000
	0x04, 0x00, 0x20, 0x00, // BlockAlign = NumChannels * BitsPerSample/8 = 4 | BitsPerSample = 32
	0x64, 0x61, 0x74, 0x61, // Subchunk2ID = "data"
	0x00, 0x00, 0x00, 0x00, // Subchunk2Size = NumSamples * NumChannels * BitsPerSample / 8 (will be overwritten later)
};
        private void butCreate_Click(object sender, EventArgs e)
        {
            uint samples = 0;
            BinaryWriter wav = new BinaryWriter(File.OpenWrite("c:\\Users\\ian\\Desktop\\" + DateTime.Now.ToString("HHmmss") + ".wav"));
            wav.Write(wavheader);

            wav.Write(wavheader, 0, wavheader.Length);
            samples += (uint)wavheader.Length;

            wav.Seek(4, SeekOrigin.Begin);
            wav.Write((uint)(samples + 36));
            wav.Seek(40, SeekOrigin.Begin);
            wav.Write((uint)samples);
            wav.Close();
        }

        private void butRecogWav_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.InitialDirectory = "C:\\users\\ian\\desktop\\";
            DialogResult res = dlg.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                r.RunRecognition(dlg.FileName, "WAV");
            }
        }

        private void butLWRF_Click(object sender, EventArgs e)
        {
            bool onState = chkLWRFOn.Checked;
            int dev, room;
            if (!Int32.TryParse(txtLWRFDevice.Text, out dev))
            {
                Form1.updateLog("ERR: Invalid LWRF device: " + txtLWRFDevice.Text, ELogLevel.Error, 
                    ELogType.LWRF);
                dev = 0;
            }
            if (!Int32.TryParse(txtLWRFRoom.Text, out room))
            {
                Form1.updateLog("ERR: Invalid LWRF device: " + txtLWRFRoom.Text, ELogLevel.Error,
                    ELogType.LWRF);
                room = 0;
            }
            lwrf.DeviceOnOff(room, dev, onState);
        }

        private void butSay_Click(object sender, EventArgs e)
        {
            Talker.Say("beast", txtSpeech.Text);
        }

        public bool StartOnConnect()
        {
            return chkStartOnConnect.Checked;
        }

        private void butSayRemote_Click(object sender, EventArgs e)
        {
            Talker.Say(txtRemoteID.Text, txtSpeech.Text);
        }

        private void butPlayWav_Click(object sender, EventArgs e)
        {
            List<FIFOStream> fouts = p.GetOutputStreams(txtRemoteID.Text);
            if (fouts == null || fouts.Count == 0)
            {
                Form1.updateLog("ERR: Couldn't find stream for remote: " + txtRemoteID.Text, 
                    ELogLevel.Error, ELogType.Audio);
                return;
            }
            foreach (FIFOStream fout in fouts)
            {
                AudioOut.PlayWav(fout, @"C:\Users\ian\Desktop\chimes.wav");
            }
        }


        private void butAlarmRecur_Click(object sender, EventArgs e)
        {
            TimeSpan recur = new TimeSpan(0,0,0);
            if (txtAlarmRecur.Text != "")
            {
                int t;
                if (Int32.TryParse(txtAlarmRecur.Text, out t))
                {
                    recur = new TimeSpan(0, 0, t);
                }
            }
            DateTime when;
            if (radioAlarmS.Checked)
            {
                int s;
                if (!Int32.TryParse(txtAlarmTime.Text, out s)) s = 10;
                when = DateTime.Now.AddSeconds(s);
            }
            else
            {
                try
                {
                    when = DateTime.Parse(txtAlarmTime.Text);
                }
                catch (Exception ex)
                {
                    Form1.updateLog("Couldn't parse time: " + txtAlarmTime.Text, ELogLevel.Error, 
                        ELogType.AlarmClock);
                    when = DateTime.Now.AddSeconds(10);
                }
            }

            if (clkActions.ContainsKey(txtAlarmID.Text))
            {
                Form1.updateLog("ERR: Cannot add alarm - non-unique ID", ELogLevel.Error, 
                    ELogType.AlarmClock);
            }
            else
            {
                clkActions.Add(txtAlarmID.Text, new Tuple<bool, bool, string>(chkAlarmBeep.Checked, 
                    chkAlarmSayTime.Checked, txtAlarmSpeak.Text));
                clk.CreateAlarm(txtAlarmID.Text, when, recur);
            }
        }

        private void butAudioForwardRec_Click(object sender, EventArgs e)
        {
            af.Run();
        }

        private void butAudioForwardStop_Click(object sender, EventArgs e)
        {
            af.Stop();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            XBMC xbmc = new XBMC("127.0.0.1", "8080", "xbmc", "test");
            xbmc.ListAudioGenres();
        }


    }
}
