using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace KingTutUtils
{
    public class WavUtils : IDisposable
    {
        static byte[] wavheader_16bit = {
	0x52, 0x49, 0x46, 0x46, // ChunkID = "RIFF"
	0x00, 0x00, 0x00, 0x00, // Chunksize (will be overwritten later)
	0x57, 0x41, 0x56, 0x45, // Format = "WAVE"
	0x66, 0x6d, 0x74, 0x20, // Subchunk1ID = "fmt "
	0x10, 0x00, 0x00, 0x00, // Subchunk1Size = 16
	0x01, 0x00, 0x01, 0x00, // AudioFormat = 1 (linear quantization) | NumChannels = 1
	0x80, 0x3e, 0x00, 0x00, // SampleRate = 16000 Hz
	0x00, 0x7d, 0x00, 0x00, // ByteRate = SampleRate * NumChannels * BitsPerSample/8 = 64000
	0x02, 0x00, 0x10, 0x00, // BlockAlign = NumChannels * BitsPerSample/8 = 4 | BitsPerSample = 32
	0x64, 0x61, 0x74, 0x61, // Subchunk2ID = "data"
	0x00, 0x00, 0x00, 0x00, // Subchunk2Size = NumSamples * NumChannels * BitsPerSample / 8 (will be overwritten later)
};
        static byte[] wavheader_32bit = {
	0x52, 0x49, 0x46, 0x46, // ChunkID = "RIFF"
	0x00, 0x00, 0x00, 0x00, // Chunksize (will be overwritten later)
	0x57, 0x41, 0x56, 0x45, // Format = "WAVE"
	0x66, 0x6d, 0x74, 0x20, // Subchunk1ID = "fmt "
	0x10, 0x00, 0x00, 0x00, // Subchunk1Size = 16
	0x01, 0x00, 0x01, 0x00, // AudioFormat = 1 (linear quantization) | NumChannels = 1
	0x80, 0x3e, 0x00, 0x00, // SampleRate = 16000 Hz
	0x00, 0xfa, 0x00, 0x00, // ByteRate = SampleRate * NumChannels * BitsPerSample/8 = 64000
	0x04, 0x00, 0x20, 0x00, // BlockAlign = NumChannels * BitsPerSample/8 = 4 | BitsPerSample = 32
	0x64, 0x61, 0x74, 0x61, // Subchunk2ID = "data"
	0x00, 0x00, 0x00, 0x00, // Subchunk2Size = NumSamples * NumChannels * BitsPerSample / 8 (will be overwritten later)
};
        public static void SaveWav(Stream source, int BitsPerSample, Stream destination)
        {
            try
            {
                BinaryWriter wav = new BinaryWriter(destination);
                if (BitsPerSample == 16) wav.Write(wavheader_16bit);
                else wav.Write(wavheader_32bit); //32 bit
                source.Seek(0, SeekOrigin.Begin);
                int offset = 0;
                byte[] k_bytes = new byte[2048];
                int read;

                while (offset < source.Length)
                {
                    read = source.Read(k_bytes, 0, 2048);
                    if (read == 0) break;
                    offset += read;
                    wav.Write(k_bytes, 0, read);
                }

                wav.Seek(4, SeekOrigin.Begin);
                wav.Write((uint)(offset + 36));
                wav.Seek(40, SeekOrigin.Begin);
                wav.Write((uint)offset);
                wav.Close();
                source.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception e)
            {
                throw new Exception("Write failed: " + e.ToString());
            }
        }
        public static void SaveWav(Stream s, int BitsPerSample, string filename)
        {
            SaveWav(s, BitsPerSample, File.OpenWrite(filename));
        }
        public static bool ReadWav(string filename, MemoryStream outStream)
        {
            //TODO: This is pretty inefficient
            try
            {
                using (FileStream f = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    byte[] buf = new byte[1024];
                    int i;
                    f.Seek(wavheader_16bit.Length, SeekOrigin.Begin);
                    while ((i = f.Read(buf, 0, 1024)) > 0)
                    {
                        outStream.Write(buf, 0, i);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("ERR: Failed to read wav file: " + e.Message);
            }
            return true;
        }
        BinaryWriter wav;
        object wavLock;
        int samples;
        public WavUtils(int BitsPerSample, string filename)
        {
            wav = new BinaryWriter(File.OpenWrite(filename));
            if (BitsPerSample == 16) wav.Write(wavheader_16bit);
            else wav.Write(wavheader_32bit); //32 bit
            samples = 0;
            wavLock = new object();
        }
        public void Write(byte[] data, int index, int count)
        {
            lock (wavLock)
            {
                if (wav == null) return;
                wav.Write(data, index, count);
            }
            samples += count;
        }
        public void Close()
        {
            BinaryWriter a = null;
            lock (wavLock)
            {
                a = wav;
                wav = null;
            }
            if (a == null) return;
            a.Seek(4, SeekOrigin.Begin);
            a.Write((uint)(samples + 36));
            a.Seek(40, SeekOrigin.Begin);
            a.Write((uint)samples);
            a.Close();
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (wav != null) wav.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

