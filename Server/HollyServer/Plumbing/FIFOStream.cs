using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HollyServer
{
    public class FIFOStream : Stream
    {
        //MemoryStream mStream;
        Queue<byte[]> mBlocks = new Queue<byte[]>();
        byte[] incompleteBlock;
        int incompleteBlockFilled;
        int mChunkSize;

        public int ChunkSize { get { return mChunkSize; } }

        public FIFOStream(int chunkSize)
        {
            mChunkSize = chunkSize;

            mBlocks = new Queue<byte[]>();
            incompleteBlock = new byte[mChunkSize];
            incompleteBlockFilled = 0;
        }

        public int QueueLength
        {
            get { return mBlocks.Count; }
        }

        public override void Close()
        {
            base.Close();
        }

        public byte[] ReadChunk()
        {
            return mBlocks.Dequeue();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (writeLock == null)
            {
                Form1.updateLog("ERR: Attempt to write to FIFOStream without writelock", ELogLevel.Error, 
                    ELogType.Audio);
                return;
            }
            int intOffset = 0;
            //Fill incomplete buffer
            if (incompleteBlockFilled > 0)
            {
                bool complete = true;
                int ct = mChunkSize - incompleteBlockFilled;
                if (ct > count)
                {
                    ct = count;
                    complete = false;
                }
                Buffer.BlockCopy(buffer, offset, incompleteBlock, incompleteBlockFilled, ct);
                if (!complete)
                {
                    incompleteBlockFilled += ct;
                    return;
                }
                mBlocks.Enqueue(incompleteBlock);
                incompleteBlock = new byte[mChunkSize];
                incompleteBlockFilled = 0;
                intOffset += ct;
            }

            //Full Blocks
            while (intOffset <= count - mChunkSize)
            {
                byte[] block = new byte[mChunkSize];
                Buffer.BlockCopy(buffer, offset + intOffset, block, 0, mChunkSize);
                mBlocks.Enqueue(block);
                intOffset += mChunkSize;
            }

            //leftover
            int leftOver = count - intOffset;
            if (leftOver > 0)
            {
                Buffer.BlockCopy(buffer, offset + intOffset, incompleteBlock, 0, leftOver);
                incompleteBlockFilled = leftOver;
            }

            if(mBlocks.Count > 0) OnDataAvailable();
        }
        
        public override bool CanRead
        {
            get { return false; }
        }
        public override bool CanSeek
        {
            get { return false; }
        }
        public override bool CanTimeout
        {
            get
            {
                return false ;
            }
        }
        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            return;
        }
      
        public delegate void DataAvailableDelegate();
        public event DataAvailableDelegate DataAvailable;
        void OnDataAvailable()
        {
            if (DataAvailable != null)
            {
                DataAvailable();
            }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }
        public override long Position
        {
            get
            {
                return 0;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        Object writeLock = null;
        Object writeLockLock = new object();
        public Object GetWriteLock(Object o)
        {
            lock (writeLockLock)
            {
                if (writeLock != null && writeLock != o) return null;
                if (writeLock == null) writeLock = o;
                return writeLock;
            }
        }
        public bool ReturnWriteLock(Object o)
        {
            lock (writeLockLock)
            {
                if (writeLock == null) return true;
                if (writeLock == o)
                {
                    writeLock = null;
                    return true;
                }
                return false;
            }
        }
    }
}
