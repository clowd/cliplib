using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClipboardGapWpf.Formats
{
    public class ImageWpfDibV5 : BytesDataConverterBase<BitmapSource>
    {
        private const ushort BFH_BM = 0x4D42;

        // http://paulbourke.net/dataformats/bitmaps/
        // http://www.libertybasicuniversity.com/lbnews/nl100/format.htm
        // https://www.displayfusion.com/Discussions/View/converting-c-data-types-to-c/?ID=38db6001-45e5-41a3-ab39-8004450204b3

        // https://github.com/FlyingPumba/tp2-orga2/blob/master/entregable/src/bmp/bmp.c
        static int calc_shift(uint mask)
        {
            int result = 0;
            while ((mask & 1) == 0)
            {
                result++;
                mask >>= 1;
            }
            return result;
        }

        static int pixelPerMeterToDpi(int pels)
        {
            return (int)(pels * 0.0254d);
        }

        public unsafe override BitmapSource ReadFromBytes(byte[] data)
        {
            bool hasFileHeader = BitConverter.ToUInt16(data, 0) == BFH_BM;
            var size_fh = Marshal.SizeOf<BITMAPFILEHEADER>();
            var size_bi = Marshal.SizeOf<BITMAPV5HEADER>();

            //if (hasFileHeader)
            //{
            //    var fh = StructUtil.Deserialize<BITMAPFILEHEADER>(data, 0);
            //}

            int offset = hasFileHeader ? size_fh : 0;
            var bi = StructUtil.Deserialize<BITMAPV5HEADER>(data, offset);
            offset += size_bi;

            int nbits = bi.bV5BitCount;

            if (bi.bV5Planes != 1)
                throw new NotSupportedException($"Bitmap bV5Planes of '{bi.bV5Planes}' is not supported.");

            switch (bi.bV5Compression)
            {
                case BitmapCompressionMode.BI_BITFIELDS:


                    break;
                case BitmapCompressionMode.BI_ALPHABITFIELDS:
                    break;
                case BitmapCompressionMode.BI_RGB:
                    break;
                default:
                    throw new NotSupportedException("bV5Compression");
            }

            if (nbits != 32 || bi.bV5Planes != 1 || comp != BitmapCompressionMode.BI_BITFIELDS)
                throw new NotSupportedException(); // not supported

            var width = bi.bV5Width;
            var height = bi.bV5Height;
            bool upsideDown = false;
            if (height < 0)
            {
                height = -height;
                upsideDown = true;
            }

            int stride = width * 4; // always 32bpp

            uint maskR = bi.bV5RedMask;
            uint maskG = bi.bV5GammaGreen;
            uint maskB = bi.bV5BlueMask;
            uint maskA = bi.bV5AlphaMask;

            int shiftR = calc_shift(bi.bV5RedMask);
            int shiftG = calc_shift(bi.bV5GreenMask);
            int shiftB = calc_shift(bi.bV5GammaBlue);
            int shiftA = calc_shift(bi.bV5AlphaMask);

            var size_colors = sizeof(uint) * 3;
            offset += size_colors;

            var bitmap = new WriteableBitmap(
                bi.bV5Width,
                bi.bV5Height,
                pixelPerMeterToDpi(bi.bV5XPelsPerMeter),
                pixelPerMeterToDpi(bi.bV5YPelsPerMeter),
                PixelFormats.Bgra32,
                BitmapPalettes.Halftone256Transparent);


            if (bitmap.BackBufferStride != stride)
                throw new InvalidOperationException();

            bitmap.Lock();

            fixed (byte* bufferStart = data)
            {
                uint* dest = (uint*)bitmap.BackBuffer;
                uint* source = (uint*)(bufferStart + offset);
                var h = height;
                while (--h >= 0)
                {
                    var w = width;
                    while (--w >= 0)
                    {
                        // TO BGRA
                        var b = (*source & maskB) << shiftB;
                        var g = (*source & maskG) << shiftG;
                        var r = (*source & maskR) << shiftR;
                        var a = (*source & maskA) << shiftA;

                        source += 1;
                        dest += 1;
                    }
                }
            }

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            bitmap.Unlock();
            return bitmap;


            //uint* buffer = (uint*)bitmap.BackBuffer;
            //while (--height >= 0)
            //{

            //}
        }

        public override byte[] WriteToBytes(BitmapSource obj)
        {
            return WriteToBytes(obj, false);
        }

        public byte[] WriteToBytes(BitmapSource obj, bool writeFileHeader)
        {
            FormatConvertedBitmap bitmap = new FormatConvertedBitmap(obj, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent, 1);

            //FormatConvertedBitmap inverseOpacityMaskBitmap = new FormatConvertedBitmap();
            //inverseOpacityMaskBitmap.BeginInit();
            //inverseOpacityMaskBitmap.Source = _rendered;
            //inverseOpacityMaskBitmap.DestinationFormat = PixelFormats.Bgra32;
            //inverseOpacityMaskBitmap.EndInit();

            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int stride = width * 4; // always 32bpp

            var size_fh = Marshal.SizeOf<BITMAPFILEHEADER>();
            var size_bi = Marshal.SizeOf<BITMAPV5HEADER>();
            var size_colors = sizeof(uint) * 3;
            var size_pixels = stride * height;

            var fh = new BITMAPFILEHEADER
            {
                bfType = BFH_BM,
                bfOffBits = (uint)(size_fh + size_bi + size_colors),
                bfSize = (uint)(size_fh + size_bi + size_colors + size_pixels),
            };

            uint mskB = 0xFF000000;
            uint mskG = 0x00FF0000;
            uint mskR = 0x0000FF00;
            uint mskA = 0x000000FF;

            var bi = new BITMAPV5HEADER
            {
                bV5Size = (uint)size_bi,
                bV5Width = width,
                bV5Height = height,
                bV5Planes = 1,
                bV5BitCount = 32,
                bV5Compression = BitmapCompressionMode.BI_BITFIELDS,
                bV5XPelsPerMeter = 0,
                bV5YPelsPerMeter = 0,
                bV5ClrUsed = 0,
                bV5ClrImportant = 0,
                bV5BlueMask = mskB,
                bV5GreenMask = mskG,
                bV5RedMask = mskR,
                bV5AlphaMask = mskA,
                bV5CSType = 0x73524742, // LCS_sRGB
                bV5Intent = 4, // LCS_GM_IMAGES
                bV5SizeImage = (uint)size_pixels,
            };

            uint[] colorSpace = new uint[] { mskR, mskG, mskB };

            byte[] buffer;
            int offset = 0;

            // write file header maybe
            if (writeFileHeader)
            {
                buffer = new byte[fh.bfSize];
                StructUtil.SerializeTo(fh, buffer, ref offset);
            }
            else
            {
                buffer = new byte[size_bi + size_colors + size_pixels];
            }

            // write info header
            StructUtil.SerializeTo(bi, buffer, ref offset);

            // write color data
            Buffer.BlockCopy(colorSpace, 0, buffer, offset, size_colors);
            offset += size_colors;

            // copy pixels
            bitmap.CopyPixels(buffer, stride, offset);

            return buffer;
        }
    }
}
