using ClipboardGapWpf.Data;
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

        private static void OleRetryTask(Func<int> task)
        {
            // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.
            int i = OLE_RETRY_COUNT;
            while (true)
            {
                int hr = task();

                if (hr >= 0) // success
                    break;

                ThrowIfOleUninitialized(hr);

                if (--i == 0)
                {
                    ThrowIfClipboardBusy(hr);
                    Marshal.ThrowExceptionForHR(hr);
                }

                Thread.Sleep(OLE_RETRY_DELAY);
            }
        }

        public static void OleFlushClipboard()
        {
            OleRetryTask(NativeMethods.OleFlushClipboard);
        }

        public static void ClipboardSetDataObject(IDataObject dataObject, bool copy)
        {
            if (dataObject == null)
                throw new ArgumentNullException(nameof(dataObject));

            OleRetryTask(() => NativeMethods.OleSetClipboard(dataObject));

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
            IDataObject oleDataObject = null;
            OleRetryTask(() => NativeMethods.OleGetClipboard(ref oleDataObject));
            return oleDataObject;
        }

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

        public unsafe static bool GetOleDataFromMedium<T>(ref STGMEDIUM medium, IDataReader<T> reader, out T data)
        {
            // HANDLE -> HANDLE
            if (medium.tymed == TYMED.TYMED_HGLOBAL && reader is IDataHandleReader<T> handleReader)
            {
                Console.WriteLine("HANDLE -> HANDLE");
                var size = NativeMethods.GlobalSize(medium.unionmember);
                var ptr = NativeMethods.GlobalLock(medium.unionmember);
                try
                {
                    return handleReader.ReadFromHandle(ptr, size);
                }
                finally
                {
                    NativeMethods.GlobalUnlock(medium.unionmember);
                }
            }

            // ISTREAM -> ISTREAM
            if (medium.tymed == TYMED.TYMED_ISTREAM && reader is IDataStreamReader<T> streamReader)
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
            if (medium.tymed == TYMED.TYMED_ISTREAM && reader is IDataHandleReader<T> streamToHandle)
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
                        streamToHandle.ReadFromHandle(ptr, size);
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
            if (medium.tymed == TYMED.TYMED_HGLOBAL && reader is IDataStreamReader<T> handleToStream)
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


        public unsafe static bool GetOleData<T>(IDataObject data, short cfFormat, IDataReader<T> reader, out T result)
        {
            TYMED[] searchOrder;

            if (reader is IDataHandleReader<T>)
            {
                searchOrder = new TYMED[] { TYMED.TYMED_HGLOBAL, TYMED.TYMED_ISTREAM };
            }
            else if (reader is IDataStreamReader<T>)
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

        public static void ThrowIfOleUninitialized(int hr)
        {
            if (hr == NativeMethods.CO_E_UNINITIALIZED)
            {
                throw new InvalidOperationException("Calling thread must be marked STA and have a window & message pump.", Marshal.GetExceptionForHR(hr));
            }
        }

        public static void ThrowIfClipboardBusy(int hr)
        {
            if (hr == NativeMethods.CLIPBRD_E_CANT_OPEN)
            {
                try
                {
                    IntPtr hwnd = NativeMethods.GetOpenClipboardWindow();

                    if (hwnd != IntPtr.Zero)
                    {
                        uint processId;
                        uint threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out processId);
                        string processName = "Unknown";

                        try
                        {
                            var p = Process.GetProcessById((int)processId);
                            processName = p.ProcessName;
                        }
                        catch
                        { }

                        throw new ClipboardBusyException((int)processId, processName, Marshal.GetExceptionForHR(hr));
                    }
                }
                catch
                {
                }

                // if we could not find any information about the locking process (it doesn't have a window?)
                throw new ClipboardBusyException(Marshal.GetExceptionForHR(hr));
            }
        }
    }
}