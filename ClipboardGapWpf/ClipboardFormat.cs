using ClipboardGapWpf.Formats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ClipboardGapWpf
{
    public class ClipboardFormat : IEquatable<ClipboardFormat>
    {
        public static ClipboardFormat[] Formats => _lookup.Values.ToArray();
        private static readonly Dictionary<uint, ClipboardFormat> _lookup = new Dictionary<uint, ClipboardFormat>();

        private const uint
            CF_TEXT = 1,
            CF_BITMAP = 2,
            CF_METAFILEPICT = 3,
            CF_SYLK = 4,
            CF_DIF = 5,
            CF_TIFF = 6,
            CF_OEMTEXT = 7,
            CF_DIB = 8,
            CF_PALETTE = 9,
            CF_PENDATA = 10,
            CF_RIFF = 11,
            CF_WAVE = 12,
            CF_UNICODETEXT = 13,
            CF_ENHMETAFILE = 14,
            CF_HDROP = 15,
            CF_LOCALE = 16,
            CF_DIBV5 = 17,
            CF_PRIVATEFIRST = 0x0200,
            CF_PRIVATELAST = 0x02FF,
            CF_GDIOBJFIRST = 0x0300, // a handle allocated by the GlobalAlloc function with the GMEM_MOVEABLE flag. automatically deletes the object using the GlobalFree function.
            CF_GDIOBJLAST = 0x03FF;

        // STANDARD FORMATS
        public static readonly ClipboardFormat<string> Text = CreateFormat(CF_TEXT, "Text", new TextAnsi());
        public static readonly ClipboardFormat Bitmap = CreateFormat(CF_BITMAP, "Bitmap");
        public static readonly ClipboardFormat MetafilePict = CreateFormat(CF_METAFILEPICT, "MetaFilePict");
        public static readonly ClipboardFormat SymbolicLink = CreateFormat(CF_SYLK, "SymbolicLink");
        public static readonly ClipboardFormat DataInterchangeFormat = CreateFormat(CF_DIF, "DataInterchangeFormat");
        public static readonly ClipboardFormat<BitmapSource> Tiff = CreateFormat(CF_TIFF, "TaggedImageFileFormat", new ImageWpfBasicEncoderTiff());
        public static readonly ClipboardFormat<string> OemText = CreateFormat(CF_OEMTEXT, "OEMText", new TextAnsi());
        public static readonly ClipboardFormat<BitmapSource> Dib = CreateFormat(CF_DIB, "DeviceIndependentBitmap", new ImageWpfDib());
        public static readonly ClipboardFormat Palette = CreateFormat(CF_PALETTE, "Palette");
        public static readonly ClipboardFormat PenData = CreateFormat(CF_PENDATA, "PenData");
        public static readonly ClipboardFormat RiffAudio = CreateFormat(CF_RIFF, "RiffAudio");
        public static readonly ClipboardFormat WaveAudio = CreateFormat(CF_WAVE, "WaveAudio");
        public static readonly ClipboardFormat<string> UnicodeText = CreateFormat(CF_UNICODETEXT, "UnicodeText", new TextUnicode());
        public static readonly ClipboardFormat EnhancedMetafile = CreateFormat(CF_ENHMETAFILE, "EnhancedMetafile");
        public static readonly ClipboardFormat<string[]> FileDrop = CreateFormat(CF_HDROP, "FileDrop", new FileDrop());
        public static readonly ClipboardFormat<CultureInfo> Locale = CreateFormat(CF_LOCALE, "Locale", new Locale());
        public static readonly ClipboardFormat DibV5 = CreateFormat(CF_DIBV5, "Format17");

        // CUSTOM FORMATS
        public static readonly ClipboardFormat<string> Html = CreateFormat("HTML Format", new TextUtf8());
        public static readonly ClipboardFormat<string> Rtf = CreateFormat("Rich Text Format", new TextAnsi());
        public static readonly ClipboardFormat<string> Csv = CreateFormat("CSV", new TextAnsi());
        public static readonly ClipboardFormat<string> Xaml = CreateFormat("Xaml", new TextUtf8());
        public static readonly ClipboardFormat<BitmapSource> Jpg = CreateFormat("JPG", new ImageWpfBasicEncoderJpeg());
        public static readonly ClipboardFormat<BitmapSource> Jpeg = CreateFormat("JPEG", new ImageWpfBasicEncoderJpeg());
        public static readonly ClipboardFormat<BitmapSource> Jfif = CreateFormat("Jfif", new ImageWpfBasicEncoderJpeg());
        public static readonly ClipboardFormat<BitmapSource> Gif = CreateFormat("Gif", new ImageWpfBasicEncoderGif());
        public static readonly ClipboardFormat<BitmapSource> Png = CreateFormat("PNG", new ImageWpfBasicEncoderPng());
        public static readonly ClipboardFormat<DragDropEffects> DropEffect = CreateFormat("Preferred DropEffect", new DropEffect());
        [Obsolete] public static readonly ClipboardFormat<string> FileName = CreateFormat("FileName", new TextAnsi());
        [Obsolete] public static readonly ClipboardFormat<string> FileNameW = CreateFormat("FileNameW", new TextUnicode());

        public uint Id { get; }
        public string Name { get; }

        public ClipboardFormat(uint std, string name)
        {
            Id = std;
            Name = name;
            _lookup.Add(std, this);
        }

        private static ClipboardFormat<T> CreateFormat<T>(uint formatId, string formatName, IDataConverter<T> formats)
        {
            return new ClipboardFormat<T>(formatId, formatName, formats);
        }

        private static ClipboardFormat CreateFormat(uint formatId, string formatName)
        {
            return new ClipboardFormat(formatId, formatName);
        }

        private static ClipboardFormat CreateFormat(uint formatId)
        {
            StringBuilder sb = new StringBuilder(255);

            var len = NativeMethods.GetClipboardFormatName(formatId, sb, 255);
            if (len == 0)
                throw new Win32Exception();

            return CreateFormat(formatId, sb.ToString());
        }

        private static ClipboardFormat<T> CreateFormat<T>(string formatName, IDataConverter<T> formats)
        {
            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            if (formatId == 0)
                throw new Win32Exception();

            return CreateFormat(formatId, formatName, formats);
        }

        //private static ClipboardFormat CreateFormat(string formatName)
        //{
        //    // If a registered format with the specified name already exists, a new format is not registered and the return value identifies the existing format. 
        //    // This enables more than one application to copy and paste data using the same registered clipboard format. Note that the format name comparison is case-insensitive.

        //    var formatId = NativeMethods.RegisterClipboardFormat(formatName);
        //    if (formatId == 0)
        //        throw new Win32Exception();

        //    return CreateFormat(formatId);
        //}

        public static ClipboardFormat GetFormat(uint formatId)
        {
            if (_lookup.TryGetValue(formatId, out var std))
                return std;

            return CreateFormat(formatId);
        }

        public static ClipboardFormat GetFormat(string formatName)
        {
            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            if (formatId == 0)
                throw new Win32Exception();

            return GetFormat(formatId);
        }

        public override bool Equals(object obj)
        {
            if (obj is ClipboardFormat other) return Equals(other);
            return false;
        }

        public override string ToString() => $"Id={Id}, Name={Name}";

        public override int GetHashCode() => Id.GetHashCode();

        public bool Equals(ClipboardFormat other) => other.Id == Id;
    }

    public class ClipboardFormat<T> : ClipboardFormat
    {
        public IDataConverter<T> ObjectParserTyped { get; }
        public ClipboardFormat(uint std, string name, IDataConverter<T> formats) : base(std, name)
        {
            ObjectParserTyped = formats;
        }
    }
}
