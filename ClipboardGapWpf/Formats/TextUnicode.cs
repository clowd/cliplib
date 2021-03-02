using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf.Formats
{
    class TextUnicode : IFormatHandleReader<string>, IFormatWriter<string>
    {
        public int GetByteSize(string data)
        {
            return (data.Length * 2 + 2);
        }

        public unsafe string ReadFromHandle(IntPtr ptr)
        {
            return new string((char*)ptr);
        }

        public void SaveToHandle(string data, IntPtr ptr)
        {
            char[] chars = data.ToCharArray(0, data.Length);
            NativeMethods.CopyMemoryW(ptr, chars, chars.Length * 2);
        }
    }
}
