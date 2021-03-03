using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClipboardGapWpf.Formats
{

    public abstract class ImageWpfBasicEncoder : IDataStreamReader<BitmapSource>, IDataStreamWriter<BitmapSource>
    {
        public BitmapSource ReadFromStream(Stream stream)
        {
            var decoder = GetDecoder(stream);
            BitmapSource bitmapSource = decoder.Frames[0];
            return bitmapSource;
        }

        public void WriteToStream(BitmapSource obj, Stream stream)
        {
            var encoder = GetEncoder();
            if (obj is BitmapFrame frame) encoder.Frames.Add(frame);
            else encoder.Frames.Add(BitmapFrame.Create(obj));
            encoder.Save(stream);
        }

        protected abstract BitmapDecoder GetDecoder(Stream stream);
        protected abstract BitmapEncoder GetEncoder();
    }

    public class ImageWpfBasicEncoderPng : ImageWpfBasicEncoder
    {
        protected override BitmapDecoder GetDecoder(Stream stream) => new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        protected override BitmapEncoder GetEncoder() => new PngBitmapEncoder();
    }

    public class ImageWpfBasicEncoderJpeg : ImageWpfBasicEncoder
    {
        protected override BitmapDecoder GetDecoder(Stream stream) => new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        protected override BitmapEncoder GetEncoder() => new JpegBitmapEncoder();
    }

    public class ImageWpfBasicEncoderGif : ImageWpfBasicEncoder
    {
        protected override BitmapDecoder GetDecoder(Stream stream) => new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        protected override BitmapEncoder GetEncoder() => new GifBitmapEncoder();
    }

    public class ImageWpfBasicEncoderTiff : ImageWpfBasicEncoder
    {
        protected override BitmapDecoder GetDecoder(Stream stream) => new TiffBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        protected override BitmapEncoder GetEncoder() => new TiffBitmapEncoder();
    }
}
