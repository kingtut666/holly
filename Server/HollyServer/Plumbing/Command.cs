using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HollyServer
{
    public class Command
    {
        List<Command> mSubCommands;
        Room mRoom;
        Device mDevice;
        EDeviceCapabilities mAction;
        bool mHasData = false;


        public Command(string room, string item, string action, TheWorld world)
        {
            mSubCommands = new List<Command>();
            //is item a device class?
            bool itemIsDeviceClass = false;
            EDeviceClass itemClass = EDeviceClass.Kinect;
            Array vals = Enum.GetValues(typeof(EDeviceClass));
            foreach (EDeviceClass c in vals)
            {
                if (SoundsLike.Get(c, false) == item)
                {
                    itemIsDeviceClass = true;
                    itemClass = c;
                    break;
                }
            }

            //convert the action to a devicecapabilitiesenum
            EDeviceCapabilities actionResolved = EDeviceCapabilities.None;
            vals = Enum.GetValues(typeof(EDeviceCapabilities));
            foreach (EDeviceCapabilities c in vals)
            {
                if (SoundsLike.Get(c, true) == action)
                {
                    actionResolved = c;
                    break;
                }
            }

            //find the devices
            foreach (Room rm in world.ListRooms())
            {
                if (room == "" || rm.Name == room)
                {
                    foreach (Device d in rm.ListDevices())
                    {
                        bool hit = false;
                        if (itemIsDeviceClass)
                        {
                            if (itemClass == d.Class) hit = true;
                        }
                        else
                        {
                            if (item == d.FriendlyName) hit = true;
                        }
                        if (hit)
                        {
                            Command c = new Command(rm, d, actionResolved);
                            mSubCommands.Add(c);
                        }
                    }
                }
            }
        }

        public Command(Room room, Device device, EDeviceCapabilities action)
        {
            mAction = action;
            mDevice = device;
            mRoom = room;
            mHasData = true;
        }

        public bool HasData { get { return mHasData; } }
        public bool HasChildren
        {
            get
            {
                if (mSubCommands != null && mSubCommands.Count > 0) return true;
                return false;
            }

        }

        public Room TargetRoom { get { return mRoom; } }
        public Device TargetDevice { get { return mDevice; } }
        public EDeviceCapabilities Action { get { return mAction; } }

        public List<Command> Children { get { return mSubCommands; } }


    }
}
