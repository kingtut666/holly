using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace HollyServer
{
    public class AudioProto
    {
        TcpListener srv;
        List<AudioProtoConnection> clients;
        Object clients_lk;
        string _className = "AudioProto";

        public AudioProto()
        {
            clients = new List<AudioProtoConnection>();
            clients_lk = new Object();
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
            lock (clients_lk)
            {
                clients.Add(c);
            }
            
            Form1.updateLog("New client connected: " + clnt.Client.RemoteEndPoint.ToString(),
                ELogLevel.Info, ELogType.Net|ELogType.Audio);
            srv.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), this);

            Form1.Talker.AddEndpoint(c.EndPoint, c.AudioOut);

            if (Form1.server.StartOnConnect()) Start(c.EndPoint);
        }

        void c_AudioReadyForRecog(System.IO.Stream s, string ID)
        {
            OnNewAudioStream(s, ID);
        }
        void c_AudioConnClosed(AudioProtoConnection conn)
        {
            //remove conn from clients
            lock (clients_lk)
            {
                clients.Remove(conn);
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
            lock (clients_lk)
            {
                foreach (AudioProtoConnection a in clients)
                {
                    if (ID == a.EndPoint || ID == "") a.Start();
                }
            }
        }
        public void Stop(string ID)
        {
            lock (clients_lk)
            {
                foreach (AudioProtoConnection a in clients)
                {
                    if (ID == a.EndPoint || ID == "") a.Stop();
                }
            }
        }

        public delegate void NewAudioStreamDelegate(System.IO.Stream stream, string ID);
        public event NewAudioStreamDelegate NewAudioStream;
        void OnNewAudioStream(System.IO.Stream newStream, string ID)
        {
            if (NewAudioStream != null)
            {
                NewAudioStream(newStream, ID);
            }
        }

        public void RecogSuccessful(string ID)
        {
            lock (clients_lk)
            {
                foreach (AudioProtoConnection conn in clients)
                {
                    if (conn.EndPoint == ID)
                        conn.RecogSuccessful();
                }
            }
        }

        public List<FIFOStream> GetOutputStreams(string ID)
        {
            List<FIFOStream> ret = new List<FIFOStream>();
            lock (clients_lk)
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
            lock (clients_lk)
            {
                foreach (AudioProtoConnection conn in clients)
                {
                    if (ID == null || ID == "" || conn.EndPoint == ID)
                        return conn.NoiseLevel;
                }
            }
            return 0;
        }
    }
}
