using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClipboardGapWpf.Formats
{
    class ImageWpfDib : BytesDataConverterBase<BitmapSource>
    {
        // https://docs.microsoft.com/en-us/windows/win32/gdi/bitmap-header-types
        // https://gitlab.idiap.ch/bob/bob.io.image/blob/c7ee46c80ae24b9e74cbf8ff76168605186271db/bob/io/image/bmp.cpp#L866
        // https://en.wikipedia.org/wiki/BMP_file_format#/media/File:BMPfileFormat.png
        // https://en.wikipedia.org/wiki/BMP_file_format
        // https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapv5header
        // http://fileformats.archiveteam.org/wiki/BMP
        // https://medium.com/sysf/bits-to-bitmaps-a-simple-walkthrough-of-bmp-image-format-765dc6857393
        // http://entropymine.com/jason/bmpsuite/bmpsuite/html/bmpsuite.html
        // https://github.com/dacap/clip/blob/main/clip_win.cpp

        const int
            BFH_SIZE = 14,
            BMPV1_SIZE = 40,
            BMPV4_SIZE = 108,
            BMPV5_SIZE = 124;

        public override BitmapSource ReadFromBytes(byte[] buffer)
        {
            var header = StructUtil.Deserialize<BITMAPINFOHEADER>(buffer, 0);
            var fileSize = BFH_SIZE + buffer.Length;

            if (header.bV5Size != BMPV1_SIZE)
                throw new NotSupportedException($"BMP header size was '{header.bV5Size}', expected '{BMPV1_SIZE}'.");

            var file = new BITMAPFILEHEADER();
            file.bfType = 0x4d42; // "BM"
            file.bfSize = (uint)fileSize;
            file.bfReserved1 = 0;
            file.bfReserved2 = 0;
            file.bfOffBits = BFH_SIZE + header.bV5Size + header.bV5ClrUsed * 4;

            var fileBytes = StructUtil.Serialize(file);

            var ms = new MemoryStream(fileBytes.Length + fileBytes.Length);
            ms.Write(fileBytes, 0, fileBytes.Length);
            ms.Write(buffer, 0, buffer.Length);
            ms.Seek(0, SeekOrigin.Begin);

            var bitmap = BitmapFrame.Create(ms);
            return bitmap;
        }

        public override byte[] WriteToBytes(BitmapSource obj)
        {
            var encoder = new BmpBitmapEncoder();
            if (obj is BitmapFrame frame) encoder.Frames.Add(frame);
            else encoder.Frames.Add(BitmapFrame.Create(obj));

            MemoryStream ms = new MemoryStream();
            encoder.Save(ms);
            var buffer = ms.GetBuffer();

            var header = StructUtil.Deserialize<BITMAPINFOHEADER>(buffer, BFH_SIZE);

            if (header.bV5Size != BMPV1_SIZE)
                throw new NotSupportedException($"BMP header size was '{header.bV5Size}', expected '{BMPV1_SIZE}'.");

            var outputSize = buffer.Length - BFH_SIZE;
            var output = new byte[outputSize];
            Buffer.BlockCopy(buffer, BFH_SIZE, output, 0, outputSize);

            return output;
        }
    }
}
