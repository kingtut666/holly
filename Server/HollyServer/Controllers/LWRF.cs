using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace HollyServer
{
    public class LWRF
    {
        public delegate void OnOffEventHandler(object sender, OnOffEventArgs e);
        public delegate void AllOffEventHandler(object sender, AllOffEventArgs e);
        public delegate void MoodEventHandler(object sender, MoodEventArgs e);
        public delegate void DimEventHandler(object sender, DimEventArgs e);
        public delegate void HeatEventHandler(object sender, HeatEventArgs e);
        public delegate void RawEventHandler(object sender, RawEventArgs e);

        public event OnOffEventHandler OnOffEvent;
        /// <summary>
        /// Regex for on/off
        /// matches :Room, Device, and State
        /// </summary>
        public Regex OnOffRegEx = new Regex("...,!R(?<Room>.)D(?<Device>[.^h])F(?<State>.)|");
#pragma warning disable 67
        public event AllOffEventHandler AllOffEvent;
#pragma warning restore 67
        /// <summary>
        /// Regex for All off
        /// Matches: Room
        /// </summary>
        public Regex AllOffRegEx = new Regex("...,!R(?<Room>.)Fa");
#pragma warning disable 67
        public event MoodEventHandler MoodEvent;
#pragma warning restore 67
        /// <summary>
        /// Regex for Mood
        /// Matches: Room, Mood
        /// </summary>
        public Regex MoodRegEx = new Regex("...,!R(?<Room>.)FmP(?<mood>.)|");//"000,!R"+ Room + "FmP" + mood + "|"
#pragma warning disable 67
        public event DimEventHandler DimEvent;
#pragma warning restore 67
        /// <summary>
        /// Regex for Dim
        /// Matches: Room, Device, State
        /// </summary>
        public Regex DimRegEx = new Regex("...,!R(?<Room>.)D(?<Device>.)FdP(?<State>..)|");//"000,!R" + Room + "D" + Device + "FdP" + pstr + "|"
#pragma warning disable 67
        public event HeatEventHandler HeatEvent;
#pragma warning restore 67
        /// <summary>
        /// Regex for Heat commands
        /// Matches: Room, State.
        /// </summary>
        public Regex HeatRegEx = new Regex("...,!R(?<Room>.)DhF(?<State>.)|");//"000,!R" + Room + "DhF" + statestr + "|";
        public event RawEventHandler RawEvent;

        public void listen()
        {
            Socket sock = new Socket(AddressFamily.InterNetwork,
                            SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, 9760);
            sock.Bind(iep);
            EndPoint ep = (EndPoint)iep;
            Console.WriteLine("Ready to receive...");
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024];
                    int recv = sock.ReceiveFrom(data, ref ep);
                    string stringData = Encoding.ASCII.GetString(data, 0, recv);
                    RawEvent(this, new RawEventArgs(stringData));
                    Match OnOffMatch = OnOffRegEx.Match(stringData);
                    Match AllOffMatch = AllOffRegEx.Match(stringData);
                    Match MoodMatch = MoodRegEx.Match(stringData);
                    Match DimMatch = DimRegEx.Match(stringData);
                    Match HeatMatch = HeatRegEx.Match(stringData);
                    if (OnOffMatch.Success)
                    {
                        EventArgs e = new EventArgs();
                        OnOffEvent(this, new OnOffEventArgs(
                            int.Parse(OnOffMatch.Groups["Room"].Value),
                            int.Parse(OnOffMatch.Groups["Device"].Value),
                            bool.Parse(OnOffMatch.Groups["State"].Value)));
                    }
                }
            }
            finally
            {
                sock.Close();
            }
        }

        /// <summary>
        /// Switches off all devices in room
        /// </summary>
        /// <param name="Room">Room to switch all off in.</param>
        public void AllOff(int Room)
        {
            string text = "000,!R" + Room + "Fa|";
            SendRaw(text);
        }

        /// <summary>
        /// sets mood in room
        /// </summary>
        /// <param name="Room">room number </param>
        /// <param name="mood">mood number</param>
        public void Mood(int Room, int mood)
        {
            string text = "000,!R"+ Room + "FmP" + mood + "|";
            SendRaw(text);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Room">room number </param>
        /// <param name="Device">device number</param>
        /// <param name="percent">percentage level for the dim< eg. 50/param>
        public void Dim(int Room, int Device, int percent)
        {
            Dim("R" + Room + "D" + Device, percent);
        }
        public void Dim(string ID, int percent){
            string pstr;
            pstr = Math.Round(((double)percent / 100 * 32)).ToString();
            string text = "999,!" + ID + "FdP" + pstr + "|";
            SendRaw(text);
        }

        /// <summary>
        /// send on/off command to a room/device
        /// </summary>
        /// <param name="Room">room number </param>
        /// <param name="Device">device number</param>
        /// <param name="onState">state (0 or 1)</param>
        public void DeviceOnOff(int Room, int Device, bool onState)
        {
            DeviceOnOff("R" + Room + "D" + Device, onState);
        }
        public void DeviceOnOff(string ID, bool onState)
        {
            string statestr;
            if (onState) statestr = "1"; else statestr = "0";
            string text = "999,!"+ ID + "F" + statestr + "|";
            SendRaw(text);
        }

        /// <summary>
        /// send on/off command to a room/device
        /// </summary>
        /// <param name="Room">room number </param>
        /// <param name="Device">device number</param>
        /// <param name="state">state (0 or 1)</param>
        public void HeatOnOff(int Room, bool state)
        {
            string statestr;
            if (state) statestr = "1"; else statestr = "0";
            string text = "000,!R" + Room + "DhF" + statestr + "|";
            SendRaw(text);
        }
   
        /// <summary>
        /// Send raw packet containing 'text' to the wifilink
        /// </summary>
        /// <param name="text">contents of packet.</param>
        public void SendRaw(string text)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            IPAddress serverAddr = IPAddress.Parse("192.168.1.8");
            IPEndPoint endPoint = new IPEndPoint(serverAddr, 9760);
            byte[] send_buffer = Encoding.ASCII.GetBytes(text);
            sock.SendTo(send_buffer, endPoint);
        }

        public static EDeviceCapabilities Capabilities
        {
            get
            {
                return EDeviceCapabilities.Dim | EDeviceCapabilities.TurnDown | EDeviceCapabilities.TurnOff
                    | EDeviceCapabilities.TurnOn | EDeviceCapabilities.TurnUp;
            }
        }

        public void Execute(string device, EDeviceCapabilities action)
        {
            switch (action)
            {
                case EDeviceCapabilities.TurnUp:
                    Dim(device, 100);
                    break;
                case EDeviceCapabilities.TurnOn:
                    DeviceOnOff(device, true);
                    break;
                case EDeviceCapabilities.TurnOff:
                    DeviceOnOff(device, false);
                    break;
                case EDeviceCapabilities.TurnDown:
                    Dim(device, 30);
                    break;
                case EDeviceCapabilities.Dim:
                    Dim(device, 30);
                    break;
                default:
                    //TODO: Should throw error
                    break;
            }
            Thread.Sleep(300);
        }
    

    
    
    }
}
