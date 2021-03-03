using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf.Formats
{
    class TextUnicode : IDataHandleReader<string>, IDataHandleWriter<string>
    {
        public int GetDataSize(string data)
        {
            return (data.Length * 2 + 2);
        }

        public unsafe string ReadFromHandle(IntPtr ptr, int memSize)
        {
            return new string((char*)ptr);
        }

        public void WriteToHandle(string data, IntPtr ptr)
        {
            char[] chars = data.ToCharArray(0, data.Length);
            NativeMethods.CopyMemoryW(ptr, chars, chars.Length * 2);
        }
    }
}
