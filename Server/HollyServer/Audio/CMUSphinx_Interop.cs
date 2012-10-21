using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;
using KingTutUtils;

namespace HollyServer
{
    public class CMUSphinx_Interop
    {
        /*
        struct RecogResult {
	        char* Text;
        };

        typedef void (__stdcall * SpeechRecognisedCallback_t)(char* result);

         __declspec( dllexport ) bool Init();
         __declspec( dllexport ) bool AddRecognizer(char* name, int arg_count, char* args[], long callback);
         __declspec( dllexport ) bool DelRecognizer(char* name);
         __declspec( dllexport ) bool Cleanup();
         * */
        [BestFitMapping(false, ThrowOnUnmappableChar = true)]
        class NativeMethods
        {
            [DllImport(@"C:\Users\ian\Downloads\cmusphinx-pocketsphinx.tar\cmusphinx-pocketsphinx\pocketsphinx\Debug\pocketsphinx_clrstub.dll",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern bool Init();

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "0"), 
            DllImport(@"C:\Users\ian\Downloads\cmusphinx-pocketsphinx.tar\cmusphinx-pocketsphinx\pocketsphinx\Debug\pocketsphinx_clrstub.dll",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern bool AddRecognizer([MarshalAs(UnmanagedType.LPStr), In] string name,
                [In] int arg_count,
                [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr), In] string[] args,
                [MarshalAs(UnmanagedType.FunctionPtr), In] AudioMatchedDelegate callback);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "0"), 
            DllImport(@"C:\Users\ian\Downloads\cmusphinx-pocketsphinx.tar\cmusphinx-pocketsphinx\pocketsphinx\Debug\pocketsphinx_clrstub.dll",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern bool DelRecognizer([MarshalAs(UnmanagedType.LPStr), In] string name);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "1"), 
            System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "0"), 
            DllImport(@"C:\Users\ian\Downloads\cmusphinx-pocketsphinx.tar\cmusphinx-pocketsphinx\pocketsphinx\Debug\pocketsphinx_clrstub.dll",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern bool RunRecognizer([MarshalAs(UnmanagedType.LPStr), In] string ID,
                [MarshalAs(UnmanagedType.LPStr), In] string wavFile);

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "0"), 
            DllImport(@"C:\Users\ian\Downloads\cmusphinx-pocketsphinx.tar\cmusphinx-pocketsphinx\pocketsphinx\Debug\pocketsphinx_clrstub.dll",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern bool RunRecognizerInMem([MarshalAs(UnmanagedType.LPStr), In] string ID,
                int sz, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1), In] byte[] data);

            [DllImport(@"C:\Users\ian\Downloads\cmusphinx-pocketsphinx.tar\cmusphinx-pocketsphinx\pocketsphinx\Debug\pocketsphinx_clrstub.dll",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern bool Cleanup();
        }

        public CMUSphinx_Interop()
        {
            NativeMethods.Init();
            workers = new List<BackgroundWorker>();
            workersLock = new object();
        }
        ~CMUSphinx_Interop()
        {
            NativeMethods.Cleanup();
        }
        //string audioDir = @"c:\users\ian\desktop\audio";
        string hmmDir = @"c:\Users\ian\Downloads\voxforge-en-0.4.tar\voxforge-en-0.4\model_parameters\voxforge_en_sphinx.cd_cont_5000";
        public bool AddGrammar(string name, string GrammarType, string Grammar, string Dict)
        {
            string dictFilename = @"D:\devel\github\holly\Server\"+name+"-dict.txt";
            string grammarFilename = @"D:\devel\github\holly\Server\" + name;
            if (GrammarType == "FSG") grammarFilename += "-fsg.txt";
            else grammarFilename += "-grammar.txt";
            //string ctlFilename = "";
            //string hypFilename = "";

            //save Grammar, Dict to file
            StreamWriter dictW = new StreamWriter(dictFilename, false);
            dictW.Write(Dict);
            dictW.Close();
            StreamWriter grammarW = new StreamWriter(grammarFilename, false);
            grammarW.Write(Grammar);
            grammarW.Close();

            //TODO: Should be able to configure some settings through UI
            string[] args = new string[]{ 
                "-hmm", hmmDir,
                "-dict", dictFilename, 
                (GrammarType=="FSG"?"-fsg" : "-jsgf"), grammarFilename, 
                "-adcin", "yes"
            };

            return NativeMethods.AddRecognizer(name, args.Length, args, AudioMatched);
        }
        public bool DelGrammar(string name)
        {
            //Note: We aren't deleting the dict and grammar files
            return NativeMethods.DelRecognizer(name);
        }
        List<BackgroundWorker> workers;
        object workersLock;
        public bool RunRecognition(Stream s, string ID, string filename)
        {
            //Save file to Wav

            BackgroundWorker b = new BackgroundWorker();
            b.DoWork += new DoWorkEventHandler(b_DoWork);
            b.RunWorkerCompleted += new RunWorkerCompletedEventHandler(b_RunWorkerCompleted);
            lock (workersLock)
            {
                workers.Add(b);
            }
            b.RunWorkerAsync(new Tuple<Stream, string, string>(s, ID, filename));


            return true;
        }

        void b_DoWork(object sender, DoWorkEventArgs e)
        {
            Stream s = ((Tuple<Stream,string, string>)e.Argument).Item1;
            string ID = ((Tuple<Stream, string, string>)e.Argument).Item2;
            string filename = ((Tuple<Stream, string, string>)e.Argument).Item3;

            MemoryStream ms;
            if (s != null)
            {
                if (s.GetType() == typeof(MemoryStream)) ms = s as MemoryStream;
                else
                {
                    ms = new MemoryStream((int)s.Length);
                    s.CopyTo(ms);
                }
            }
            else
            {
                ms = new MemoryStream();
                WavUtils.ReadWav(filename, ms);
            }

            bool ret = NativeMethods.RunRecognizerInMem(ID, (int)ms.Length, ms.GetBuffer());
            e.Result = ID;

            /*
            string f;
            if (filename == null) f = audioDir + "\\wav-" + DateTime.Now.ToString("HHmmssf") + ".wav";
            else f = filename;
            if (s != null)
            {
                WavFile.SaveWav(s, 16, filename);
            }
            bool ret = RunRecognizer(ID, filename);
            if (s != null)
            {
                File.Delete(filename);
            }
            e.Result = ID;
            */
        }

        void b_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string ID = e.Result as string;
            lock (workersLock)
            {
                workers.Remove((BackgroundWorker)sender);
                ((BackgroundWorker)sender).Dispose();
            }

            OnRecognitionComplete(ID);
        }


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void AudioMatchedDelegate(string name, string text, int score);
        AudioMatchedDelegate AudioMatched = delegate(string name, string text, int score)
        {
            CMUSphinx_Interop.OnRecognitionSuccessful(name, text, score);
        };

        public delegate void RecognitionSuccessfulDelegate(object sender, RecognitionSuccessfulEventArgs e);
        static public event RecognitionSuccessfulDelegate RecognitionSuccessful;
        static void OnRecognitionSuccessful(string name, string text, int score)
        {
            RecognitionSuccess res = new RecognitionSuccess();
            res.GrammarName = name;
            res.Text = text;
            res.Confidence = (float)score;
            if (RecognitionSuccessful != null)
            {
                RecognitionSuccessful(null, new RecognitionSuccessfulEventArgs(null, name, res));
            }
        }
        public delegate void RecognitionCompleteDelegate(object sender, RecognitionCompleteEventArgs e);
        static public event RecognitionCompleteDelegate RecognitionComplete;
        static void OnRecognitionComplete(string ID)
        {
            if (RecognitionComplete != null)
            {
                RecognitionComplete(null, new RecognitionCompleteEventArgs(null, ID));
            }
        }

    }
}
