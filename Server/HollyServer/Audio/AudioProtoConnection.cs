using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Timers;

namespace HollyServer
{
    public class AudioProtoConnection
    {
        const int buffer_length = 4; //seconds
        const int buffers_per_s = 8;



        TcpClient mClnt;
        NetworkStream mStream;
        FIFOStream mAudioOut;
        byte[] rdHdr = new byte[4];
        byte[] rdBuffer;
        int rdBuffer_len = 0;
        bool saveToFile = false;
        int chunkSize = 512;  //256 samples of 16 bits

        byte[] msg_hello = ASCIIEncoding.ASCII.GetBytes("HELLO");
        byte[] msg_ready = ASCIIEncoding.ASCII.GetBytes("RDY");
        byte[] msg_start = ASCIIEncoding.ASCII.GetBytes("START");
        byte[] msg_stop = ASCIIEncoding.ASCII.GetBytes("STOP");
        byte[] msg_getbaseline = ASCIIEncoding.ASCII.GetBytes("GETBASE");
        byte[] msg_rptbaseline = ASCIIEncoding.ASCII.GetBytes("BASELINE");
        byte[] msg_config = ASCIIEncoding.ASCII.GetBytes("CONFIG");
        byte[] msg_data = ASCIIEncoding.ASCII.GetBytes("DATA");
        byte[] msg_audioout = ASCIIEncoding.ASCII.GetBytes("AUDIOOUT");

        byte[]  wavheader = {
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


        public bool Startable = false;
        public bool Started = false;

        BinaryWriter wav;
        WavFile wav_out;
        string mEndpoint;
        Stream mStreamOut;

        //Queue of data blocks
        Queue<Tuple<byte[], int, int>> blockQueue = new Queue<Tuple<byte[], int, int>>();
        Queue<Tuple<double, int>> avgQueue = new Queue<Tuple<double, int>>();
        double runningAverage = 0;
        int runningNSamples = 0;

        bool isEqual(byte[] a, byte[] b, int len)
        {
            if (len <= 0) return true;
            for (int i = 0; i < len; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        public AudioProtoConnection(TcpClient clnt)
        {
            mClnt = clnt;
            mStream = mClnt.GetStream();
            mAudioOut = new FIFOStream(chunkSize); //TODO: implement my own stream
            ReadHeader();
            streamsLock = new Object();
            CreateAudioBuffers();
            tick = new Timer();
            tick.Elapsed += new ElapsedEventHandler(tick_Elapsed);
            mEndpoint = mClnt.Client.RemoteEndPoint.ToString();
            mAudioOut.DataAvailable += new FIFOStream.DataAvailableDelegate(mAudioOut_DataAvailable);

        }

        void mAudioOut_DataAvailable()
        {
            while (mAudioOut.QueueLength > 0)
            {
                WriteAudioOut();
            }
        }

        public string EndPoint { get { return mEndpoint; } }
        public FIFOStream AudioOut { get { return mAudioOut; } }

        int tick_ct = 0;
        void tick_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Form1.updateLog("Tick ("+DateTime.Now.ToString("MMss.ff")+"): cur_stream_no="+cur_stream_no.ToString()+"  num_buffers="+num_buffers.ToString());
            if (_remaining != 0) _remaining = 0;
            if (cur_stream_no < num_buffers) cur_stream_no++;
            //else tick.Stop();
            tick_ct++;
            if (tick_ct < (buffers_per_s * 3)) return; //each window is 3s
            tick_ct = 0;

            Queue<Tuple<byte[], int, int>> blk = blockQueue;
            blockQueue = new Queue<Tuple<byte[], int, int>>();

            //average blockQueue
            double tot = 0;
            int nFrames = 0;
            while (blk.Count > 0)
            {
                Tuple<byte[], int, int> b = blk.Dequeue(); //data,from,count
                int nSamples = b.Item3 / 4;
                for (int j = 0; j < nSamples; j++)
                {
                    //16-bit is largest SpeechRecog can handle. Convert from 32 bit.
                    int d = BitConverter.ToInt32(b.Item1, b.Item2 + j * 4);
                    d = d >> 16;
                    if (d < 0) d = 0 - d;
                    tot += (short)d;
                    nFrames++;
                }
            }


            double dd = runningAverage;
            dd = dd * runningNSamples;
            dd += tot;
            runningNSamples += nFrames;
            if (runningNSamples != 0) runningAverage = dd / runningNSamples;
            else runningAverage = 0;


            //Add to avgQueue
            Tuple<double,int> newAvg = new Tuple<double,int>(tot, nFrames);
            avgQueue.Enqueue(newAvg);

            while (avgQueue.Count > 15)   //15 * 3s = 45s
            {
                newAvg = avgQueue.Dequeue();
                dd = runningAverage * runningNSamples;
                dd -= newAvg.Item1;
                runningNSamples -= newAvg.Item2;
                if (runningNSamples != 0) runningAverage = dd / runningNSamples;
                else runningAverage = 0;

            }
        }
        void ReadHeader()
        {
            mStream.BeginRead(rdHdr, 0, 4, new AsyncCallback(DoReadHdr), this);
        }
        void ReadData(int sz)
        {
            rdBuffer = new byte[sz];
            mStream.BeginRead(rdBuffer, 0, (int)sz, new AsyncCallback(DoReadBlock), this);
        }



        void HandleBlock()
        {
            try
            {
                //Parse contents of rdBuffer
                if (isEqual(rdBuffer, msg_hello, msg_hello.Length))
                {
                    SendRequest();
                }
                else if (isEqual(rdBuffer, msg_ready, msg_ready.Length))
                {
                    Startable = true;
                }
                else if (isEqual(rdBuffer, msg_data, msg_data.Length))
                {
                    //Form1.updateLog("Got data... (" + (rdBuffer_len - msg_data.Length).ToString() + ") bytes");
                    if (saveToFile)
                    {
                        if (wav_out != null)
                        {
                            wav_out.Write(rdBuffer, msg_data.Length, rdBuffer_len - msg_data.Length);
                        }
                    }
                    AddToStream(rdBuffer, msg_data.Length, rdBuffer_len - msg_data.Length);
                }
                else if (isEqual(rdBuffer, msg_rptbaseline, msg_rptbaseline.Length))
                {
                }

                //Form1.updateLog(ASCIIEncoding.ASCII.GetString(rdBuffer));
                ReadHeader();
            }
            catch (Exception e)
            {
                Form1.updateLog("ERR: HandleBlock: exception " + e.ToString(), ELogLevel.Error,
                    ELogType.Audio | ELogType.Net);
            }
        }
        DateTime startTime;
        double _remaining;
        public void Start(){
            if (saveToFile)
            {
                wav_out = new WavFile(32, DateTime.Now.ToString("HHmmssff") + ".wav");
            }
            SendStart();
            StartTimer();
        }
        void StartTimer()
        {
            if (cur_stream_no < num_buffers)
            {
                if (_remaining == 0)
                    tick.Interval = (((double)1.0) / buffers_per_s)*1000;
                else
                    tick.Interval = _remaining;
                startTime = DateTime.Now;
                tick.Start();
            }
        }
        void Stoptimer()
        {
            tick.Stop();
            if (cur_stream_no < num_buffers)
            {
                DateTime endTime = DateTime.Now;
                double diff = endTime.Ticks - startTime.Ticks;
                diff /= 10000; //milliseconds
                //diff /= 1000; //seconds
                if (_remaining != 0)
                {
                    _remaining -= diff;
                    if (_remaining <= 0) _remaining = 0;
                }
            }
        }
        public void Stop(){
            SendStop();
            if(saveToFile) CloseWav();
        }

        void SendAudioData(byte[] data, int sz)
        {
            byte[] blob = new byte[msg_audioout.Length + sz];
            Buffer.BlockCopy(msg_audioout, 0, blob, 0, msg_audioout.Length);
            Buffer.BlockCopy(data, 0, blob, msg_audioout.Length, sz);
            sendMsg(blob);
        }
        void SendRequest()
        {
            sendMsg(msg_config);
        }
        void SendStart()
        {
            sendMsg(msg_start);
            Started = true;
        }
        void SendStop()
        {
            sendMsg(msg_stop);
            Started = false;
        }
        void SendGetBaseline()
        {
            sendMsg(msg_getbaseline);
        }
        void sendMsg(byte[] msg)
        {
            byte[] hdr = BitConverter.GetBytes(msg.Length);
            byte[] buf = new byte[msg.Length + hdr.Length];
            Buffer.BlockCopy(hdr, 0, buf, 0, hdr.Length);
            Buffer.BlockCopy(msg, 0, buf, hdr.Length, msg.Length);
            mStream.Write(buf, 0, buf.Length);
        }

        public static void DoReadHdr(IAsyncResult ar)
        {
            AudioProtoConnection p = (AudioProtoConnection)ar.AsyncState;
            try
            {
                
                int len = p.mStream.EndRead(ar);
                if (len != 4)
                {
                    p.OnConnClosed();
                    return;
                }
                int sz = BitConverter.ToInt32(p.rdHdr, 0);
                sz = System.Net.IPAddress.NetworkToHostOrder(sz);
                //Form1.updateLog("Header says sz = " + sz.ToString());
                p.ReadData(sz);
            }
            catch (Exception e)
            {
                Form1.updateLog("ERR: DoReadHdr: exception " + e.ToString(), ELogLevel.Error,
                    ELogType.Net | ELogType.Audio);
                p.OnConnClosed();
            }
        }
        public static void DoReadBlock(IAsyncResult ar)
        {
            try {
                AudioProtoConnection p = (AudioProtoConnection)ar.AsyncState;
                int len = p.mStream.EndRead(ar);
                p.rdBuffer_len = len;
                if (len == 0)
                {
                    p.OnConnClosed();
                    return;
                }
                p.HandleBlock();
            }
            catch (Exception e)
            {
                Form1.updateLog("ERR: DoReadBlock: exception " + e.ToString(), ELogLevel.Error,
                    ELogType.Net | ELogType.Audio);
            }
        }

        void WriteAudioOut()
        {
            byte[] chunk = mAudioOut.ReadChunk();

            /*
            if (len == 0 || len == 1) return; //TODO: Be cleverer
            if (len % 2 != 0)
            {
                //need full frames
                len -= 1;
            }
            mAudioOutOffset += len;
             * */
            SendAudioData(chunk, chunkSize);
        }

        
        public delegate void AudioConnClosedDelegate(AudioProtoConnection conn);
        public event AudioConnClosedDelegate AudioConnClosed;
        void OnConnClosed()
        {
            Form1.updateLog("Connection Closed", ELogLevel.Info,
                ELogType.Audio | ELogType.Net);
            if (saveToFile)
            {
                CloseWav();
            }

            mStream.Close();
            if (AudioConnClosed != null)
            {
                AudioConnClosed(this);
            }
        }
        void CloseWav()
        {
            if (wav != null)
            {
                WavFile a = wav_out;
                wav = null;
                a.Close();
            }
        }

        Object streamsLock;
        Timer tick;

        MemoryStream[] streams;
        BinaryWriter[] streamWriters;
        int cur_stream_no;
        int num_buffers;
        int max_buffer_length; //max length of buffer in bytes
        void CreateAudioBuffers()
        {
            lock (streamsLock)
            {
                num_buffers = buffer_length * buffers_per_s;
                max_buffer_length = 16000 * buffer_length * 2;
                streams = new MemoryStream[num_buffers];
                streamWriters = new BinaryWriter[num_buffers];
                for (int i = 0; i < num_buffers; i++)
                {
                    streams[i] = new MemoryStream();
                    streamWriters[i] = new BinaryWriter(streams[i]);
                }
                cur_stream_no = 0;
            }
        }
        public delegate void AudioReadyForRecogDelegate(Stream s, string ID);
        public event AudioReadyForRecogDelegate AudioReadyForRecog;
        void OnAudioReadyForRecog(Stream s)
        {
            if (AudioReadyForRecog != null)
            {
                AudioReadyForRecog(s, mEndpoint);
            }
        }
        public static double AnalyseStream(Stream s)
        {
            s.Seek(0, SeekOrigin.Begin);
            BinaryReader b = new BinaryReader(s);
            double d = 0;
            double dd = 0;
            int nFrames = 0;
            try
            {
                while (true)
                {
                    dd = b.ReadInt16();
                    if (dd < 0) dd = 0 - dd;
                    d += dd;
                    nFrames++;
                }
            }
            catch (Exception e)
            {
                //expected when stream empty
            }

            s.Seek(0, SeekOrigin.Begin);

            return d / nFrames;
        }
        
        void AddToStream(byte[] data, int from, int count)
        {
            lock (streamsLock)
            {
                blockQueue.Enqueue(new Tuple<byte[], int, int>(data, from, count));
                for (int i = 0; i < cur_stream_no; i++)
                {
                    int nSamples = count / 4;
                    for (int j = 0; j < nSamples; j++)
                    {
                        //16-bit is largest SpeechRecog can handle. Convert from 32 bit.
                        int d = BitConverter.ToInt32(data, from + j * 4);
                        d = d >> 16;
                        short ds = (short)d;
                        streamWriters[i].Write(ds);
                    }
                    if (streams[i].Length >= max_buffer_length)
                    {
                        double volume = AnalyseStream(streams[i]);

                        Form1.updateLog("OnAudioReadyForRecog (" + mEndpoint + "): i="+i.ToString()+
                            " cur_stream_no=" + cur_stream_no.ToString() + "  num_buffers=" + num_buffers.ToString(),
                            ELogLevel.Debug, ELogType.Audio);
                        Form1.updateLog("   noise="+NoiseLevel.ToString()+" volume="+volume.ToString(), 
                            ELogLevel.Debug, ELogType.Audio);

                        //CANNOT filter on volume vs noise here as it measures the average volume of the entire
                        //  stream/slot/window, but speech may (for short commands) only be a small part of the
                        //  window. Therefore, the correct place to do this is _after_ speech recognition, as
                        //  that way we only measure the volume of the matched speech.
                        /*if (volume < NoiseLevel + 50)
                        {
                            Form1.updateLog("      Audio < Noise+50 (a="+volume.ToString()+", n="+NoiseLevel.ToString()+"): Skipping", 
                                ELogLevel.Info, ELogType.Audio | ELogType.SpeechRecog);
                        }
                        else */
                            OnAudioReadyForRecog(streams[i]);
                        streams[i] = new MemoryStream();
                        streamWriters[i] = new BinaryWriter(streams[i]);
                    }
                }
            }
        }
        public void RecogSuccessful()
        {
            //if recog was successful, purge all buffers
            tick.Stop();
            _remaining = 0;
            //lock (streamsLock)
            //{
                cur_stream_no = 0;
                for (int i = 0; i < num_buffers; i++)
                {
                    streams[i] = new MemoryStream();
                    streamWriters[i] = new BinaryWriter(streams[i]);
                }
            //}
            StartTimer();
        }

        public Stream GetStreamOut()
        {
            //TODO: Sort out a stream for audio out
            if (mStreamOut == null) mStreamOut = new MemoryStream();
            return mStreamOut;
        }
        public double NoiseLevel
        {
            get { return runningAverage; }
        }

    }
}
