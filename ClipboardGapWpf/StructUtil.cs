using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf
{
    internal class StructUtil
    {
        public static byte[] Serialize<T>(T s) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var array = new byte[size];
            int offset = 0;
            SerializeTo(s, array, ref offset);
            return array;
        }

        public static void SerializeTo<T>(T s, byte[] buffer, ref int destOffset) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(s, ptr, true);
            Marshal.Copy(ptr, buffer, destOffset, size);
            Marshal.FreeHGlobal(ptr);
            destOffset += size;
        }

        public static T Deserialize<T>(byte[] buffer, int offset) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, offset, ptr, size);
            var s = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return s;
        }
    }
}
