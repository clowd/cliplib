using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClipboardGapWpf.Formats
{
    public class ImageWpfPng : IFormatStreamReader<BitmapSource>
    {
        public BitmapSource ReadFromStream(Stream stream)
        {
            PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapSource bitmapSource = decoder.Frames[0];
            return bitmapSource;
        }
    }
}
