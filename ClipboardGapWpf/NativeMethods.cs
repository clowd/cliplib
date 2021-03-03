using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace ClipboardGapWpf
{
    internal class NativeMethods
    {
        public const int S_OK = 0x00000000;
        public const int S_FALSE = 0x00000001;

        public const int
            E_NOTIMPL = unchecked((int)0x80004001),
            E_OUTOFMEMORY = unchecked((int)0x8007000E),
            E_INVALIDARG = unchecked((int)0x80070057),
            E_NOINTERFACE = unchecked((int)0x80004002),
            E_FAIL = unchecked((int)0x80004005),
            E_ABORT = unchecked((int)0x80004004),
            E_ACCESSDENIED = unchecked((int)0x80070005),
            E_UNEXPECTED = unchecked((int)0x8000FFFF),
            CO_E_UNINITIALIZED = -2147221008,
            CLIPBRD_E_CANT_OPEN = -2147221040;

        public const int GMEM_MOVEABLE = 0x0002;
        public const int GMEM_ZEROINIT = 0x0040;


        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern int DragQueryFile(HandleRef hDrop, int iFile, StringBuilder lpszFile, int cch);

        [DllImport("ole32.dll")]
        public static extern int OleInitialize(IntPtr pvReserved);

        [DllImport("ole32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int OleGetClipboard(ref IComDataObject data);

        [DllImport("ole32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int OleSetClipboard(IComDataObject pDataObj);

        [DllImport("ole32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int OleFlushClipboard();

        [DllImport("ole32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern void ReleaseStgMedium(ref System.Runtime.InteropServices.ComTypes.STGMEDIUM medium);

        [DllImport("user32.dll")]
        public static extern IntPtr GetOpenClipboardWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, BestFitMapping = false, SetLastError = true)]
        public static extern int GetClipboardFormatName(int format, StringBuilder lpString, int cchMax);

        [DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, BestFitMapping = false)]
        public static extern int RegisterClipboardFormat(string format);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GlobalAlloc(int uFlags, int dwBytes);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GlobalLock(IntPtr handle);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool GlobalUnlock(IntPtr handle);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GlobalSize(IntPtr handle);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GlobalFree(IntPtr handle);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GlobalReAlloc(IntPtr handle, int bytes, int flags);

        [DllImport("kernel32.dll", ExactSpelling = true, EntryPoint = "RtlMoveMemory", CharSet = CharSet.Unicode)]
        public static extern void CopyMemoryW(IntPtr pdst, char[] psrc, int cb);

        [DllImport("kernel32.dll", ExactSpelling = true, EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(IntPtr pdst, byte[] psrc, int cb);

        [DllImport("kernel32.dll", ExactSpelling = true, EntryPoint = "RtlMoveMemory", CharSet = CharSet.Ansi)]
        public static extern void CopyMemoryA(IntPtr pdst, char[] psrc, int cb);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int WideCharToMultiByte(int codePage, int flags, [MarshalAs(UnmanagedType.LPWStr)]string wideStr, int chars, [In, Out]byte[] pOutBytes, int bufferBytes, IntPtr defaultChar, IntPtr pDefaultUsed);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int lstrlen(String s);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
