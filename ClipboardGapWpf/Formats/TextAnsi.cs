using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf.Formats
{
    class TextAnsi : HandleDataConverterBase<string>
    {
        public override int GetDataSize(string data)
        {
            int size = NativeMethods.WideCharToMultiByte(0 /*CP_ACP*/, 0, data, data.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
            return size + 1;
        }

        public override unsafe string ReadFromHandle(IntPtr ptr, int memSize)
        {
            return new string((sbyte*)ptr);
        }

        public override void WriteToHandle(string str, IntPtr ptr)
        {
            int pinvokeSize = NativeMethods.WideCharToMultiByte(0 /*CP_ACP*/, 0, str, str.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
            byte[] strBytes = new byte[pinvokeSize];
            NativeMethods.WideCharToMultiByte(0 /*CP_ACP*/, 0, str, str.Length, strBytes, strBytes.Length, IntPtr.Zero, IntPtr.Zero);
            NativeMethods.CopyMemory(ptr, strBytes, pinvokeSize);
        }
    }
}
