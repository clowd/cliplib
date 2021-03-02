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

        //TImage GetImage();
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

        //private const string DATAFORMAT_PNG = "PNG";
        //private const string DATAFORMAT_PNG_OFFICEART = "PNG+Office Art";
        //private const string DATAFORMAT_V5BITMAP = "Format17";
        //private const string DATAFORMAT_DIB = "DeviceIndependentBitmap";
        //private const string DATAFORMAT_JPG = "JPG";
        //private const string DATAFORMAT_JPEG = "JPEG";
        //private const string DATAFORMAT_JFIF = "JFIF";
        //private const string DATAFORMAT_GIF = "GIF";
        //private const string DATAFORMAT_TIFF = "TaggedImageFileFormat";
        //private const string DATAFORMAT_JFIF_OFFICEART = "JFIF+Office Art";
        //private const string DATAFORMAT_BITMAP = "System.Drawing.Bitmap";
        //private const string DATAFORMAT_BITMAPSOURCE = "System.Windows.Media.Imaging.BitmapSource";

        static ClipboardDataObject()
        {

        }

        private ClipboardDataObject(IComDataObject data)
        {
            this._data = data;
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

        public BitmapSource GetImage()
        {
            var readers = new FormatSet<BitmapSource>()
            {
                { ClipboardFormat.Png, new ImageWpfPng() },
                { ClipboardFormat.DIBV5, new ImageWpfDibV5() },
                { ClipboardFormat.DIB, new ImageWpfDibV5() },
                { ClipboardFormat.FileDrop, new ImageWpfFileDrop() },
            };

            return GetData(readers);
        }

        public string GetText()
        {
            var readers = new FormatSet<string>()
            {
                { ClipboardFormat.UnicodeText, new TextUnicode() },
                { ClipboardFormat.Text, new TextAnsi() },
                { ClipboardFormat.Rtf, new TextAnsi() },
                { ClipboardFormat.OemText, new TextAnsi() },
                { ClipboardFormat.Html, new TextAnsi() },
            };

            return GetData(readers);
        }

        public string[] GetFileDropList()
        {
            var formats = OleUtil.EnumFormatsInDataObject(_data).ToDictionary(f => f.cfFormat, f => f);

            if (formats.TryGetValue(ClipboardFormat.FileDrop.Id, out var fmtDrop))
            {
                return OleUtil.GetOleData(_data, fmtDrop.cfFormat, new FileDrop());
            }

            var legacyFile = GetData(new FormatSet<string>() {
        #pragma warning disable CS0612 // Type or member is obsolete
                { ClipboardFormat.FileName, new TextAnsi() },
                { ClipboardFormat.FileNameW, new TextUnicode() },
        #pragma warning restore CS0612 // Type or member is obsolete
            });

            if (!String.IsNullOrEmpty(legacyFile))
                return new string[] { legacyFile };

            return null;
        }

        private T GetData<T>(FormatSet<T> lookup) where T : class
        {
            var formats = OleUtil.EnumFormatsInDataObject(_data).ToDictionary(f => f.cfFormat, f => f);

            foreach (var p in lookup)
            {
                if (formats.TryGetValue(p.Format.Id, out var fmt))
                {
                    var value = OleUtil.GetOleData<T>(_data, fmt.cfFormat, p.Reader);
                    if (value != null)
                    {
                        var cl = ClipboardFormat.GetFormat(fmt.cfFormat);
                        return value;
                    }
                }
            }

            return null;
        }

        //public void SetData(string format, object data)
        //{
        //    //SetData(format, data, true, DVASPECT.DVASPECT_CONTENT, 0)
        //}

        //public BitmapSource GetImage()
        //{
        //    EnumFORMATETC
        //    _data.EnumFormatEtc
        //    _data.GetData()
        //}

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
