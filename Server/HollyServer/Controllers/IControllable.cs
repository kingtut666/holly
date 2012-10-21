using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Speech.Recognition.SrgsGrammar;
using System.Speech.Recognition;

namespace HollyServer
{
    public delegate void IControllableEventDelegate(object sender, IControllableEventArgs e);
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
        SrgsDocument CreateGrammarDoc_SRGS();
        CMUSphinx_GrammarDict CreateGrammarDoc_JSGF();
        CMUSphinx_GrammarDict CreateGrammarDoc_FSG();

        bool OnSpeechRecognised(string ID, RecognitionSuccess result);

        event IControllableEventDelegate ControllableEvent;
    }
}
