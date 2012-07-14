using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HollyServer
{
    //[Flags]
    /*public enum DeviceCapabilitiesEnum
    {
        OnOff  = 0x01,
        Dimmer = 0x02,
        AudioSource = 0x04
    }*/

    public enum EDeviceCapabilities
    {
        None = 0x000,
        [SoundsLike("turn on")]
        TurnOn = 0x001,
        [SoundsLike("turn off")]
        TurnOff = 0x002,
        [SoundsLike("dim")]
        Dim = 0x004,
        [SoundsLike("turn up")]
        TurnUp = 0x008,
        [SoundsLike("turn down")]
        TurnDown = 0x010,
        Special_Audio_In = 0x100,
        Special_XBMC = 0x200
    }

    public class DeviceCapabilities
    {
        EDeviceCapabilities mCaps;
        public DeviceCapabilities(EDeviceCapabilities en)
        {
            mCaps = en;
        }
        public DeviceCapabilities(string descr)
        {
            switch (descr)
            {
                case "Dimmer":
                    mCaps = EDeviceCapabilities.Dim | EDeviceCapabilities.TurnDown
                        | EDeviceCapabilities.TurnOff | EDeviceCapabilities.TurnOn
                        | EDeviceCapabilities.TurnUp;
                    break;
                case "OnOff":
                    mCaps = EDeviceCapabilities.TurnOff | EDeviceCapabilities.TurnOn;
                    break;
                case "AudioIn":
                    mCaps = EDeviceCapabilities.Special_Audio_In;
                    break;
                case "XBMC":
                    mCaps = EDeviceCapabilities.Special_XBMC;
                    break;
                default:
                    mCaps = EDeviceCapabilities.None;
                    break;
            }
        }
        public EDeviceCapabilities Caps
        {
            get { return mCaps; }
            set { mCaps = value; }
        }
        public string CapsAsIntString
        {
            get { return mCaps.ToString(); }
        }
        public List<string> Actions
        {
            get
            {
                List<string> ret = new List<string>();
                Array vals = Enum.GetValues(typeof(EDeviceCapabilities));
                foreach (EDeviceCapabilities c in vals)
                {
                    if ((c & mCaps) == c)
                    {
                        string snd = SoundsLike.Get(c, true);
                        if(snd!=null) ret.Add(snd);
                    }
                }
                
                return ret;
            }
        }
    
        
    
    }
}
