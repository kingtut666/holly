using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KingTutUtils;
using System.IO;
using Microsoft.Research.Kinect.Audio;
using System.Threading;

namespace HollyClient_Win
{
    class AudioIn_KinectXbox : IDisposable
    {
        FIFOStream mStream;
        KinectAudioSource mSrc = null;
        Thread mThread;
        ManualResetEvent mRunningEvent;
        bool mRunning;
        const bool mSaveAudio = true;
        WavUtils wav_out;

        public AudioIn_KinectXbox(FIFOStream stream)
        {
            mStream = stream;
             mSrc = new KinectAudioSource();
             mSrc.SystemMode = SystemMode.OptibeamArrayOnly;
             mThread = new Thread(new ThreadStart(KinectThreadProc));
             mRunningEvent = new ManualResetEvent(false);
             mRunning = false;
        }


        public void Start(){
            if(mSaveAudio){
                string path2 = @"C:\Users\ian\Desktop\audio\client-" + DateTime.Now.ToString("HHmmssff") + ".wav";
                wav_out = new WavUtils(16, path2);
            }
            mRunningEvent.Set();
            mRunning = true;
            if(!mThread.IsAlive) mThread.Start();
            
        }
        public void Stop()
        {
            if (mSaveAudio)
            {
                wav_out.Close();
                wav_out = null;
            }
            mRunning = false;
            mRunningEvent.Reset();
        }

        void KinectThreadProc()
        {
            while (true)
            {
                byte[] buf = new byte[mStream.ChunkSize];
                byte[] buf16 = new byte[mStream.ChunkSize / 2];
                mRunningEvent.WaitOne();
                Stream s = mSrc.Start();
                
                while (mRunning)
                {
                    //Read 16-bit PCM
                    int i = s.Read(buf16, 0, mStream.ChunkSize/2);
                    if(mSaveAudio && wav_out!=null){
                        wav_out.Write(buf16, 0, mStream.ChunkSize/2);
                    }
                    
                    //need to convert to 32-bit PCM (which this doesn't)
                    for (int j16 = 0, j32=0; j16 < i; j16+=2,j32+=4)
                    {
                        buf[j32] = 0;
                        buf[j32 + 1] = 0;
                        buf[j32 + 2] = buf16[j16];
                        buf[j32 + 3] = buf16[j16+1];
                    }

                    mStream.GetWriteLock(s);
                    mStream.Write(buf, 0, i*2);
                    mStream.ReturnWriteLock(s);
                }

                mSrc.Stop();
            }

        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if(mSrc!=null) mSrc.Dispose();
                if (mRunningEvent != null) mRunningEvent.Dispose();
                if (wav_out != null) wav_out.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
