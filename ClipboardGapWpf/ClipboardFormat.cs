using ClipboardGapWpf.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf
{
    public class ClipboardFormat : IEquatable<ClipboardFormat>
    {
        public static ClipboardFormat[] Formats => _lookup.Values.ToArray();
        private static readonly Dictionary<short, ClipboardFormat> _lookup = new Dictionary<short, ClipboardFormat>();

        private const int
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
        public static readonly ClipboardFormat Text = CreateFormat(CF_TEXT, "Text", new TextAnsi());
        public static readonly ClipboardFormat Bitmap = CreateFormat(CF_BITMAP, "Bitmap");
        public static readonly ClipboardFormat MetafilePict = CreateFormat(CF_METAFILEPICT, "MetaFilePict");
        public static readonly ClipboardFormat SymbolicLink = CreateFormat(CF_SYLK, "SymbolicLink");
        public static readonly ClipboardFormat DataInterchangeFormat = CreateFormat(CF_DIF, "DataInterchangeFormat");
        public static readonly ClipboardFormat Tiff = CreateFormat(CF_TIFF, "TaggedImageFileFormat", new ImageWpfBasicEncoderTiff());
        public static readonly ClipboardFormat OemText = CreateFormat(CF_OEMTEXT, "OEMText", new TextAnsi());
        public static readonly ClipboardFormat Dib = CreateFormat(CF_DIB, "DeviceIndependentBitmap", new ImageWpfDib());
        public static readonly ClipboardFormat Palette = CreateFormat(CF_PALETTE, "Palette");
        public static readonly ClipboardFormat PenData = CreateFormat(CF_PENDATA, "PenData");
        public static readonly ClipboardFormat RiffAudio = CreateFormat(CF_RIFF, "RiffAudio");
        public static readonly ClipboardFormat WaveAudio = CreateFormat(CF_WAVE, "WaveAudio");
        public static readonly ClipboardFormat UnicodeText = CreateFormat(CF_UNICODETEXT, "UnicodeText", new TextUnicode());
        public static readonly ClipboardFormat EnhancedMetafile = CreateFormat(CF_ENHMETAFILE, "EnhancedMetafile");
        public static readonly ClipboardFormat FileDrop = CreateFormat(CF_HDROP, "FileDrop", new FileDrop());
        public static readonly ClipboardFormat Locale = CreateFormat(CF_LOCALE, "Locale", new Locale());
        public static readonly ClipboardFormat DibV5 = CreateFormat(CF_DIBV5, "Format17");

        // CUSTOM FORMATS
        public static readonly ClipboardFormat Html = CreateFormat("HTML Format", new TextUtf8());
        public static readonly ClipboardFormat Rtf = CreateFormat("Rich Text Format", new TextAnsi());
        public static readonly ClipboardFormat Csv = CreateFormat("CSV", new TextAnsi());
        public static readonly ClipboardFormat Xaml = CreateFormat("Xaml", new TextUtf8());
        public static readonly ClipboardFormat Jpg = CreateFormat("JPG", new ImageWpfBasicEncoderJpeg());
        public static readonly ClipboardFormat Jpeg = CreateFormat("JPEG", new ImageWpfBasicEncoderJpeg());
        public static readonly ClipboardFormat Jfif = CreateFormat("Jfif", new ImageWpfBasicEncoderJpeg());
        public static readonly ClipboardFormat Gif = CreateFormat("Gif", new ImageWpfBasicEncoderGif());
        public static readonly ClipboardFormat Png = CreateFormat("PNG", new ImageWpfBasicEncoderPng());
        public static readonly ClipboardFormat DropEffect = CreateFormat("Preferred DropEffect", new DropEffect());
        [Obsolete] public static readonly ClipboardFormat FileName = CreateFormat("FileName", new TextAnsi());
        [Obsolete] public static readonly ClipboardFormat FileNameW = CreateFormat("FileNameW", new TextUnicode());

        public short Id { get; }
        public string Name { get; }
        public List<IDataConverter> Converters { get; }

        private ClipboardFormat(short std, string name, IEnumerable<IDataConverter> formats)
        {
            Id = std;
            Name = name;
            Converters = formats?.ToList() ?? new List<IDataConverter>();
            _lookup.Add(std, this);
        }

        internal IEnumerable<IDataReader<T>> GetReadersForType<T>()
        {
            foreach (var c in Converters.Concat(new[] { new Binary() }))
            {
                if (c is IDataReader<T> r) yield return r;
            }
        }

        internal IEnumerable<IDataWriter<T>> GetWritersForType<T>()
        {
            foreach (var c in Converters.Concat(new[] { new Binary() }))
            {
                if (c is IDataWriter<T> r) yield return r;
            }
        }

        private static ClipboardFormat CreateFormat(short formatId, string formatName, params IDataConverter[] formats)
        {
            return new ClipboardFormat(formatId, formatName, formats);
        }

        private static ClipboardFormat CreateFormat(short formatId, params IDataConverter[] formats)
        {
            StringBuilder sb = new StringBuilder(255);
            NativeMethods.GetClipboardFormatName(formatId, sb, 255);
            return CreateFormat(formatId, sb.ToString(), formats);
        }

        private static ClipboardFormat CreateFormat(string formatName, params IDataConverter[] formats)
        {
            // If a registered format with the specified name already exists, a new format is not registered and the return value identifies the existing format. 
            // This enables more than one application to copy and paste data using the same registered clipboard format. Note that the format name comparison is case-insensitive.

            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            return CreateFormat((short)formatId, formats);
        }

        public static ClipboardFormat GetFormat(short formatId)
        {
            if (_lookup.TryGetValue(formatId, out var std))
                return std;
            return CreateFormat(formatId);
        }

        public static ClipboardFormat GetFormat(string formatName)
        {
            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            return GetFormat((short)formatId);
        }

        internal static ClipboardFormat GetFormat(FORMATETC format)
        {
            return GetFormat(format.cfFormat);
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
}
