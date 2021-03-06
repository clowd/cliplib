using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf.Formats
{
    public class TextUtf8 : HandleDataConverterBase<string>
    {
        public override int GetDataSize(string obj)
        {
            var enc = new UTF8Encoding();
            return enc.GetByteCount(obj) + 1;
        }

        public override string ReadFromHandle(IntPtr pointerUtf8, int memSize)
        {
            string stringData = null;
            int utf8ByteCount;

            // read to end of memory, or until null terminator
            for (utf8ByteCount = 0; utf8ByteCount < memSize; utf8ByteCount++)
            {
                byte endByte = Marshal.ReadByte((IntPtr)((long)pointerUtf8 + utf8ByteCount));
                if (endByte == '\0') break;
            }

            if (utf8ByteCount > 0)
            {
                byte[] bytes = new byte[utf8ByteCount];
                Marshal.Copy(pointerUtf8, bytes, 0, utf8ByteCount);
                UTF8Encoding utf8Encoding = new UTF8Encoding();
                stringData = utf8Encoding.GetString(bytes, 0, utf8ByteCount);
            }

            return stringData;
        }

        public override void WriteToHandle(string obj, IntPtr ptr)
        {
            var enc = new UTF8Encoding();
            var bytes = enc.GetBytes(obj);
            NativeMethods.CopyMemory(ptr, bytes, bytes.Length);
        }
    }
}
