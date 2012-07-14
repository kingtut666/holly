using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HollyServer
{

    public enum EDeviceProtocol
    {
        AudioProto,
        LWRF,
        XBMC
    }

    public class DeviceProtocolMappings
    {
        LWRF mLWRF;
        TheWorld mWorld;

        public DeviceProtocolMappings()
        {
            mLWRF = new LWRF();
            //AudioProto is handled by AudioProtoConnection
        }
        public void SetWorld(TheWorld world)
        {
            mWorld = world;
        }

        public void Execute(Command cmd)
        {
            if (cmd.HasChildren)
            {
                foreach (Command c in cmd.Children) Execute(c);
            }
            if (cmd.HasData)
            {
                //does the device support the action. It should by this point.
                if ((cmd.Action & cmd.TargetDevice.Capabilities.Caps) != cmd.Action){
                    //TODO: Should throw an error
                    return; 
                }
            
                //does the protocol support the action?
                switch (cmd.TargetDevice.Protocol)
                {
                    case EDeviceProtocol.LWRF:
                        if ((cmd.Action & LWRF.Capabilities) != cmd.Action)
                        {
                            //TODO: Throw error
                            return;
                        }
                        mLWRF.Execute(cmd.TargetDevice.ID, cmd.Action);
                        break;
                    case EDeviceProtocol.AudioProto:
                        //TODO: Should throw an error
                        return;
                    default:
                        //TODO: Should throw and error
                        return;
                }
            
            }
            
        }



    }
}
