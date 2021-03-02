using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf
{
    class ComStreamImpl : IStream
    {
        private const int
            STGTY_STREAM = 2,
            STGM_READ = 0x00000000,
            STGM_WRITE = 0x00000001,
            STGM_READWRITE = 0x00000002;

        private readonly Stream stream;

        public ComStreamImpl(Stream stream)
        {
            this.stream = stream;
        }

        public void Read(byte[] pv, int cb, IntPtr pcbRead)
        {
            var br = stream.Read(pv, 0, cb);
            if (pcbRead != IntPtr.Zero)
                Marshal.WriteInt32(pcbRead, br);
        }

        public void Write(byte[] pv, int cb, IntPtr pcbWritten)
        {
            stream.Write(pv, 0, cb);
            if (pcbWritten != IntPtr.Zero)
                Marshal.WriteInt32(pcbWritten, cb);
        }

        public void Seek(long offset, int origin, IntPtr newPositionPtr)
        {
            long position = stream.Seek(offset, (SeekOrigin)origin);
            if (newPositionPtr != IntPtr.Zero)
                Marshal.WriteInt64(newPositionPtr, position);
        }

        public void SetSize(long libNewSize)
        {
            stream.SetLength(libNewSize);
        }

        public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            throw new NotImplementedException();
        }

        public void Commit(int grfCommitFlags)
        {
            throw new NotImplementedException();
        }

        public void Revert()
        {
            throw new NotImplementedException();
        }

        public void LockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new NotImplementedException();
        }

        public void UnlockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new NotImplementedException();
        }

        public void Clone(out IStream ppstm)
        {
            ppstm = null;
            throw new NotImplementedException();
        }

        public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG streamStats, int grfStatFlag)
        {
            streamStats = new System.Runtime.InteropServices.ComTypes.STATSTG();
            streamStats.type = STGTY_STREAM;
            streamStats.cbSize = stream.Length;

            // Return access information in grfMode.
            streamStats.grfMode = 0; // default value for each flag will be false
            if (stream.CanRead && stream.CanWrite)
            {
                streamStats.grfMode |= STGM_READWRITE;
            }
            else if (stream.CanRead)
            {
                streamStats.grfMode |= STGM_READ;
            }
            else if (stream.CanWrite)
            {
                streamStats.grfMode |= STGM_WRITE;
            }
            else
            {
                throw new ObjectDisposedException(nameof(stream));
            }
        }
    }
}
