using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HollyServer
{
    public class AudioOut
    {
        public static void PlayBeep(FIFOStream s)
        {

        }
        public static void PlayWav(FIFOStream s, string fname)
        {
            //TODO: Handle transcoding
            FileStream fs = File.OpenRead(fname);
            byte[] buffer = new byte[s.ChunkSize];
            int len;
            s.GetWriteLock(fs);
            while ((len=fs.Read(buffer, 0, s.ChunkSize)) > 0)
            {
                
                s.Write(buffer, 0, len);
                
            }
            s.ReturnWriteLock(fs);
            fs.Close();
        }

    }
}
