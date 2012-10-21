using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KingTutUtils;
using NAudio.Wave;
using System.Threading;

namespace HollyClient_Win
{
    class AudioOut : IDisposable
    {
        FIFOStream mStream;
        BufferedWaveProvider mBuffOut;
        IWavePlayer mWaveOut;
        bool mPlaying;
        Thread mPlayer;
        ManualResetEvent mPlayEvent;

        public AudioOut(FIFOStream stream)
        {
            mStream = stream;
            mStream.DataAvailable += mStream_DataAvailable;
            WaveFormat fmt = new WaveFormat(16000, 16, 1);
            mBuffOut = new BufferedWaveProvider(fmt);
            mBuffOut.DiscardOnBufferOverflow = true;
            mWaveOut = new WaveOutEvent();
            mWaveOut.Init(mBuffOut);
            mPlaying = false;

            //start player thread
            mPlayEvent = new ManualResetEvent(false); 
            mPlayer = new Thread(new ThreadStart(ThreadProc));
            mPlayer.Priority = ThreadPriority.AboveNormal;
            mPlayer.Start();
        }

        void mStream_DataAvailable(object sender, EventArgs e)
        {
            Console.WriteLine("Audio data received. Queue length = " + mStream.Length.ToString());
            int len = (int)mStream.Length;
            byte[] a = new byte[len];
            int i = mStream.Read(a, 0, len);
            mBuffOut.AddSamples(a, 0, i);
            if (mBuffOut.BufferedDuration.TotalSeconds > 1.0 && !mPlaying) mPlayEvent.Set();
            else mPlayEvent.Reset();
        }

        public void DataReceived()
        {
        }

        void ThreadProc()
        {
            while (true)
            {
                mPlayEvent.WaitOne();
                mPlaying = true;
                mWaveOut.Play();
                mWaveOut.PlaybackStopped += mWaveOut_PlaybackStopped;

            }
        }

        void mWaveOut_PlaybackStopped(object sender, EventArgs e)
        {
            mPlaying = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (mWaveOut != null) mWaveOut.Dispose();
                if (mPlayEvent != null) mPlayEvent.Dispose();
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
