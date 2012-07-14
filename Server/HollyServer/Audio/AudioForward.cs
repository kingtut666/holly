using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio;
using NAudio.Wave;

namespace HollyServer
{
    public class AudioForward
    {
        WaveIn waveInStream;
        WaveFileWriter writer;

        public AudioForward()
        {
            // WaveIn Streams for recording


            waveInStream = new WaveIn();
            waveInStream.WaveFormat = new WaveFormat();
            writer = new WaveFileWriter(@"C:\Users\ian\Desktop\audio\wavestream.wav", waveInStream.WaveFormat);

            waveInStream.DataAvailable += new EventHandler<WaveInEventArgs>(waveInStream_DataAvailable);

        }

        void waveInStream_DataAvailable(object sender, WaveInEventArgs e)
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
        }

        public void Run()
        {
            waveInStream.StartRecording();
        }

        public void Stop()
        {
            waveInStream.StopRecording();
            waveInStream.Dispose();
            waveInStream = null;
            writer.Close();
            writer = null;
        }

    }
}
