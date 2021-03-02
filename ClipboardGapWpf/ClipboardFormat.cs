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
        public static readonly ClipboardFormat Text = new ClipboardFormat(CF_TEXT, "Text");
        public static readonly ClipboardFormat Bitmap = new ClipboardFormat(CF_BITMAP, "Bitmap");
        public static readonly ClipboardFormat MetafilePict = new ClipboardFormat(CF_METAFILEPICT, "MetaFilePict");
        public static readonly ClipboardFormat SymbolicLink = new ClipboardFormat(CF_SYLK, "SymbolicLink");
        public static readonly ClipboardFormat DataInterchangeFormat = new ClipboardFormat(CF_DIF, "DataInterchangeFormat");
        public static readonly ClipboardFormat Tiff = new ClipboardFormat(CF_TIFF, "TaggedImageFileFormat");
        public static readonly ClipboardFormat OemText = new ClipboardFormat(CF_OEMTEXT, "OEMText");
        public static readonly ClipboardFormat DIB = new ClipboardFormat(CF_DIB, "DeviceIndependentBitmap");
        public static readonly ClipboardFormat Palette = new ClipboardFormat(CF_PALETTE, "Palette");
        public static readonly ClipboardFormat PenData = new ClipboardFormat(CF_PENDATA, "PenData");
        public static readonly ClipboardFormat RiffAudio = new ClipboardFormat(CF_RIFF, "RiffAudio");
        public static readonly ClipboardFormat WaveAudio = new ClipboardFormat(CF_WAVE, "WaveAudio");
        public static readonly ClipboardFormat UnicodeText = new ClipboardFormat(CF_UNICODETEXT, "UnicodeText");
        public static readonly ClipboardFormat EnhancedMetafile = new ClipboardFormat(CF_ENHMETAFILE, "EnhancedMetafile");
        public static readonly ClipboardFormat FileDrop = new ClipboardFormat(CF_HDROP, "FileDrop");
        public static readonly ClipboardFormat Locale = new ClipboardFormat(CF_LOCALE, "Locale");
        public static readonly ClipboardFormat DIBV5 = new ClipboardFormat(CF_DIBV5, "Format17");

        // CUSTOM FORMATS
        public static readonly ClipboardFormat Html = GetFormat("HTML Format");
        public static readonly ClipboardFormat Rtf = GetFormat("Rich Text Format");
        public static readonly ClipboardFormat Csv = GetFormat("CSV");
        public static readonly ClipboardFormat Jpg = GetFormat("JPG");
        public static readonly ClipboardFormat Jpeg = GetFormat("JPEG");
        public static readonly ClipboardFormat Jfif = GetFormat("Jfif");
        public static readonly ClipboardFormat Gif = GetFormat("Gif");
        public static readonly ClipboardFormat Png = GetFormat("PNG");
        [Obsolete] public static readonly ClipboardFormat FileName = GetFormat("FileName");
        [Obsolete] public static readonly ClipboardFormat FileNameW = GetFormat("FileNameW");


        public static readonly ClipboardFormat[] Formats = new ClipboardFormat[]
        {
            Text,
            Bitmap,
            MetafilePict,
            SymbolicLink,
            DataInterchangeFormat,
            Tiff,
            OemText,
            DIB,
            Palette,
            PenData,
            RiffAudio,
            WaveAudio,
            UnicodeText,
            EnhancedMetafile,
            FileDrop,
            Locale,
            DIBV5,
            Html,
            Rtf,
            Csv,
            Jpg,
            Jpeg,
            Jfif,
            Gif,
        };

        private static readonly Dictionary<short, ClipboardFormat> _lookup = Formats.ToDictionary(f => f.Id, f => f);

        public short Id { get; }
        public string Name { get; }

        private ClipboardFormat(short std, string name)
        {
            Id = std;
            Name = name;
        }

        public static ClipboardFormat GetFormat(string formatName)
        {
            // If a registered format with the specified name already exists, a new format is not registered and the return value identifies the existing format. 
            // This enables more than one application to copy and paste data using the same registered clipboard format. Note that the format name comparison is case-insensitive.

            var formatId = NativeMethods.RegisterClipboardFormat(formatName);
            StringBuilder sb = new StringBuilder(255);
            NativeMethods.GetClipboardFormatName(formatId, sb, 255);
            return new ClipboardFormat((short)formatId, sb.ToString());
        }

        public static ClipboardFormat GetFormat(short formatId)
        {
            if (_lookup.TryGetValue(formatId, out var std))
                return std;

            StringBuilder sb = new StringBuilder(255);
            NativeMethods.GetClipboardFormatName(formatId, sb, 255);
            return new ClipboardFormat(formatId, sb.ToString());
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
