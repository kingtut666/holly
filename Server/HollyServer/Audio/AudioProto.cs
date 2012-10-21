using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using KingTutUtils;

namespace HollyServer
{
    public class AudioProto
    {
        TcpListener srv;
        List<AudioProtoConnection> clients;

        public AudioProto()
        {
            clients = new List<AudioProtoConnection>();
        }

        public int Listen(string port)
        {
            int ipt = 0;
            if(!Int32.TryParse(port, out ipt)){
                Form1.updateLog("Cannot resolve port", ELogLevel.Error, ELogType.Net | ELogType.Audio);
                return -1;
            }
            srv = new TcpListener(System.Net.IPAddress.Any, ipt);
            srv.Start();
            srv.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), this);
            Form1.updateLog("Listening", ELogLevel.Info, ELogType.Net | ELogType.Audio);

            return 0;
        }

        void AddClient(TcpClient clnt)
        {
            //TODO: Locks
            AudioProtoConnection c = new AudioProtoConnection(clnt);
            c.AudioConnClosed += new AudioProtoConnection.AudioConnClosedDelegate(c_AudioConnClosed);
            c.AudioReadyForRecog += new AudioProtoConnection.AudioReadyForRecogDelegate(c_AudioReadyForRecog);
            lock (clients)
            {
                clients.Add(c);
            }
            
            Form1.updateLog("New client connected: " + clnt.Client.RemoteEndPoint.ToString(),
                ELogLevel.Info, ELogType.Net|ELogType.Audio);
            srv.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), this);

            Form1.Talker.AddEndpoint(c.EndPoint, c.AudioOut);

            if (Form1.server.StartOnConnect()) Start(c.EndPoint);
        }

        void c_AudioReadyForRecog(object sender, AudioReadyForRecogEventArgs e)
        {
            OnNewAudioStream(e.s, e.ID);
        }
        void c_AudioConnClosed(object sender, AudioConnClosedEventArgs e)
        {
            //remove conn from clients
            lock (clients)
            {
                clients.Remove(e.conn);
            }
        }


        public static void DoAcceptTcpClientCallback(IAsyncResult ar)
        {
            AudioProto p = (AudioProto)ar.AsyncState;
            TcpClient client = p.srv.EndAcceptTcpClient(ar);
            p.AddClient(client);
        }

        public void Start(string ID)
        {
            List<AudioProtoConnection> toStart = new List<AudioProtoConnection>();
            lock (clients)
            {
                foreach (AudioProtoConnection a in clients)
                {
                    if (ID == a.EndPoint || ID == "") toStart.Add(a);
                }
            }
            foreach (AudioProtoConnection a in toStart)
            {
                a.Start();
            }
        }
        public void Stop(string ID)
        {
            List<AudioProtoConnection> toStop = new List<AudioProtoConnection>();
            lock (clients)
            {
                foreach (AudioProtoConnection a in clients)
                {
                    if (ID == a.EndPoint || ID == "") toStop.Add(a);
                }
            }
            foreach (AudioProtoConnection a in toStop)
            {
                a.Stop();
            }
        }

        public delegate void NewAudioStreamDelegate(object sender, NewAudioStreamEventArgs e);
        public event NewAudioStreamDelegate NewAudioStream;
        void OnNewAudioStream(System.IO.Stream newStream, string ID)
        {
            if (NewAudioStream != null)
            {
                NewAudioStream(this, new NewAudioStreamEventArgs(newStream, ID));
            }
        }

        public void RecogSuccessful(string ID)
        {
            List<AudioProtoConnection> recognised = new List<AudioProtoConnection>();
            lock (clients)
            {
                foreach (AudioProtoConnection conn in clients)
                {
                    if (conn.EndPoint == ID)
                    {
                        recognised.Add(conn);
                    }
                }
            }
            foreach (AudioProtoConnection conn in recognised)
            {
                conn.PurgeBuffers();
            }
        }

        public List<FIFOStream> GetOutputStreams(string ID)
        {
            List<FIFOStream> ret = new List<FIFOStream>();
            lock (clients)
            {
                foreach (AudioProtoConnection conn in clients)
                {
                    if (ID==null || ID=="" || conn.EndPoint == ID)
                        ret.Add(conn.AudioOut);
                }
            }
            return ret;
        }
        public double GetNoiseLevel(string ID)
        {
            lock (clients)
            {
                foreach (AudioProtoConnection conn in clients)
                {
                    if (ID == null || ID == "" || conn.EndPoint == ID)
                        return conn.NoiseLevel;
                }
            }
            return 0;
        }
        public Dictionary<string, double> SummariseNoiseLevels()
        {
            Dictionary<string, double> ret = new Dictionary<string, double>();
            foreach (AudioProtoConnection conn in clients)
            {
                ret.Add(conn.EndPoint, conn.NoiseLevel);
            }


            return ret;
        }
    }
}
