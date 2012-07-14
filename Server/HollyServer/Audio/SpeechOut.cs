using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Speech.Synthesis;
using System.IO;

namespace HollyServer
{
    public class SpeechOut
    {
        Dictionary<string, SpeechSynthesizer> voices;
        Dictionary<string, Stream> outStreams;
        public SpeechOut()
        {
            voices = new Dictionary<string, SpeechSynthesizer>();
            outStreams = new Dictionary<string, Stream>();
        }
        public void AddEndpoint(string ID, Stream outstream)
        {
            SpeechSynthesizer voice = new SpeechSynthesizer();
            if (outstream == null) voice.SetOutputToDefaultAudioDevice();
            else voice.SetOutputToAudioStream(outstream, new System.Speech.AudioFormat.SpeechAudioFormatInfo(
                16000, System.Speech.AudioFormat.AudioBitsPerSample.Sixteen, System.Speech.AudioFormat.AudioChannel.Mono));
            //if (chkIVONA.Checked) voice.SelectVoice("IVONA 2 Amy");
            //else voice.SelectVoice("Microsoft Anna")
            voices.Add(ID, voice);
            outStreams.Add(ID, outstream);
        }
        public void RemoveEndpoint(string ID)
        {
            voices.Remove(ID);
            outStreams.Remove(ID);
        }
       
        public void Say(string ID, string msg)
        {
            //voice.Speak(text, SpeechVoiceSpeakFlags.SVSFlagsAsync);
            //voice.WaitUntilDone(10000);
            if (ID == "")
            {
                foreach (string key in voices.Keys)
                {
                    Say(key, msg);
                }
            }
            else
            {
                if (!voices.ContainsKey(ID))
                {
                    Form1.updateLog("ERR: Tried to speak to non-existent voice " + ID, ELogLevel.Error,
                        ELogType.SpeechOut);
                    return;
                }
                SpeechSynthesizer voice = voices[ID];
                voice.Rate = -1;
                voice.Volume = 100;
                PromptBuilder pb = new PromptBuilder();
                pb.AppendTextWithHint(msg, SayAs.Text);
                if (outStreams[ID] != null && (outStreams[ID] is FIFOStream))
                {
                    while (((FIFOStream)outStreams[ID]).GetWriteLock(this) == null) ; //TODO: Sleep or something
                }
                voice.Speak(pb);
                if (outStreams[ID] != null && (outStreams[ID] is FIFOStream))
                {
                    ((FIFOStream)outStreams[ID]).ReturnWriteLock(this);
                }
            }
        }

        public string ListInstalledVoices()
        {
            SpeechSynthesizer voice = new SpeechSynthesizer();
            string ret = (" Installed Voices - Name, Culture, Age, Gender, Description, ID, Enabled\n");
            foreach (InstalledVoice vx in voice.GetInstalledVoices())
            {
                VoiceInfo vi = vx.VoiceInfo;
                ret += ("   " + vi.Name + ", " +
                    vi.Culture + ", " +
                    vi.Age + ", " +
                    vi.Gender + ", " +
                    vi.Description + ", " +
                    vi.Id + ", " +
                    vx.Enabled+"\n");
            }
            return ret;
        }



    }
}
