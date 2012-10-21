using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HollyServer
{
    public class NewAudioStreamEventArgs : EventArgs
    {
        public NewAudioStreamEventArgs(System.IO.Stream s, string id)
        {
            ID = id;
            stream = s;
        }
        public string ID;
        public System.IO.Stream stream;
    }
    public class AudioConnClosedEventArgs : EventArgs
    {
        public AudioConnClosedEventArgs(AudioProtoConnection c)
        {
            conn = c;
        }
        public AudioProtoConnection conn;
    }
    public class AudioReadyForRecogEventArgs : EventArgs
    {
        public AudioReadyForRecogEventArgs(Stream stream, string id)
        {
            id = ID;
            s = stream;
        }
        public string ID;
        public Stream s;
    }
    public class RecognitionSuccessfulEventArgs : EventArgs
    {
        public RecognitionSuccessfulEventArgs(AudioRecog e, string id, RecognitionSuccess r)
        {
            engine = e;
            res = r;
            ID = id;
        }
        public AudioRecog engine;
        public string ID;
        public RecognitionSuccess res;
    }
    public class RecognitionCompleteEventArgs : EventArgs
    {
        public RecognitionCompleteEventArgs(AudioRecog e, string id)
        {
            engine = e;
            ID = id;
        }
        public AudioRecog engine;
        public string ID;
    }




    public class OnOffEventArgs : EventArgs
    {
        public OnOffEventArgs(int room, int device, bool state)
        {
            Room = room;
            Device = device;
            State = state;
        }
        public int Room;
        public int Device;
        public bool State;
    }
    public class AllOffEventArgs : EventArgs
    {
        public AllOffEventArgs(int room)
        {
            Room = room;
        }
        public int Room;
    }
    public class MoodEventArgs : EventArgs
    {
        public MoodEventArgs(int room, int mood)
        {
            Room = room;
            Mood = mood;
        }
        public int Room;
        public int Mood;
    }
    public class DimEventArgs : EventArgs
    {
        public DimEventArgs(int device, int pct)
        {
            Device = device;
            PCT = pct;
        }
        public int Device;
        public int PCT;
    }
    public class HeatEventArgs : EventArgs
    {
        public HeatEventArgs(int room, int state)
        {
            Room = room;
            State = state;
        }
        public int Room;
        public int State;
    }
    public class RawEventArgs : EventArgs
    {
        public RawEventArgs(string rawData)
        {
            RawData = rawData;
        }
        public string RawData;
    }
        
}
