using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace ClipboardGapWpf.Data
{
    internal class ComStreamWrapper : Stream
    {
        private IStream mSource;
        private IntPtr mInt64;
        private long mPos;

        public ComStreamWrapper(IStream source)
        {
            mSource = source;
            mInt64 = Marshal.AllocCoTaskMem(8);
            mSource.Seek(0, 0, IntPtr.Zero);
            mPos = 0;
        }

        ~ComStreamWrapper()
        {
            Marshal.FreeCoTaskMem(mInt64);
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override void Flush()
        {
            mSource.Commit(0);
        }

        public override long Length
        {
            get
            {
                STATSTG stat;
                mSource.Stat(out stat, 1);
                return stat.cbSize;
            }
        }

        public override long Position
        {
            get => mPos;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset != 0) throw new NotImplementedException();
            mSource.Read(buffer, count, mInt64);
            var bytesRead = Marshal.ReadInt32(mInt64);
            mPos += bytesRead;
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin != SeekOrigin.Begin)
                throw new NotSupportedException("Only seeking from Origin=Begin is supported.");

            mPos = offset;
            mSource.Seek(offset, (int)origin, mInt64);
            return Marshal.ReadInt64(mInt64);
        }

        public override void SetLength(long value)
        {
            mSource.SetSize(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
