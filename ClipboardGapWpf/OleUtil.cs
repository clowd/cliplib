using ClipboardGapWpf.Formats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClipboardGapWpf
{
    internal static class OleUtil
    {
        private const int OLE_RETRY_COUNT = 10;
        private const int OLE_RETRY_DELAY = 100;
        private const int OLE_FLUSH_DELAY = 10;

        private static bool OLE_SUCCESS(int hr) => hr >= 0;
        private static bool OLE_FAIL(int hr) => hr < 0;

        public static void OleFlushClipboard()
        {
            // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.
            // See Dev10 bug 616223 and VSWhidbey bug 476911.

            int i = OLE_RETRY_COUNT;

            while (true)
            {
                int hr = NativeMethods.OleFlushClipboard();

                if (OLE_SUCCESS(hr))
                {
                    break;
                }

                if (--i == 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                Thread.Sleep(OLE_RETRY_DELAY);
            }
        }

        public static void ClipboardSetDataObject(IDataObject dataObject, bool copy)
        {
            if (dataObject == null)
            {
                throw new ArgumentNullException(nameof(dataObject));
            }

            // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.
            // See Dev10 bug 616223 and VSWhidbey bug 476911.

            int i = OLE_RETRY_COUNT;

            while (true)
            {
                // Clear the system clipboard by calling OleSetClipboard with null parameter.
                int hr = NativeMethods.OleSetClipboard(dataObject);

                if (OLE_SUCCESS(hr))
                {
                    break;
                }

                if (--i == 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                Thread.Sleep(OLE_RETRY_DELAY);
            }

            if (copy)
            {
                // Dev10 bug 835751 - OleSetClipboard and OleFlushClipboard both modify the clipboard
                // and cause notifications to be sent to clipboard listeners. We sleep a bit here to
                // mitigate issues with clipboard listeners (like TS) corrupting the clipboard contents
                // as a result of these two calls being back to back.
                Thread.Sleep(OLE_FLUSH_DELAY);

                OleFlushClipboard();
            }
        }

        public static IDataObject ClipboardGetDataObject()
        {
            IDataObject oleDataObject;

            int i = OLE_RETRY_COUNT;

            while (true)
            {
                oleDataObject = null;
                int hr = NativeMethods.OleGetClipboard(ref oleDataObject);

                if (OLE_SUCCESS(hr))
                {
                    break;
                }

                if (--i == 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                Thread.Sleep(OLE_RETRY_DELAY);
            }

            return oleDataObject;
        }

        //private static bool GetTymedUseable(TYMED tymed)
        //{
        //    var ALLOWED_TYMEDS = new TYMED[] {
        //        TYMED.TYMED_HGLOBAL,
        //        TYMED.TYMED_ISTREAM,
        //        TYMED.TYMED_ENHMF,
        //        TYMED.TYMED_MFPICT,
        //        TYMED.TYMED_GDI
        //    };

        //    for (int i = 0; i < ALLOWED_TYMEDS.Length; i++)
        //    {
        //        if ((tymed & ALLOWED_TYMEDS[i]) != 0)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        public static IEnumerable<FORMATETC> EnumFormatsInDataObject(IDataObject data)
        {
            var enumerator = data.EnumFormatEtc(DATADIR.DATADIR_GET);

            if (enumerator == null)
                yield break;

            FORMATETC[] formatetc;
            int[] retrieved;

            enumerator.Reset();

            formatetc = new FORMATETC[] { new FORMATETC() };
            retrieved = new int[] { 1 };

            while (retrieved[0] > 0)
            {
                retrieved[0] = 0;

                if (enumerator.Next(1, formatetc, retrieved) == NativeMethods.S_OK && retrieved[0] > 0)
                {
                    // Release the allocated memory by IEnumFORMATETC::Next for DVTARGETDEVICE
                    // pointer in the ptd member of the FORMATETC structure.
                    // Otherwise, there will be the memory leak.
                    for (int formatetcIndex = 0; formatetcIndex < formatetc.Length; formatetcIndex++)
                    {
                        if (formatetc[formatetcIndex].ptd != IntPtr.Zero)
                        {
                            Marshal.FreeCoTaskMem(formatetc[formatetcIndex].ptd);
                        }
                    }

                    yield return formatetc[0];
                }
            }
        }

        //public static T GetOleData<T>(IDataObject data, FORMATETC format, IFormatReader<T> reader) where T : class
        //{
        //    var hg = GetOleData(data, format, out var medium);
        //    if (hg == IntPtr.Zero)
        //        return null;

        //    try
        //    {

        //        var ptr = NativeMethods.GlobalLock(hg);
        //        try
        //        {
        //            return reader.ConvertToObject(ptr);
        //        }
        //        finally
        //        {
        //            NativeMethods.GlobalUnlock(hg);
        //        }
        //    }
        //    finally
        //    {
        //        if (hg != IntPtr.Zero) NativeMethods.GlobalFree(hg);
        //        NativeMethods.ReleaseStgMedium(ref medium);
        //    }
        //}

        //public static unsafe MemoryStream ToMemoryStream(IStream comStream)
        //{
        //    MemoryStream stream = new MemoryStream();
        //    byte[] pv = new byte[100];
        //    uint num = 0;

        //    IntPtr pcbRead = new IntPtr((void*)&num);

        //    do
        //    {
        //        num = 0;
        //        comStream.Read(pv, pv.Length, pcbRead);
        //        stream.Write(pv, 0, (int)num);
        //    }
        //    while (num > 0);
        //    return stream;
        //}

        public unsafe static T GetOleDataFromMedium<T>(ref STGMEDIUM medium, IFormatReader<T> reader) where T : class
        {
            // HANDLE -> HANDLE
            if (medium.tymed == TYMED.TYMED_HGLOBAL && reader is IFormatHandleReader<T> handleReader)
            {
                Console.WriteLine("HANDLE -> HANDLE");
                var ptr = NativeMethods.GlobalLock(medium.unionmember);
                try
                {
                    return handleReader.ReadFromHandle(ptr);
                }
                finally
                {
                    NativeMethods.GlobalUnlock(medium.unionmember);
                }
            }

            // ISTREAM -> ISTREAM
            if (medium.tymed == TYMED.TYMED_ISTREAM && reader is IFormatStreamReader<T> streamReader)
            {
                Console.WriteLine("ISTREAM -> ISTREAM");
                var stream = new ComStreamWrapper((IStream)Marshal.GetObjectForIUnknown(medium.unionmember));
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    return streamReader.ReadFromStream(stream);
                }
                finally
                {
                    Marshal.Release(medium.unionmember);
                }
            }

            // ISTREAM -> HANDLE
            if (medium.tymed == TYMED.TYMED_ISTREAM && reader is IFormatHandleReader<T> streamToHandle)
            {
                Console.WriteLine("ISTREAM -> HANDLE");
                // we can read the stream into an HGlobal and then send to handle reader
                var pStream = (IComStream)Marshal.GetObjectForIUnknown(medium.unionmember);
                try
                {
                    pStream.Stat(out var stats, 0);
                    int size = (int)stats.cbSize;
                    IntPtr hglobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, size);
                    IntPtr ptr = NativeMethods.GlobalLock(hglobal);
                    try
                    {
                        pStream.Seek(0, 0);
                        pStream.Read(ptr, size);
                        streamToHandle.ReadFromHandle(ptr);
                    }
                    finally
                    {
                        NativeMethods.GlobalUnlock(hglobal);
                        NativeMethods.GlobalFree(hglobal);
                    }
                }
                finally
                {
                    Marshal.Release(medium.unionmember);
                }
            }

            // HANDLE -> ISTREAM
            if (medium.tymed == TYMED.TYMED_HGLOBAL && reader is IFormatStreamReader<T> handleToStream)
            {
                Console.WriteLine("HANDLE -> ISTREAM");
                var ptr = NativeMethods.GlobalLock(medium.unionmember);
                try
                {
                    var size = NativeMethods.GlobalSize(medium.unionmember);
                    byte[] bytes = new byte[size];
                    Marshal.Copy(ptr, bytes, 0, size);
                    handleToStream.ReadFromStream(new MemoryStream(bytes));
                }
                finally
                {
                    NativeMethods.GlobalUnlock(medium.unionmember);
                }
            }

            return null;
        }


        public unsafe static T GetOleData<T>(IDataObject data, short cfFormat, IFormatReader<T> reader) where T : class
        {
            TYMED[] searchOrder;

            if (reader is IFormatHandleReader<T>)
            {
                searchOrder = new TYMED[] { TYMED.TYMED_HGLOBAL, TYMED.TYMED_ISTREAM };
            }
            else if (reader is IFormatStreamReader<T>)
            {
                searchOrder = new TYMED[] { TYMED.TYMED_ISTREAM, TYMED.TYMED_HGLOBAL };
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(reader));
            }

            foreach (var t in searchOrder)
            {
                FORMATETC fmthgl = new FORMATETC()
                {
                    cfFormat = cfFormat,
                    dwAspect = DVASPECT.DVASPECT_CONTENT,
                    lindex = -1,
                    ptd = IntPtr.Zero,
                    tymed = t,
                };

                if (NativeMethods.S_OK == data.QueryGetData(ref fmthgl))
                {
                    data.GetData(ref fmthgl, out var medium);
                    if (medium.unionmember != IntPtr.Zero)
                    {
                        try
                        {
                            return GetOleDataFromMedium(ref medium, reader);
                        }
                        finally
                        {
                            NativeMethods.ReleaseStgMedium(ref medium);
                        }
                    }
                }
            }

            return null;
        }

        //public unsafe static IntPtr GetDataFromOleIStream(IDataObject data, short clFmt)
        //{
        //    // https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/DataObject.cs,d5d12d4d56539d53,references

        //    var medium = GetOleData(data, clFmt, TYMED.TYMED_ISTREAM);

        //    if (medium.unionmember == IntPtr.Zero)
        //        return IntPtr.Zero;

        //    IStream pStream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
        //    Marshal.Release(medium.unionmember);
        //    pStream.Stat(out var stats, 0);
        //    int size = (int)stats.cbSize;

        //    byte[] buffer = new byte[size];

        //    int bytesRead = 0;
        //    int* brPtr = &bytesRead;
        //    pStream.Read(buffer, size, (IntPtr)brPtr);

        //    var ptr = Marshal.AllocHGlobal(size);
        //    Marshal.Copy(buffer, 0, ptr, size);

        //    return ptr;
        //}

        //public static IntPtr GetDataFromOleHGlobal(IDataObject data, short clFmt)
        //{
        //    // https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/DataObject.cs,0d06320c18f1cb9c,references

        //    var medium = GetOleData(data, clFmt, TYMED.TYMED_HGLOBAL);

        //    if (medium.unionmember == IntPtr.Zero)
        //        return IntPtr.Zero;

        //    return medium.unionmember;
        //}

        //private static STGMEDIUM GetOleData(IDataObject data, short clFmt, TYMED tymed)
        //{
        //    FORMATETC formatetc = new FORMATETC();
        //    STGMEDIUM medium = new STGMEDIUM();

        //    formatetc.cfFormat = clFmt;
        //    formatetc.dwAspect = DVASPECT.DVASPECT_CONTENT;
        //    formatetc.lindex = -1;
        //    formatetc.tymed = TYMED.TYMED_ISTREAM;
        //    medium.tymed = TYMED.TYMED_ISTREAM;

        //    data.GetData(ref formatetc, out medium);
        //    return medium;
        //}

        //public static IntPtr CopyHGlobal(IntPtr data)
        //{
        //    IntPtr src = NativeMethods.GlobalLock(data);
        //    var size = NativeMethods.GlobalSize(data);
        //    IntPtr ptr = Marshal.AllocHGlobal(size);
        //    IntPtr buffer = NativeMethods.GlobalLock(ptr);

        //    try
        //    {
        //        for (int i = 0; i < size; i++)
        //        {
        //            byte val = Marshal.ReadByte(new IntPtr((long)src + i));

        //            Marshal.WriteByte(new IntPtr((long)buffer + i), val);
        //        }
        //    }
        //    finally
        //    {
        //        if (buffer != IntPtr.Zero)
        //        {
        //            NativeMethods.GlobalUnlock(buffer);
        //        }

        //        if (src != IntPtr.Zero)
        //        {
        //            NativeMethods.GlobalUnlock(src);
        //        }
        //    }
        //    return ptr;
        //}

        //public static string GetProcessNameHoldingClipboard()
        //{
        //    IntPtr hwnd = NativeMethods.GetOpenClipboardWindow();

        //    if (hwnd == IntPtr.Zero)
        //        return null;

        //    uint processId;
        //    uint threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out processId);

        //    var p = Process.GetProcessById((int)processId);

        //    try
        //    {
        //        return p.Modules[0].FileName;
        //    }
        //    catch
        //    {
        //        return p.ProcessName;
        //    }
        //}
    }
}
