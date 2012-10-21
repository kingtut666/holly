using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace KingTutUtils
{
    public class FIFOStream : Stream
    {
        //MemoryStream mStream;
        Queue<byte[]> mBlocks = new Queue<byte[]>();
        byte[] incompleteBlock;
        int incompleteBlockFilled;
        int mChunkSize;
        long mTotalDataSize;
        int mChunkReadPosition;

        public int ChunkSize { get { return mChunkSize; } }

        public FIFOStream(int chunkSize)
        {
            mChunkSize = chunkSize;

            mBlocks = new Queue<byte[]>();
            incompleteBlock = new byte[mChunkSize];

            ClearEverything();
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
            byte[] ret = mBlocks.Dequeue();
            mTotalDataSize -= ret.Length;
            return ret;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (mBlocks.Count == 0) return 0;
            if (buffer == null) throw new ArgumentNullException();
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();
            if (offset + count > buffer.Length) throw new ArgumentException();

            int writeOffset = offset;
            int countRemaining = count;
            //first read out of any incomplete chunk
            if (mChunkReadPosition != 0 && mBlocks.Count>0)
            {
                int toRead = (countRemaining > (mChunkSize - mChunkReadPosition) ? (mChunkSize - mChunkReadPosition) : countRemaining);
                byte[] a = mBlocks.Peek();
                Array.Copy(a, mChunkReadPosition, buffer, writeOffset, toRead);
                countRemaining -= toRead;
                writeOffset += toRead;
                mChunkReadPosition += toRead;
                if (mChunkReadPosition == mChunkSize)
                {
                    mBlocks.Dequeue();
                    mChunkReadPosition = 0;
                }
            }

            //now read any full chunks
            while (countRemaining >= mChunkSize)
            {
                if (mBlocks.Count == 0) break;
                byte[] a = mBlocks.Dequeue();
                a.CopyTo(buffer, writeOffset);
                countRemaining -= mChunkSize;
                writeOffset += mChunkSize;
            }

            //now read any leftover from queue
            if (countRemaining > 0 && mBlocks.Count>0)
            {
                byte[] a = mBlocks.Peek();
                Array.Copy(a, 0, buffer, writeOffset, countRemaining);
                writeOffset += countRemaining;
                mChunkReadPosition += countRemaining;
                countRemaining = 0;
            }

            //finally, read any leftover from the incomplete write block
            if (countRemaining > 0 && incompleteBlockFilled > 0)
            {
                if (countRemaining > incompleteBlockFilled)
                {
                    Array.Copy(incompleteBlock, 0, buffer, writeOffset, incompleteBlockFilled);
                    writeOffset += incompleteBlockFilled;
                    countRemaining -= incompleteBlockFilled;
                    incompleteBlockFilled = 0;
                }
                else
                {
                    Array.Copy(incompleteBlock, 0, buffer, writeOffset, countRemaining);
                    writeOffset += countRemaining;
                    Array.Copy(incompleteBlock, countRemaining, incompleteBlock, 0, incompleteBlockFilled - countRemaining);
                    incompleteBlockFilled -= countRemaining;
                    countRemaining = 0;
                }
            }
            mTotalDataSize -= writeOffset;

            return writeOffset;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (writeLock == null)
            {
                //Form1.updateLog("ERR: Attempt to write to FIFOStream without writelock", ELogLevel.Error, 
                //    ELogType.Audio);
                throw new Exception(" Attempt to write to FIFOStream without writelock");
                //return;
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
            mTotalDataSize += count;
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
      
        public delegate void DataAvailableDelegate(object sender, EventArgs e);
        public event DataAvailableDelegate DataAvailable;
        void OnDataAvailable()
        {
            if (DataAvailable != null)
            {
                DataAvailable(this, new EventArgs());
            }
        }

        public override long Length
        {
            get { return mTotalDataSize; }
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
        void ClearEverything()
        {
            incompleteBlockFilled = 0;
            mBlocks.Clear();
            mChunkReadPosition = 0;
            mTotalDataSize = 0;

        }
        public override void SetLength(long value)
        {
            if (value == 0)
            {
                //purge contents
                ClearEverything();                
                return;
            }
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
