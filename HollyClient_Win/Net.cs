using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KingTutUtils;
using System.Net.Sockets;
using System.IO;

namespace HollyClient_Win
{
    class Net : IDisposable
    {
        public enum EMSG_TYPE { HELLO, CONFIG, READY, START, GETBASE, BASELINE, AUDIOOUT, DATA, STOP, _ERR, _NONE }

        byte[] msg_hello = ASCIIEncoding.ASCII.GetBytes("HELLO");
        byte[] msg_ready = ASCIIEncoding.ASCII.GetBytes("RDY");
        byte[] msg_start = ASCIIEncoding.ASCII.GetBytes("START");
        byte[] msg_stop = ASCIIEncoding.ASCII.GetBytes("STOP");
        byte[] msg_getbaseline = ASCIIEncoding.ASCII.GetBytes("GETBASE");
        byte[] msg_rptbaseline = ASCIIEncoding.ASCII.GetBytes("BASELINE");
        byte[] msg_config = ASCIIEncoding.ASCII.GetBytes("CONFIG");
        byte[] msg_data = ASCIIEncoding.ASCII.GetBytes("DATA");
        byte[] msg_audioout = ASCIIEncoding.ASCII.GetBytes("AUDIOOUT");


        int mChunkSize = 512;
        TcpClient mClient;
        NetworkStream mStream;

        byte[] packetHdr = new byte[4];
        int packetHdr_Offset = 0;
        int dataLen = 0;
        byte[] data;
        int dataOffset = 0;

        public Net()
        {
            //mClient = new TcpClient();
            AudioInStream = new FIFOStream(mChunkSize);
            AudioInStream.DataAvailable += AudioInStream_DataAvailable;
            AudioOutStream = new FIFOStream(mChunkSize);
        }

        void AudioInStream_DataAvailable(object sender, EventArgs e)
        {
            Send(EMSG_TYPE.DATA);
        }


        public FIFOStream AudioInStream;
        public FIFOStream AudioOutStream;
        public bool Connect(string host, int port)
        {
            try
            {
                mClient = new TcpClient(host, port);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERR: Connection failed: " + e.Message);
                return false;
            }
            try
            {
                mStream = mClient.GetStream();
                //mStream.ReadTimeout = mReadTimeout;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERR: Connection(GetStream) failed: " + e.Message);
                return false;
            }

            //send Hello
            Send(EMSG_TYPE.HELLO);

            return true;
        }

        DateTime started;
        double nShorts = 0.0;
        public EMSG_TYPE Recv(bool block)
        {
            try
            {
                if (!block && !mStream.DataAvailable) return EMSG_TYPE._NONE;

                //header
                if (dataLen == 0)
                {
                    while (packetHdr_Offset < 4)
                    {
                        int i = mStream.Read(packetHdr, packetHdr_Offset, (int)(4 - packetHdr_Offset));
                        packetHdr_Offset += i;
                        if (!block && !mStream.DataAvailable) return EMSG_TYPE._NONE;
                    }
                    dataLen = BitConverter.ToInt32(packetHdr, 0);
                    dataLen = System.Net.IPAddress.NetworkToHostOrder(dataLen);
                    data = new byte[dataLen];
                    dataOffset = 0;
                }

                //data
                while (dataOffset < dataLen)
                {
                    int i = mStream.Read(data, dataOffset, (dataLen - dataOffset));
                    dataOffset += i;
                    if (dataOffset < dataLen && !block && !mStream.DataAvailable) return EMSG_TYPE._NONE;
                }

                //data is now full. Parse it
                packetHdr_Offset = 0;
                dataLen = 0;
                dataOffset = 0;

                if (isEqual(data, msg_start, msg_start.Length))
                {
                    started = DateTime.Now;
                    return EMSG_TYPE.START;
                }
                else if (isEqual(data, msg_stop, msg_stop.Length))
                {
                    return EMSG_TYPE.STOP;
                }
                else if (isEqual(data, msg_config, msg_config.Length))
                {
                    return EMSG_TYPE.CONFIG;
                }
                else if (isEqual(data, msg_audioout, msg_audioout.Length))
                {
                    AudioOutStream.GetWriteLock(mStream);
                    AudioOutStream.Write(data, msg_audioout.Length, (data.Length - msg_audioout.Length));
                    AudioOutStream.ReturnWriteLock(mStream);
                    return EMSG_TYPE.AUDIOOUT;
                }
                else
                {
                    return EMSG_TYPE._ERR;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERR: Exception in Recv: " + e.Message);
                mStream.Close();
                mStream = null;
                mClient.Close();
                mClient = null;
                OnConnectionClosed();
            }
            return EMSG_TYPE._ERR;
        }

        public bool Send(EMSG_TYPE type)
        {
            int msgLen = 0;
            byte[] cmd = null;
            byte[] payload = null;

            switch (type)
            {
                case EMSG_TYPE.HELLO:
                    cmd = msg_hello;
                    payload = null;
                    break;
                case EMSG_TYPE.READY:
                    cmd = msg_ready;
                    payload = null;
                    break;
                case EMSG_TYPE.BASELINE:
                    cmd = msg_rptbaseline;
                    payload = null;
                    break;
                case EMSG_TYPE.DATA:
                    cmd = msg_data;
                    int l = (int)AudioInStream.Length;
                    payload = new byte[l];
                    if (AudioInStream.Read(payload, 0, l) < l)
                    {
                        Console.WriteLine("Warning: Underrun in data read");
                    }
                    nShorts += (l / 2);
                    double br = nShorts/((DateTime.Now - started).TotalSeconds);
                    Console.WriteLine("Kinect shorts sent: "+nShorts.ToString()+" Samples/second="+br.ToString());
                    break;
                default:
                    cmd = null;
                    payload = null;
                    break;
            }
            if (cmd == null) return false;
            
            msgLen = cmd.Length;
            if (payload != null) msgLen += payload.Length;
            Console.WriteLine("Sending " + msgLen.ToString() + " bytes");
            if (msgLen == 0)
            {
                throw new Exception("WTF?");
            }
            byte[] msgLen_Bytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(msgLen));
            try
            {
                mStream.Write(msgLen_Bytes, 0, msgLen_Bytes.Length);
                mStream.Write(cmd, 0, cmd.Length);
                if (payload != null) mStream.Write(payload, 0, payload.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine("Err: Failed net write: " + e.Message);
                return false;
            }

            return true;
        }

        public bool Connected
        {
            get
            {
                if(mClient == null) return false;
                if(mStream == null) return false;
                return mClient.Connected;
            }
        }

        bool isEqual(byte[] a, byte[] b, int len)
        {
            if (len <= 0) return true;
            for (int i = 0; i < len; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        public event EventHandler ConnectionClosed;
        void OnConnectionClosed()
        {
            EventHandler c = ConnectionClosed;
            if (c != null) c(this, new EventArgs());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (AudioInStream != null) AudioInStream.Dispose();
                if (AudioOutStream != null) AudioOutStream.Dispose();
                if (mClient != null) mClient.Close();
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
