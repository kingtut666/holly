using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Timers;
using KingTutUtils;
using System.Threading;

namespace HollyServer
{
    public class AudioProtoConnection : IDisposable
    {
        const int buffer_length = 6; //seconds
        const int buffers_per_s = 2;
        const int bitrate = 16000;
        const int noise_window_size = 3; //seconds

        TcpClient mClnt;
        NetworkStream mStream;
        
        FIFOStream mAudioOut;
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

        public bool Startable = false;
        public bool Started = false;

        WavUtils wav_out;

        string mEndpoint;

        //Queue of data blocks
        Queue<byte[]> blockQueue = new Queue<byte[]>();
        Queue<Tuple<double, int>> avgQueue = new Queue<Tuple<double, int>>();
        double runningAverage = 0;
        int runningNSamples = 0;

        Thread readThread;
        

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
            mAudioOut = new FIFOStream(chunkSize);
            streamsLock = new Object();
            CreateAudioBuffers();
            tick = new System.Timers.Timer();
            tick.Elapsed += new ElapsedEventHandler(tick_Elapsed);
            mEndpoint = mClnt.Client.RemoteEndPoint.ToString();
            mAudioOut.DataAvailable += new FIFOStream.DataAvailableDelegate(mAudioOut_DataAvailable);

            readThread = new Thread(new ThreadStart(NetReader_ThreadProc));
            readThread.Start();
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (mAudioOut != null) mAudioOut.Dispose();
                if (tick != null) tick.Dispose();
                if (wav_out != null) wav_out.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string EndPoint { get { return mEndpoint; } }
        public FIFOStream AudioOut { get { return mAudioOut; } }

  
        #region Receive and handle
        void NetReader_ThreadProc()
        {
            byte[] netHead = new byte[4];
            int netHeadOffset = 0;
            byte[] netBody = new byte[1024];
            int netBodyOffset = 0;
            while (true)
            {
                //header
                while (netHeadOffset < 4)
                {
                    int i = mStream.Read(netHead, netHeadOffset, (int)(4 - netHeadOffset));
                    netHeadOffset += i;
                }
                netHeadOffset = 0;
                netBodyOffset = 0;

                int dataLen = BitConverter.ToInt32(netHead, 0);
                dataLen = System.Net.IPAddress.NetworkToHostOrder(dataLen);

                if (netBody.Length < dataLen) netBody = new byte[dataLen];

                //body
                while (netBodyOffset < dataLen)
                {
                    int i = mStream.Read(netBody, netBodyOffset, (dataLen - netBodyOffset));
                    netBodyOffset += i;
                }

                //handle
                HandleBlock(netBody, dataLen);
            }
            

            
        }

        void HandleBlock(byte[] netBuffer, int netBuffer_len)
        {
            try
            {
                //Parse contents of rdBuffer
                if (isEqual(netBuffer, msg_hello, msg_hello.Length))
                {
                    SendRequest();
                }
                else if (isEqual(netBuffer, msg_ready, msg_ready.Length))
                {
                    Startable = true;
                }
                else if (isEqual(netBuffer, msg_data, msg_data.Length))
                {
                    //Form1.updateLog("Got data... (" + (rdBuffer_len - msg_data.Length).ToString() + ") bytes");

                    AddToStream(netBuffer, msg_data.Length, netBuffer_len - msg_data.Length);
                }
                else if (isEqual(netBuffer, msg_rptbaseline, msg_rptbaseline.Length))
                {
                }
                else
                {
                    Form1.updateLog("Unknown packet", ELogLevel.Warning, ELogType.Net);
                }

                //Form1.updateLog(ASCIIEncoding.ASCII.GetString(rdBuffer));
            }
            catch (Exception e)
            {
                Form1.updateLog("ERR: HandleBlock: exception " + e.ToString(), ELogLevel.Error,
                    ELogType.Audio | ELogType.Net);
            }
        }
        #endregion


        public void Start(){
            if (saveToFile)
            {
                wav_out = new WavUtils(16, @"C:\Users\ian\Desktop\audio\server-" + DateTime.Now.ToString("HHmmssff") + ".wav");
            }
            SendStart();
            StartTimer();
        }
        public void Stop(){
            SendStop();
            StopTimer();
            PurgeBuffers();
            if (saveToFile)
            {
                wav_out.Close();
                wav_out = null;
            }
        }

        #region Network Send

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
            byte[] hdr = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(msg.Length));
            byte[] buf = new byte[msg.Length + hdr.Length];
            Buffer.BlockCopy(hdr, 0, buf, 0, hdr.Length);
            Buffer.BlockCopy(msg, 0, buf, hdr.Length, msg.Length);
            mStream.Write(buf, 0, buf.Length);
        }

        #region Send AudioOut
        void mAudioOut_DataAvailable(object sender, EventArgs e)
        {
            while (mAudioOut.QueueLength > 0)
            {
                WriteAudioOut();
            }
        }
        void WriteAudioOut()
        {
            byte[] chunk = mAudioOut.ReadChunk();
            SendAudioData(chunk, chunkSize);
        }
        void SendAudioData(byte[] data, int sz)
        {
            byte[] blob = new byte[msg_audioout.Length + sz];
            Buffer.BlockCopy(msg_audioout, 0, blob, 0, msg_audioout.Length);
            Buffer.BlockCopy(data, 0, blob, msg_audioout.Length, sz);
            sendMsg(blob);
        }
        #endregion

        #endregion

        #region Network Errors etc
        public delegate void AudioConnClosedDelegate(object sender, AudioConnClosedEventArgs e);
        public event AudioConnClosedDelegate AudioConnClosed;
        void OnConnClosed()
        {
            Form1.updateLog("Connection Closed", ELogLevel.Info,
                ELogType.Audio | ELogType.Net);
            if (saveToFile)
            {
                wav_out.Close();
                wav_out = null;
            }

            mStream.Close();
            mClnt.Close();

            if (AudioConnClosed != null)
            {
                AudioConnClosed(this, new AudioConnClosedEventArgs(this));
            }
        }

        #endregion


        #region Audio Windows
        Object streamsLock;
        MemoryStream[] streams;
        int max_stream_no; //needed while we fill up the set of buffers
        int num_buffers;
        int max_buffer_length; //max length of buffer in bytes
        int new_stream_when; //number of bytes into current stream at which a new one is created
        void CreateAudioBuffers()
        {
            lock (streamsLock)
            {
                num_buffers = buffer_length * buffers_per_s;
                max_buffer_length = bitrate * buffer_length * 2; //16 bits
                new_stream_when = (bitrate * 2) / buffers_per_s;
                streams = new MemoryStream[num_buffers];
                PurgeBuffers();
            }
        }
        public delegate void AudioReadyForRecogDelegate(object sender, AudioReadyForRecogEventArgs e);
        public event AudioReadyForRecogDelegate AudioReadyForRecog;
        void OnAudioReadyForRecog(Stream s)
        {
            if (AudioReadyForRecog != null)
            {
                string ID = mEndpoint+"@"+DateTime.Now.ToString("mm.ss.fff");
                Form1.updateLog("AudioReadyForRecog: " + ID, ELogLevel.Debug, ELogType.SpeechRecog);
                AudioReadyForRecog(this, new AudioReadyForRecogEventArgs(s, ID));
            }
        }
        void AddToStream(byte[] data, int from, int count)
        {
            //convert 32 bit little endian to 16 bit little endian (drop first two packets)
            byte[] data16 = new byte[count / 2];
            for (int j16 = 0, j32 = from; j16 < count/2; j16 += 2, j32+=4)
            {
                data16[j16] = data[j32 + 2];
                data16[j16 + 1] = data[j32 + 3];
            }
            if (saveToFile)
            {
                if (wav_out != null)
                {
                    wav_out.Write(data16, 0, data16.Length);
                }
            }
            MemoryStream readyStream = null;
            lock (streamsLock)
            {
                //queue for averaging purposes
                blockQueue.Enqueue(data16);

                //fill windows
                for (int i = 0; i < max_stream_no; i++)
                {
                    streams[i].Write(data16, 0, data16.Length);

                    if (streams[i].Length >= max_buffer_length)
                    {
                        Form1.updateLog("OnAudioReadyForRecog (" + mEndpoint + "): i="+i.ToString()+
                            " cur_stream_no=" + max_stream_no.ToString() + "  num_buffers=" + num_buffers.ToString(),
                            ELogLevel.Debug, ELogType.Audio);

                        //CANNOT filter on volume vs noise here as it measures the average volume of the entire
                        //  stream/slot/window, but speech may (for short commands) only be a small part of the
                        //  window. Therefore, the correct place to do this is _after_ speech recognition, as
                        //  that way we only measure the volume of the matched speech.
                        readyStream = streams[i];
                        streams[i] = new MemoryStream();
                    }
                }
                if (max_stream_no < num_buffers && streams[max_stream_no - 1].Length >= new_stream_when)
                    max_stream_no++;
            }
            if (readyStream != null) OnAudioReadyForRecog(readyStream);


        }
        public void PurgeBuffers()
        {
            //if recog was successful, purge all buffers
            bool restartTimer = false;
            if (tick != null && tick.Enabled)
            {
                restartTimer = true;
                StopTimer();
            }
            lock (streamsLock)
            {
                max_stream_no = 1;
                for (int i = 0; i < num_buffers; i++)
                {
                    streams[i] = new MemoryStream();
                }
            }
            if(restartTimer) StartTimer();
        }

        #endregion

        #region Background Noise
        System.Timers.Timer tick;
        public static double AnalyseStream(Stream s)
        {
            if (!s.CanSeek)
            {
                throw new Exception("Analysis can only be performed on seekable streams");
            }
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
                UnreferencedVariable.Ignore(e);
            }

            s.Seek(0, SeekOrigin.Begin);

            return d / nFrames;
        }
        public double NoiseLevel
        {
            get { return runningAverage; }
        }
        void StartTimer()
        {
            tick.Interval = noise_window_size * 1000;
            tick.Start();
        }
        void StopTimer()
        {
            tick.Stop();
        }
        void tick_Elapsed(object sender, ElapsedEventArgs e)
        {
            UnreferencedVariable.Ignore(e);

            //Form1.updateLog("Tick ("+DateTime.Now.ToString("MMss.ff")+"): cur_stream_no="+cur_stream_no.ToString()+"  num_buffers="+num_buffers.ToString());
            Queue<byte[]> blk = blockQueue;
            blockQueue = new Queue<byte[]>();

            //average blockQueue
            double tot = 0;
            int nFrames = 0;
            while (blk.Count > 0)
            {
                byte[] b = blk.Dequeue(); //data,from,count
                int nSamples = b.Length / 2;
                for (int j = 0; j < nSamples; j++)
                {
                    //16-bit is largest SpeechRecog can handle. Convert from 32 bit.
                    short d = BitConverter.ToInt16(b, j * 2);
                    tot += d;
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
            Tuple<double, int> newAvg = new Tuple<double, int>(tot, nFrames);
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
        #endregion

    }
}
