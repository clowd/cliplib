using ClipboardGapWpf.Formats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClipboardGapWpf
{
    public class ClipboardHandle : IDisposable
    {
        public bool IsDisposed { get; private set; }

        IntPtr _hWindow;
        short _clsAtom;
        string _clsName;
        bool _cleared;

        protected ClipboardHandle()
        {
            _clsName = "ClipboardGap_" + DateTime.Now.Ticks;

            WindowClass wc;
            wc.style = 0;
            wc.lpfnWndProc = OnWindowMessageReceived;
            wc.cbClsExtra = 0;
            wc.cbWndExtra = 0;
            wc.hInstance = IntPtr.Zero;
            wc.hIcon = IntPtr.Zero;
            wc.hCursor = IntPtr.Zero;
            wc.hbrBackground = IntPtr.Zero;
            wc.lpszMenuName = "";
            wc.lpszClassName = _clsName;

            _clsAtom = NativeMethods.RegisterClass(ref wc);
            if (_clsAtom == 0)
                throw new Win32Exception();

            _hWindow = NativeMethods.CreateWindowEx(0, _clsName, "", 0, 0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (_hWindow == IntPtr.Zero)
                throw new Win32Exception();

            // try a few times to open the clipboard, if we fail, destroy our window and throw
            try
            {
                int i = 10;
                while (true)
                {
                    var success = NativeMethods.OpenClipboard(_hWindow);
                    if (success)
                        break;

                    if (--i == 0)
                    {
                        var hr = Marshal.GetLastWin32Error();
                        var mex = Marshal.GetExceptionForHR(hr);

                        if (hr == 5)  // ACCESS DENIED
                        {
                            IntPtr hwnd = NativeMethods.GetOpenClipboardWindow();
                            if (hwnd != IntPtr.Zero)
                            {
                                uint threadId = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
                                string processName = "Unknown";
                                try
                                {
                                    var p = Process.GetProcessById((int)processId);
                                    processName = p.ProcessName;
                                }
                                catch { }

                                throw new ClipboardBusyException((int)processId, processName, mex);

                            }
                            else
                            {
                                throw new ClipboardBusyException(mex);
                            }
                        }

                        throw mex;
                    }

                    Thread.Sleep(100);
                }
            }
            catch
            {
                NativeMethods.DestroyWindow(_hWindow);
                NativeMethods.UnregisterClass(_clsName, IntPtr.Zero);
                throw;
            }
        }

        public static ClipboardHandle Open()
        {
            return new ClipboardHandle();
        }

        public virtual void Empty()
        {
            ThrowIfDisposed();

            if (!NativeMethods.EmptyClipboard())
                throw new Win32Exception();
            _cleared = true;
        }

        public virtual IEnumerable<ClipboardFormat> GetPresentFormats()
        {
            uint next = NativeMethods.EnumClipboardFormats(0);
            while (next != 0)
            {
                yield return ClipboardFormat.GetFormat(next);
                next = NativeMethods.EnumClipboardFormats(next);
            }

            // If there are no more clipboard formats to enumerate, the return value is zero. 
            // In this case, the GetLastError function returns the value ERROR_SUCCESS.
            var err = Marshal.GetLastWin32Error();
            if (err != 0)
                Marshal.ThrowExceptionForHR(err);
        }

        public virtual string GetText()
        {
            return GetFormat(ClipboardFormat.UnicodeText);
        }

        public virtual void SetText(string text)
        {
            SetFormat(ClipboardFormat.UnicodeText, text);
        }

        public virtual void SetImage(BitmapSource bitmap)
        {
            // Write PNG format as some applications do not support alpha in DIB's and
            // also often will attempt to read PNG format first.
            SetFormat(ClipboardFormat.Png, bitmap);
            SetFormat(ClipboardFormat.Dib, bitmap);
        }

        public virtual BitmapSource GetImage()
        {
            return GetFormat(ClipboardFormat.Png) ?? GetFormat(ClipboardFormat.Dib);
        }

        public virtual string[] GetFileDropList()
        {
            var drop = GetFormat(ClipboardFormat.FileDrop);
            if (drop != null)
                return drop;

#pragma warning disable CS0612 // Type or member is obsolete
            var legacy = GetFormat(ClipboardFormat.FileNameW) ?? GetFormat(ClipboardFormat.FileName);
            if (legacy != null)
                return new[] { legacy };
#pragma warning restore CS0612 // Type or member is obsolete

            return null;
        }

        public virtual T GetFormat<T>(ClipboardFormat<T> format)
        {
            return GetFormatObject(format.Id, format.ObjectParserTyped);
        }

        public virtual byte[] GetFormat(ClipboardFormat format)
        {
            return GetFormatObject(format.Id, new BytesDataConverter());
        }

        public virtual void SetFormat<T>(ClipboardFormat<T> format, T obj)
        {
            SetFormatObject(format.Id, obj, format.ObjectParserTyped);
        }

        public virtual void SetFormat(ClipboardFormat format, byte[] bytes)
        {
            SetFormatObject(format.Id, bytes, new BytesDataConverter());
        }

        protected virtual T GetFormatObject<T>(uint format, IDataConverter<T> converter)
        {
            ThrowIfDisposed();

            var hglobal = NativeMethods.GetClipboardData(format);

            if (hglobal == IntPtr.Zero)
            {
                var err = Marshal.GetLastWin32Error();
                if (err == 0)
                {
                    throw new Exception("Clipboard data could not be retrieved for this format, is it currently present?");
                }
                else
                {
                    throw new Win32Exception(err);
                }
            }

            return converter.ReadFromHGlobal(hglobal);
        }

        protected virtual void SetFormatObject<T>(uint cfFormat, T obj, IDataConverter<T> converter)
        {
            ThrowIfDisposed();

            // EmptyClipboard must be called to update the current clipboard owner before setting data
            if (!_cleared)
                Empty();

            var hglobal = converter.WriteToHGlobal(obj);
            if (hglobal == IntPtr.Zero)
                throw new Exception("Unable to copy data into global memory");

            try
            {
                var hdata = NativeMethods.SetClipboardData(cfFormat, hglobal);
                if (hdata == IntPtr.Zero)
                    throw new Win32Exception();
            }
            catch
            {
                // free hglobal only if error, if success - ownership of hglobal has transferred to system
                NativeMethods.GlobalFree(hglobal);
            }
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            NativeMethods.CloseClipboard();
            NativeMethods.DestroyWindow(_hWindow);
            NativeMethods.UnregisterClass(_clsName, IntPtr.Zero);
        }

        protected virtual void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(ClipboardHandle));
        }

        protected virtual IntPtr OnWindowMessageReceived(IntPtr hwnd, uint messageId, IntPtr wparam, IntPtr lparam)
        {
            return NativeMethods.DefWindowProc(hwnd, messageId, wparam, lparam);
        }
    }
}
