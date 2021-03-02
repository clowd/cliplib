using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf.Formats
{
    class TextAnsi : IFormatHandleReader<string>, IFormatWriter<string>
    {
        public unsafe int GetByteSize(string data)
        {
            int size = NativeMethods.WideCharToMultiByte(0 /*CP_ACP*/, 0, data, data.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
            return size + 1;
        }

        public unsafe string ReadFromHandle(IntPtr ptr)
        {
            return new string((sbyte*)ptr);
        }

        public void SaveToHandle(string str, IntPtr ptr)
        {
            int pinvokeSize = NativeMethods.WideCharToMultiByte(0 /*CP_ACP*/, 0, str, str.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
            byte[] strBytes = new byte[pinvokeSize];
            NativeMethods.WideCharToMultiByte(0 /*CP_ACP*/, 0, str, str.Length, strBytes, strBytes.Length, IntPtr.Zero, IntPtr.Zero);
            NativeMethods.CopyMemory(ptr, strBytes, pinvokeSize);
        }
    }
}
