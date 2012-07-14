using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HollyServer
{
    public class Room
    {
        List<Device> devices;
        List<Room> mConnectsTo;
        string mName;

        public Room(string name)
        {
            devices = new List<Device>();
            mConnectsTo = new List<Room>();
            mName = name;
        }
        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }
        public void AddDevice(Device dev)
        {
            devices.Add(dev);
            dev.Parent = this; //TODO: What if device was already in use?
        }
        public void AddDoorway(Room connectsTo)
        {
            if (!mConnectsTo.Contains(connectsTo))
            {
                mConnectsTo.Add(connectsTo);
                connectsTo.AddDoorway(this);
            }
        }
        public List<Device> ListDevices()
        {
            return devices; //TODO: Not safe
        }


    }
}
