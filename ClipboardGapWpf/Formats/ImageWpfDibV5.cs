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

        private const uint
            LCS_CALIBRATED_RGB = 0,
            LCS_sRGB = 0x73524742, // 'sRGB'
            LCS_WINDOWS_COLOR_SPACE = 0x57696e20, // 'Win '
            LCS_GM_IMAGES = 4;

        // http://paulbourke.net/dataformats/bitmaps/
        // http://www.libertybasicuniversity.com/lbnews/nl100/format.htm
        // https://www.displayfusion.com/Discussions/View/converting-c-data-types-to-c/?ID=38db6001-45e5-41a3-ab39-8004450204b3

        // https://github.com/FlyingPumba/tp2-orga2/blob/master/entregable/src/bmp/bmp.c
        static int calc_shift(uint mask)
        {

            //uint v = mask;      // 32-bit word input to count zero bits on right
            //int c = 32; // c will be the number of zero bits on the right
            //v &= -(v);
            //if (v != 0) c--;
            //if ((v & 0x0000FFFF) != 0) c -= 16;
            //if ((v & 0x00FF00FF) != 0) c -= 8;
            //if ((v & 0x0F0F0F0F) != 0) c -= 4;
            //if ((v & 0x33333333) != 0) c -= 2;
            //if ((v & 0x55555555) != 0) c -= 1;

            for (int shift = 0; shift < sizeof(uint) * 8; ++shift)
            {
                if ((mask & (1 << shift)) != 0)
                {
                    return shift;
                }
            }
            throw new NotSupportedException("Invalid Bit Mask");

            //int result = 0;
            //while ((mask & 1) == 0)
            //{
            //    result++;
            //    mask >>= 1;

            //    if (result > 255)
            //        throw new NotSupportedException("Invalid Bit Mask");
            //}
            //return result;
        }

        static int pixelPerMeterToDpi(int pels)
        {
            if (pels == 0)
                return 96;

            return (int)(pels * 0.0254d);
        }

        public unsafe override BitmapSource ReadFromBytes(byte[] data)
        {
            fixed (byte* bufferStart = data)
            {
                bool hasFileHeader = BitConverter.ToUInt16(data, 0) == BFH_BM;
                var size_fh = Marshal.SizeOf<BITMAPFILEHEADER>();
                var size_bi = Marshal.SizeOf<BITMAPINFOHEADER>();
                var size_biv5 = Marshal.SizeOf<BITMAPV5HEADER>();

                int offset = 0;
                var fh = default(BITMAPFILEHEADER);
                if (hasFileHeader)
                    fh = StructUtil.Deserialize<BITMAPFILEHEADER>(bufferStart, ref offset);

                var bi = StructUtil.Deserialize<BITMAPINFOHEADER>(data, offset);
                BITMAPV5HEADER biv5 = default(BITMAPV5HEADER);
                bool isv5 = false;

                if (bi.bV5Size != size_bi && bi.bV5Size != size_biv5)
                    throw new NotSupportedException($"Bitmap header of size '{bi.bV5Size}' is not supported. Expected {size_bi} or {size_biv5}.");

                if (bi.bV5Size == size_biv5)
                {
                    isv5 = true;
                    biv5 = StructUtil.Deserialize<BITMAPV5HEADER>(data, offset);
                }

                offset += (int)bi.bV5Size;

                int nbits = bi.bV5BitCount;

                //if (nbits != 24 && nbits != 32 && nbits != 16)
                //    throw new NotSupportedException($"Bitmaps with bpp of '{nbits}' are not supported. Expected 16, 24, or 32.");

                if (bi.bV5Planes != 1)
                    throw new NotSupportedException($"Bitmap bV5Planes of '{bi.bV5Planes}' is not supported.");

                if (isv5 && (biv5.bV5CSType != LCS_sRGB && biv5.bV5CSType != LCS_WINDOWS_COLOR_SPACE))
                    throw new NotSupportedException($"Bitmap with color space type of '{biv5.bV5CSType}' is not supported.");

                uint maskR = 0;
                uint maskG = 0;
                uint maskB = 0;
                uint maskA = 0;

                bool hasAlphaChannel = false, hasRgbMask = false;

                switch (bi.bV5Compression)
                {
                    case BitmapCompressionMode.BI_BITFIELDS:
                        // seems that v5 bitmaps with >= 16 bits do not have a color table, despite BI_BITFIELDS
                        // saying it should
                        if (nbits < 16 || !isv5)
                        {
                            maskR = BitConverter.ToUInt32(data, offset);
                            offset += sizeof(uint);
                            maskG = BitConverter.ToUInt32(data, offset);
                            offset += sizeof(uint);
                            maskB = BitConverter.ToUInt32(data, offset);
                            offset += sizeof(uint);
                            hasRgbMask = true;
                        }
                        break;
                    case BitmapCompressionMode.BI_ALPHABITFIELDS:
                        maskR = BitConverter.ToUInt32(data, offset);
                        offset += sizeof(uint);
                        maskG = BitConverter.ToUInt32(data, offset);
                        offset += sizeof(uint);
                        maskB = BitConverter.ToUInt32(data, offset);
                        offset += sizeof(uint);
                        maskA = BitConverter.ToUInt32(data, offset);
                        offset += sizeof(uint);
                        hasAlphaChannel = hasRgbMask = true;
                        break;
                    case BitmapCompressionMode.BI_RGB:
                        switch (nbits)
                        {
                            case 32:
                                // windows wrongly uses the 4th byte of BI_RGB 32bit dibs as alpha
                                // but we need to do it too if we have any hope of reading alpha data
                                maskR = 0xff0000;
                                maskG = 0xff00;
                                maskB = 0xff;
                                maskA = 0xff000000;
                                hasRgbMask = true;
                                break;
                            case 24:
                                maskR = 0xff0000;
                                maskG = 0xff00;
                                maskB = 0xff;
                                hasRgbMask = true;
                                break;
                            case 16:
                                maskR = 0x7c00;
                                maskG = 0x03e0;
                                maskB = 0x001f;
                                maskA = 0x8000; // fake transparency?
                                hasRgbMask = true;
                                break;
                        }
                        break;
                    default:
                        throw new NotSupportedException($"Bitmap with bV5Compression of '{bi.bV5Compression.ToString()}' is not supported.");
                }

                // The number of entries in the palette is either 2n (where n is the number of bits per pixel) or a smaller number specified in the header
                RGBQUAD[] palette = new RGBQUAD[0];

                if (bi.bV5ClrUsed > 0 || nbits < 16)
                {
                    palette = new RGBQUAD[bi.bV5ClrUsed != 0 ? (int)bi.bV5ClrUsed : (1 << nbits)];
                    for (int i = 0; i < palette.Length; i++)
                    {
                        palette[i] = StructUtil.Deserialize<RGBQUAD>(bufferStart, ref offset);
                    }
                }

                if (palette.Length > Math.Pow(2, nbits))
                    throw new NotSupportedException("Bitmap palette invalid.");

                if (palette.Length > 256)
                    throw new NotSupportedException("Bitmap palette with more than 256 colors are not supported.");

                if (isv5)
                {
                    // lets use the v5 masks if present as this seems to be typical
                    if (biv5.bV5RedMask != 0) maskR = biv5.bV5RedMask;
                    if (biv5.bV5BlueMask != 0) maskB = biv5.bV5BlueMask;
                    if (biv5.bV5GreenMask != 0) maskG = biv5.bV5GreenMask;
                    if (biv5.bV5AlphaMask != 0)
                    {
                        maskA = biv5.bV5AlphaMask;
                        hasAlphaChannel = true;
                    }

                    if (maskR != 0 && maskB != 0 && maskG != 0)
                        hasRgbMask = true;
                }

                // For RGB DIBs, the image orientation is indicated by the biHeight member of the BITMAPINFOHEADER structure. 
                // If biHeight is positive, the image is bottom-up. If biHeight is negative, the image is top-down.
                // DirectDraw uses top-down DIBs. In GDI, all DIBs are bottom-up. 
                // Also, any DIB type that uses a FOURCC in the biCompression member, should express its biHeight as a positive number 
                // no matter what its orientation is, since the FOURCC itself identifies a compression scheme whose image orientation 
                // should be understood by any compatible filter. Common YUV formats such as UYVY, YV12, and YUY2 are top-down oriented. 
                // It is invalid to store an image with these compression types in bottom-up orientation. 
                // The sign of biHeight for such formats must always be set positive

                var width = bi.bV5Width;
                var height = bi.bV5Height;
                bool upside_down = false;

                if (height < 0)
                {
                    height = -height;
                    upside_down = true;
                }

                int shiftR = 0, shiftG = 0, shiftB = 0, shiftA = 0;
                uint maxR = 0, maxG = 0, maxB = 0, maxA = 0;
                uint multR = 0, multG = 0, multB = 0, multA = 0;

                if (maskR != 0)
                {
                    shiftR = calc_shift(maskR);
                    maxR = maskR >> shiftR;
                    multR = (uint)(Math.Ceiling(255d / maxR * 65536 * 256)); // bitshift << 24
                }

                if (maskG != 0)
                {
                    shiftG = calc_shift(maskG);
                    maxG = maskG >> shiftG;
                    multG = (uint)(Math.Ceiling(255d / maxG * 65536 * 256));
                }

                if (maskB != 0)
                {
                    shiftB = calc_shift(maskB);
                    maxB = maskB >> shiftB;
                    multB = (uint)(Math.Ceiling(255d / maxB * 65536 * 256));
                }

                if (maskA != 0)
                {
                    shiftA = calc_shift(maskA);
                    maxA = maskA >> shiftA;
                    multA = (uint)(Math.Ceiling(255d / maxA * 65536 * 256)); // bitshift << 24
                }

                var bitmap = new WriteableBitmap(
                    bi.bV5Width,
                    bi.bV5Height,
                    pixelPerMeterToDpi(bi.bV5XPelsPerMeter),
                    pixelPerMeterToDpi(bi.bV5YPelsPerMeter),
                    PixelFormats.Bgra32,
                    BitmapPalettes.Halftone256Transparent);

                int source_stride = (nbits * width + 31) / 32 * 4; // = width * (nbits / 8) + (width % 4); // (width * nbits + 7) / 8;
                int dest_stride = bitmap.BackBufferStride;

                bitmap.Lock();

                //var test4 = new byte[data.Length - offset];
                //Buffer.BlockCopy(data, offset, test4, 0, data.Length - offset);
                //Console.WriteLine();
                //byte* dest = (byte*)bitmap.BackBuffer;
                //byte* source = (byte*)(bufferStart + offset);

                //var num_bytes = (nbits / 8);

                if (hasFileHeader && fh.bfOffBits != 0)
                    offset = (int)fh.bfOffBits;

                restartLoop:

                RGBQUAD color;
                int pal = palette.Length;
                int px;
                byte b, r, g, a;
                uint s32;
                byte s1;
                byte* dest, source;
                int w, h = height, nbytes = (nbits / 8);

                if (palette.Length > 0 && nbits == 1)
                {
                    while (--h >= 0)
                    {
                        dest = (byte*)(bitmap.BackBuffer + ((upside_down ? height - h - 1 : h) * dest_stride));
                        source = (byte*)(bufferStart + offset + ((height - h - 1) * source_stride));
                        for (int x = 0; x < source_stride - 1; x++)
                        {
                            s1 = *source++;
                            for (int bit = 7; bit >= 0; bit--)
                            {
                                var color = palette[(s1 & (1 << bit)) >> bit];
                                *(uint*)dest = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                                dest += 4;
                            }
                        }

                        // last bits in a row might not make up a whole byte
                        s1 = *source++;
                        for (int bit = 7; bit >= 8 - (width - ((source_stride - 1) * 8)); bit--)
                        {
                            var color = palette[(s1 & (1 << bit)) >> bit];
                            *(uint*)dest = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                            dest += 4;
                        }
                    }
                }
                else if (palette.Length > 0 && nbits == 2)
                {
                    var remain = width % 4;

                    while (--h >= 0)
                    {
                        var destu = (uint*)(bitmap.BackBuffer + ((upside_down ? height - h - 1 : h) * dest_stride));
                        source = (byte*)(bufferStart + offset + ((height - h - 1) * source_stride));

                        for (int x = 0; x < source_stride - remain; x++)
                        {
                            s1 = *source++;

                            px = (s1 & 0b_1100_0000) >> 6;
                            color = palette[px % pal];
                            *(uint*)dest = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));


                            var px1 = (s1 & 0b_1100_0000) >> 6;
                            var c1 = palette[px1];
                            *(uint*)dest = (uint)((c1.rgbBlue) | (c1.rgbGreen << 8) | (c1.rgbRed << 16) | (0xFF << 24));
                            dest += 4;

                            var px1 = (s1 & 0b_1100_0000) >> 6;
                            var c1 = palette[px1];
                            *(uint*)dest = (uint)((c1.rgbBlue) | (c1.rgbGreen << 8) | (c1.rgbRed << 16) | (0xFF << 24));
                            dest += 4;

                            //var px2 = (s1 & 0b_0000_1111);
                            //var c2 = palette[px2];
                            //*(uint*)dest = (uint)((c2.rgbBlue) | (c2.rgbGreen << 8) | (c2.rgbRed << 16) | (0xFF << 24));
                            //dest += 4;
                        }

                        // last bits in a row might not make up a whole byte
                        if (remain > 0)
                        {
                            s1 = *source++;

                            var px1 = (s1 & 0b_1111_0000) >> 4;
                            var c1 = palette[px1];
                            *(uint*)dest = (uint)((c1.rgbBlue) | (c1.rgbGreen << 8) | (c1.rgbRed << 16) | (0xFF << 24));
                            dest += 4;
                        }
                    }
                }
                else if (palette.Length > 0 && nbits == 4)
                {
                    var remain = width % 2;
                    while (--h >= 0)
                    {
                        dest = (byte*)(bitmap.BackBuffer + ((upside_down ? height - h - 1 : h) * dest_stride));
                        source = (byte*)(bufferStart + offset + ((height - h - 1) * source_stride));

                        for (int x = 0; x < source_stride - remain; x++)
                        {
                            s1 = *source++;

                            var px1 = (s1 & 0b_1111_0000) >> 4;
                            var c1 = palette[px1];
                            *(uint*)dest = (uint)((c1.rgbBlue) | (c1.rgbGreen << 8) | (c1.rgbRed << 16) | (0xFF << 24));
                            dest += 4;

                            var px2 = (s1 & 0b_0000_1111);
                            var c2 = palette[px2];
                            *(uint*)dest = (uint)((c2.rgbBlue) | (c2.rgbGreen << 8) | (c2.rgbRed << 16) | (0xFF << 24));
                            dest += 4;
                        }

                        // last bits in a row might not make up a whole byte
                        if (remain > 0)
                        {
                            s1 = *source++;

                            var px1 = (s1 & 0b_1111_0000) >> 4;
                            var c1 = palette[px1];
                            *(uint*)dest = (uint)((c1.rgbBlue) | (c1.rgbGreen << 8) | (c1.rgbRed << 16) | (0xFF << 24));
                            dest += 4;
                        }
                    }
                }
                else if (palette.Length > 0 && nbits == 8)
                {
                    while (--h >= 0)
                    {
                        dest = (byte*)(bitmap.BackBuffer + ((upside_down ? height - h - 1 : h) * dest_stride));
                        source = (byte*)(bufferStart + offset + ((height - h - 1) * source_stride));
                        w = width;
                        while (--w >= 0)
                        {
                            s1 = *source++;
                            var color = palette[s1];
                            *(uint*)dest = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                            dest += 4;
                        }
                    }
                }
                else if (hasRgbMask && hasAlphaChannel)
                {
                    while (--h >= 0)
                    {
                        dest = (byte*)(bitmap.BackBuffer + ((upside_down ? height - h - 1 : h) * dest_stride));
                        source = (byte*)(bufferStart + offset + ((height - h - 1) * source_stride));
                        w = width;

                        while (--w >= 0)
                        {
                            s32 = *(uint*)source;

                            b = (byte)((((s32 & maskB) >> shiftB) * multB) >> 24);
                            g = (byte)((((s32 & maskG) >> shiftG) * multG) >> 24);
                            r = (byte)((((s32 & maskR) >> shiftR) * multR) >> 24);
                            a = (byte)((((s32 & maskA) >> shiftA) * multA) >> 24);

                            //b = (byte)(((s32 & maskB) >> shiftB) * 255 / maxB);
                            //g = (byte)(((s32 & maskG) >> shiftG) * 255 / maxG);
                            //r = (byte)(((s32 & maskR) >> shiftR) * 255 / maxR);
                            //a = (byte)(((s32 & maskA) >> shiftA) * 255 / maxA);

                            *(uint*)dest = (uint)((b) | (g << 8) | (r << 16) | (a << 24));
                            source += nbytes;
                            dest += 4;
                        }
                    }
                }
                else if (hasRgbMask && maskA != 0) // hasAlpha = false, and maskA != 0 - we might have _fake_ alpha.. need to check for it
                {
                    while (--h >= 0)
                    {
                        dest = (byte*)(bitmap.BackBuffer + ((upside_down ? height - h - 1 : h) * dest_stride));
                        source = (byte*)(bufferStart + offset + ((height - h - 1) * source_stride));
                        w = width;

                        while (--w >= 0)
                        {
                            s32 = *(uint*)source;
                            b = (byte)((((s32 & maskB) >> shiftB) * multB) >> 24);
                            g = (byte)((((s32 & maskG) >> shiftG) * multG) >> 24);
                            r = (byte)((((s32 & maskR) >> shiftR) * multR) >> 24);
                            a = (byte)((((s32 & maskA) >> shiftA) * multA) >> 24);

                            if (a != 0)
                            {
                                // this BMP should not have an alpha channel, but windows likes doing this and we need to detect it
                                hasAlphaChannel = true;
                                goto restartLoop;
                            }

                            *(uint*)dest = (uint)((b) | (g << 8) | (r << 16) | (0xFF << 24));
                            source += nbytes;
                            dest += 4;
                        }
                    }
                }
                else if (hasRgbMask) /*if (nbits == 32 || nbits == 24)*/ // this bitmap has no alpha data
                {
                    while (--h >= 0)
                    {
                        dest = (byte*)(bitmap.BackBuffer + ((upside_down ? height - h - 1 : h) * dest_stride));
                        source = (byte*)(bufferStart + offset + ((height - h - 1) * source_stride));
                        w = width;

                        while (--w >= 0)
                        {
                            s32 = *(uint*)source;
                            b = (byte)((((s32 & maskB) >> shiftB) * multB) >> 24);
                            g = (byte)((((s32 & maskG) >> shiftG) * multG) >> 24);
                            r = (byte)((((s32 & maskR) >> shiftR) * multR) >> 24);

                            *(uint*)dest = (uint)((b) | (g << 8) | (r << 16) | (0xFF << 24));
                            source += nbytes;
                            dest += 4;
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("Bitmap combination of palette and bit masks is not supported.");
                }
                //else if (nbits == 16)
                //{
                //    while (--h >= 0)
                //    {
                //        dest = (byte*)(bitmap.BackBuffer + ((upside_down ? height - h - 1 : h) * dest_stride));
                //        source = (byte*)(bufferStart + offset + ((height - h - 1) * source_stride));
                //        w = width;

                //        while (--w >= 0)
                //        {
                //            s16 = *(ushort*)source;

                //            b = (byte)((((s16 & maskB) >> shiftB) * multB) >> 24);
                //            g = (byte)((((s16 & maskG) >> shiftG) * multG) >> 24);
                //            r = (byte)((((s16 & maskR) >> shiftR) * multR) >> 24);

                //            *(uint*)dest = (uint)((b) | (g << 8) | (r << 16) | (0xFF << 24));
                //            source += numbytes;
                //            dest += 4;
                //        }
                //    }
                //}
                //else if (nbits == 32) // this bitmap has no alpha
                //{
                //    while (--h >= 0)
                //    {
                //        dest = (byte*)(bitmap.BackBuffer + ((upside_down ? height - h - 1 : h) * stride));
                //        source = (byte*)(bufferStart + offset + ((height - h - 1) * stride));
                //        w = width;

                //        while (--w >= 0)
                //        {
                //            s32 = *(uint*)source;
                //            b = (byte)((((s32 & maskB) >> shiftB) * multB) >> 24);
                //            g = (byte)((((s32 & maskG) >> shiftG) * multG) >> 24);
                //            r = (byte)((((s32 & maskR) >> shiftR) * multR) >> 24);
                //            a = 0xFF;
                //            *(uint*)dest = (uint)((b) | (g << 8) | (r << 16) | (a << 24));
                //            source += 4;
                //            dest += 4;
                //        }
                //    }
                //}
                //else if (nbits == 24)
                //{
                //    while (--h >= 0)
                //    {
                //        var y = upside_down ? height - h - 1 : h;
                //        byte* dest = (byte*)(bitmap.BackBuffer + (y * stride));
                //        byte* source = (byte*)(bufferStart + offset + (y * stride));

                //        var w = width;
                //        while (--w >= 0)
                //        {
                //            var s32 = *(uint*)source;
                //            byte b = (byte)((s32 & maskB) >> shiftB);
                //            byte g = (byte)((s32 & maskG) >> shiftG);
                //            byte r = (byte)((s32 & maskR) >> shiftR);
                //            *(uint*)dest = (uint)((b) | (g << 8) | (r << 16) | (0xFF << 24));
                //            source += 3;
                //            dest += 4;
                //        }
                //    }
                //}

                bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
                bitmap.Unlock();
                return bitmap;
            }
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
                bV5CSType = LCS_sRGB,
                bV5Intent = LCS_GM_IMAGES,
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
