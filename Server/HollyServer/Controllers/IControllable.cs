using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Speech.Recognition.SrgsGrammar;
using System.Speech.Recognition;

namespace HollyServer
{
    public delegate void IControllableEventDelegate(IControllable from, IControllableEventArgs e);
    public class IControllableEventArgs : EventArgs
    {
        public IControllableEventArgs(string name, string action)
        {
            Name = name;
            Action = action;
        }
        public string Name;
        public string Action;
    }

    public interface IControllable
    {
        string GetName();
        SrgsDocument CreateGrammarDoc();
        bool OnSpeechRecognised(string ID, RecognitionResult result);

        event IControllableEventDelegate ControllableEvent;
    }
}
