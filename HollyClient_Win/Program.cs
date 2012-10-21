using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Kinect.Audio;
using System.Threading;

namespace HollyClient_Win
{
    class Program
    {
        static AudioIn_KinectXbox mAudioIn;
        static AudioOut mAudioOut;
        static Net mNet;

        static void Main(string[] args)
        {
            mNet = new Net();
            mNet.ConnectionClosed += mNet_ConnectionClosed;
            mAudioOut = new AudioOut(mNet.AudioOutStream);
            mAudioIn = new AudioIn_KinectXbox(mNet.AudioInStream);

            string server = "127.0.0.1";
            int port = 31337;
            bool retryConnects = true;

            //parse arguments

            //setup


            while (true)
            {
                //connect to server
                if (!mNet.Connected)
                {
                    if (!mNet.Connect(server, port))
                    {
                        Console.Write("Err: failed to connect.");
                        if (retryConnects)
                        {
                            Console.WriteLine("     Waiting 5s and then will retry");
                            Thread.Sleep(5000);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                //enter message loop
                Net.EMSG_TYPE msg = mNet.Recv(true); //note this may still block for a short time
                switch (msg)
                {
                    case Net.EMSG_TYPE.CONFIG:
                        Console.WriteLine("Received config, sending ready");
                        mNet.Send(Net.EMSG_TYPE.READY);
                        break;
                    case Net.EMSG_TYPE.START:
                        Console.WriteLine("Received start");
                        mAudioIn.Start();
                        break;
                    case Net.EMSG_TYPE.STOP:
                        Console.WriteLine("Received stop");
                        mAudioIn.Stop();
                        break;
                    case Net.EMSG_TYPE.GETBASE:
                        Console.WriteLine("Received baseline request");
                        mNet.Send(Net.EMSG_TYPE.BASELINE);
                        break;
                    case Net.EMSG_TYPE.AUDIOOUT:
                        //mAudioOut.DataReceived();
                        break;
                    default:
                        break;
                }
                //Thread.Sleep(50); //ms
            }

        }

        static void mNet_ConnectionClosed(object sender, EventArgs e)
        {
            //stop Kinect
            mAudioIn.Stop();

            //purge buffers
            mNet.AudioInStream.SetLength(0);
            mNet.AudioOutStream.SetLength(0);


        }
    }
}
