using ClipboardGapWpf.Formats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using IComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace ClipboardGapWpf
{
    public interface IClipboardDataObject
    {
        bool ContainsImage();
        bool ContainsText();
        bool ContainsFileDropList();
        //bool ContainsDataFormat(params string[] formats);

        void SetImage(BitmapSource bitmapSource);
        void SetText(string text);
        void SetFileDropList(string[] files);

        BitmapSource GetImage();
        string GetText();
        string[] GetFileDropList();

        //TAudio GetAudio();

        //void SetDataFormat<TO>(ClipboardFormat fmt, TO data, IFormatWriter<TO> writer) { }
        //void SetDataFormat(ClipboardFormat fmt, Stream data) { }

        //void SetDataFormat(string format, object obj, bool autoConvert = false);
        //void SetDataFormat(Type format, object obj);
        //T GetDataFormat<T>(string format);
        //T GetDataFormat<T>(Type format);
        //object GetDataFormat(string format);
        //object GetDataFormat(Type format);

        void SetToClipboard();
    }

    public class ClipboardDataObject
    {
        private readonly IComDataObject _data;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        static ClipboardDataObject()
        {

        }

        public ClipboardDataObject() : this(new MyDataObject())
        {
            EnsureSTA();
        }

        private ClipboardDataObject(IComDataObject data)
        {
            this._data = data;
        }

        private static void EnsureSTA()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                throw new InvalidOperationException("Calling thread must be marked STA (Single-Threaded-Apartment).");
        }

        public static void SetConsoleOnlyMode()
        {
            EnsureSTA();
            var hr = NativeMethods.OleInitialize(IntPtr.Zero);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        public static ClipboardDataObject GetCurrentClipboard()
        {
            _semaphore.Wait();
            try
            {
                return new ClipboardDataObject(OleUtil.ClipboardGetDataObject());
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public IEnumerable<ClipboardFormat> GetFormats()
        {
            return OleUtil.EnumFormatsInDataObject(_data).Select(ClipboardFormat.GetFormat);
        }

        public void SetImage(BitmapSource image)
        {
            SetData(ClipboardFormat.Png, image);
            SetData(ClipboardFormat.Dib, image);
        }

        public BitmapSource GetImage()
        {
            return GetAnyDataOfType<BitmapSource>();
        }

        public void SetText(string text)
        {
            // CF_UNICODETEXT will be automatically converted to CF_OEMTEXT, CF_TEXT, CF_LOCALE by the system
            SetData(ClipboardFormat.UnicodeText, text);
        }

        public string GetText()
        {
            // we try to retrieve our preferred format first
            var unicode = GetDataFromFormat<string>(ClipboardFormat.UnicodeText);
            if (unicode != null)
                return unicode;

            // check other common formats
            var other = GetDataFromFormat<string>(ClipboardFormat.UnicodeText);
            if (other != null)
                return other;

            // fallback to any available string data
            return GetAnyDataOfType<string>();
        }

        public string[] GetFileDropList()
        {
            var effect = GetDataFromFormat<System.Windows.DragDropEffects>(ClipboardFormat.DropEffect);

            var drop = GetDataFromFormat<string[]>(ClipboardFormat.FileDrop);
            if (drop != null)
                return drop;

#pragma warning disable CS0612 // Type or member is obsolete
            var legacy = GetDataFromFormat<string>(ClipboardFormat.FileName, ClipboardFormat.FileNameW);
            if (legacy != null)
                return new[] { legacy };
#pragma warning restore CS0612 // Type or member is obsolete

            return null;
        }

        public void SetFileDropList(string[] dropList)
        {
            SetData(ClipboardFormat.FileDrop, dropList);
        }

        private void SetData<T>(ClipboardFormat format, T obj)
        {
            if (_data is MyDataObject my)
            {
                var writers = format.GetWritersForType<T>();
                if (writers == null || !writers.Any())
                    throw new NotSupportedException($"Unable to set data: No converters available for format '{format.Name}' and type '{typeof(T).FullName}'.");
                my.SetFormat<T>(format, obj, writers.First());
            }
            else
            {
                throw new NotSupportedException("Please create a new clipboard object via the empty constructor before trying to set data.");
            }
        }

        private T GetDataFromFormat<T>(params ClipboardFormat[] formats) 
        {
            var presentFormats = OleUtil.EnumFormatsInDataObject(_data).ToDictionary(f => f.cfFormat, f => f);
            var matchFound = false;

            foreach (var pf in formats)
            {
                if (presentFormats.TryGetValue(pf.Id, out var cpb))
                {
                    matchFound = true;
                    var readers = pf.GetReadersForType<T>();

                    if (readers.Any())
                        return OleUtil.GetOleData<T>(_data, pf.Id, readers.First());
                }
            }

            if (matchFound)
                throw new NotSupportedException($"One or more of the requested formats were on the clipboard, but there was no available conversion to type '{typeof(T).FullName}'.");

            return default;
        }

        private T GetAnyDataOfType<T>() where T : class
        {
            if (_data is MyDataObject my)
            {
                var co = my.GetEntryType<T>();
                if (co != null)
                    return co;
            }

            var possibleFormats = ClipboardFormat.Formats
                .Select(f => new { Format = f, Readers = f.GetReadersForType<T>() })
                .Where(f => f.Readers.Any())
                .ToDictionary(f => f.Format.Id, f => f);

            var presentFormats = OleUtil.EnumFormatsInDataObject(_data).ToDictionary(f => f.cfFormat, f => f);

            foreach (var f in presentFormats)
            {
                if (possibleFormats.TryGetValue(f.Key, out var cpb))
                {
                    return OleUtil.GetOleData<T>(_data, f.Key, cpb.Readers.First());
                }
            }

            return default;
        }

        public void SetToClipboard()
        {
            _semaphore.Wait();
            try
            {
                OleUtil.ClipboardSetDataObject(_data, true);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
