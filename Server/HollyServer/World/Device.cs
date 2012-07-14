using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HollyServer
{
    public enum EDeviceClass
    {
        [SoundsLike("The Lights")]
        Light,
        [SoundsLike("The Kinect")]
        Kinect,
        [SoundsLike("XBMC")]
        XBMC
    }


    public class Device
    {
        Room mParent;
        DeviceCapabilities mCaps;
        EDeviceClass mClass;
        EDeviceProtocol mProto;
        string mID;
        string mFriendly;
        Dictionary<string, string> mArgs;
        Object mInstance;

        public Device(string ID, string friendlyname, EDeviceClass dclass, EDeviceProtocol proto, 
            DeviceCapabilities caps, Dictionary<string,string> args)
        {
            mParent = null;
            mID = ID;
            mFriendly = friendlyname;
            mProto = proto;
            mCaps = caps;
            mClass = dclass;
            mArgs = args;
        }
        public string ID
        {
            set { mID = value; }
            get { return mID; }
        }
        public string FriendlyName
        {
            set { mFriendly = value; }
            get { return mFriendly; }
        }
        public EDeviceProtocol Protocol
        {
            set { mProto = value; }
            get { return mProto; }
        }
        public DeviceCapabilities Capabilities
        {
            set { mCaps = value; }
            get { return mCaps; }
        }
        public EDeviceClass Class
        {
            set { mClass = value; }
            get { return mClass; }
        }
        public Room Parent { 
            get { return mParent; }
            set { mParent = value; }
        }
        public Dictionary<string,string> Args
        {
            get { return mArgs; }
            set { mArgs = value; }
        }
        public Object Instance
        {
            get { return mInstance; }
            set { mInstance = value; }
        }
    }
}
