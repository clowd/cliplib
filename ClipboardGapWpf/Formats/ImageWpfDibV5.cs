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
            LCS_GM_IMAGES = 4,
            PROFILE_LINKED = 1279872587,
            PROFILE_EMBEDDED = 1296188740;

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

        static double pixelPerMeterToDpi(int pels)
        {
            if (pels == 0) return 96;
            return pels * 0.0254d;
        }

        public unsafe override BitmapSource ReadFromBytes(byte[] data)
        {
            fixed (byte* bufferStart = data)
            {
                bool hasFileHeader = BitConverter.ToUInt16(data, 0) == BFH_BM;
                var size_fh = Marshal.SizeOf<BITMAPFILEHEADER>();

                int offset = 0;
                var fh = default(BITMAPFILEHEADER);
                if (hasFileHeader)
                    fh = StructUtil.Deserialize<BITMAPFILEHEADER>(bufferStart, ref offset);

                // we'll just unpack all the various header types we support into a standard BMPV5 header 
                // this makes subsequent code easier to maintain as it only needs to refer to one place

                var header_size = BitConverter.ToUInt32(data, offset);
                var bi = default(BITMAPV5HEADER);
                bool is_os21x_ = false;

                if (header_size == 12)
                {
                    var bich = StructUtil.Deserialize<BITMAPCOREHEADER>(data, offset);
                    bi.bV5Size = bich.bcSize;
                    bi.bV5Width = bich.bcWidth;
                    bi.bV5Height = bich.bcHeight;
                    bi.bV5Planes = bich.bcPlanes;
                    bi.bV5BitCount = bich.bcBitCount;

                    bi.bV5CSType = LCS_sRGB;
                    is_os21x_ = true;
                }
                else if (/*header_size == 14 || */header_size == 16 || header_size == 42 || header_size == 46 || header_size == 64)
                {
                    var biih = StructUtil.Deserialize<BITMAPINFOHEADER>(data, offset);
                    bi.bV5Size = biih.bV5Size;
                    bi.bV5Width = biih.bV5Width;
                    bi.bV5Height = biih.bV5Height;
                    bi.bV5Planes = biih.bV5Planes;
                    bi.bV5BitCount = biih.bV5BitCount;

                    if (header_size > 16)
                    {
                        bi.bV5Compression = biih.bV5Compression;
                        bi.bV5SizeImage = biih.bV5SizeImage;
                        bi.bV5XPelsPerMeter = biih.bV5XPelsPerMeter;
                        bi.bV5YPelsPerMeter = biih.bV5YPelsPerMeter;
                        bi.bV5ClrUsed = biih.bV5ClrUsed;
                        bi.bV5ClrImportant = biih.bV5ClrImportant;
                    }

                    // https://www.fileformat.info/mirror/egff/ch09_05.htm (G31D)
                    if (bi.bV5Compression == (BitmapCompressionMode)3 && bi.bV5BitCount == 1)
                        bi.bV5Compression = BitmapCompressionMode.HUFFMAN1D;

                    else if (bi.bV5Compression == (BitmapCompressionMode)4 && bi.bV5BitCount == 24)
                        bi.bV5Compression = BitmapCompressionMode.RLE24;

                    bi.bV5CSType = LCS_sRGB;

                }
                else if (header_size == 40)
                {
                    var biih = StructUtil.Deserialize<BITMAPINFOHEADER>(data, offset);
                    bi.bV5Size = biih.bV5Size;
                    bi.bV5Width = biih.bV5Width;
                    bi.bV5Height = biih.bV5Height;
                    bi.bV5Planes = biih.bV5Planes;
                    bi.bV5BitCount = biih.bV5BitCount;
                    bi.bV5Compression = biih.bV5Compression;
                    bi.bV5SizeImage = biih.bV5SizeImage;
                    bi.bV5XPelsPerMeter = biih.bV5XPelsPerMeter;
                    bi.bV5YPelsPerMeter = biih.bV5YPelsPerMeter;
                    bi.bV5ClrUsed = biih.bV5ClrUsed;
                    bi.bV5ClrImportant = biih.bV5ClrImportant;

                    bi.bV5CSType = LCS_sRGB;
                }
                else if (header_size == 52 || header_size == 56)
                {
                    var biih = StructUtil.Deserialize<BITMAPV3INFOHEADER>(data, offset);
                    bi.bV5Size = biih.bV5Size;
                    bi.bV5Width = biih.bV5Width;
                    bi.bV5Height = biih.bV5Height;
                    bi.bV5Planes = biih.bV5Planes;
                    bi.bV5BitCount = biih.bV5BitCount;
                    bi.bV5Compression = biih.bV5Compression;
                    bi.bV5SizeImage = biih.bV5SizeImage;
                    bi.bV5XPelsPerMeter = biih.bV5XPelsPerMeter;
                    bi.bV5YPelsPerMeter = biih.bV5YPelsPerMeter;
                    bi.bV5ClrUsed = biih.bV5ClrUsed;
                    bi.bV5ClrImportant = biih.bV5ClrImportant;
                    bi.bV5RedMask = biih.bV5RedMask;
                    bi.bV5GreenMask = biih.bV5GreenMask;
                    bi.bV5BlueMask = biih.bV5BlueMask;

                    if (header_size == 56) // 56b header adds alpha mask
                        bi.bV5AlphaMask = biih.bV5AlphaMask;

                    bi.bV5CSType = LCS_sRGB;
                }
                else if (header_size == 108)
                {
                    var biih = StructUtil.Deserialize<BITMAPV4HEADER>(data, offset);
                    bi.bV5Size = biih.bV5Size;
                    bi.bV5Width = biih.bV5Width;
                    bi.bV5Height = biih.bV5Height;
                    bi.bV5Planes = biih.bV5Planes;
                    bi.bV5BitCount = biih.bV5BitCount;
                    bi.bV5Compression = biih.bV5Compression;
                    bi.bV5SizeImage = biih.bV5SizeImage;
                    bi.bV5XPelsPerMeter = biih.bV5XPelsPerMeter;
                    bi.bV5YPelsPerMeter = biih.bV5YPelsPerMeter;
                    bi.bV5ClrUsed = biih.bV5ClrUsed;
                    bi.bV5ClrImportant = biih.bV5ClrImportant;
                    bi.bV5RedMask = biih.bV5RedMask;
                    bi.bV5GreenMask = biih.bV5GreenMask;
                    bi.bV5BlueMask = biih.bV5BlueMask;
                    bi.bV5AlphaMask = biih.bV5AlphaMask;
                    bi.bV5CSType = biih.bV5CSType;
                    bi.bV5Endpoints_1x = biih.bV5Endpoints_1x;
                    bi.bV5Endpoints_1y = biih.bV5Endpoints_1y;
                    bi.bV5Endpoints_1z = biih.bV5Endpoints_1z;
                    bi.bV5Endpoints_2x = biih.bV5Endpoints_2x;
                    bi.bV5Endpoints_2y = biih.bV5Endpoints_2y;
                    bi.bV5Endpoints_2z = biih.bV5Endpoints_2z;
                    bi.bV5Endpoints_3x = biih.bV5Endpoints_3x;
                    bi.bV5Endpoints_3y = biih.bV5Endpoints_3y;
                    bi.bV5Endpoints_3z = biih.bV5Endpoints_3z;
                    bi.bV5GammaRed = biih.bV5GammaRed;
                    bi.bV5GammaGreen = biih.bV5GammaGreen;
                    bi.bV5GammaBlue = biih.bV5GammaBlue;
                }
                else if (header_size == 124)
                {
                    bi = StructUtil.Deserialize<BITMAPV5HEADER>(data, offset);
                }
                else
                {
                    throw new NotSupportedException($"Bitmap header size '{header_size}' not supported, expected 12, 40, 52, 56, 108, or 124.");
                }

                offset += (int)header_size;

                // https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Formats/Bmp/BmpDecoderCore.cs
                // https://dxr.mozilla.org/mozilla-central/source/image/decoders/nsBMPDecoder.cpp

                int nbits = bi.bV5BitCount;

                //if (nbits != 32 && nbits != 24 &&  && nbits != 16)
                //    throw new NotSupportedException($"Bitmaps with bpp of '{nbits}' are not supported. Expected 16, 24, or 32.");

                if (bi.bV5Planes != 1)
                    throw new NotSupportedException($"Bitmap bV5Planes of '{bi.bV5Planes}' is not supported.");

                if (bi.bV5CSType != LCS_sRGB && bi.bV5CSType != LCS_WINDOWS_COLOR_SPACE && bi.bV5CSType != PROFILE_EMBEDDED && bi.bV5CSType != LCS_CALIBRATED_RGB)
                    throw new NotSupportedException($"Bitmap with header size '{header_size}' and color space of '{bi.bV5CSType}' is not supported.");

                uint maskR = 0;
                uint maskG = 0;
                uint maskB = 0;
                uint maskA = 0;

                bool hasAlphaChannel = false;

                switch (bi.bV5Compression)
                {
                    case BitmapCompressionMode.BI_BITFIELDS:
                        // seems that v5 bitmaps sometimes do not have a color table, even if BI_BITFIELDS is set
                        // we read/skip them here anyways, if we have a file header we can correct the offset later
                        // whether or not these follow the header depends entirely on the application..
                        // if (header_size <= 40)
                        // {

                        // OS/2 bitmaps are only 9 bytes here instead of 12 as they are not aligned

                        //if (is_os21x_)
                        //{
                        //    maskR = BitConverter.ToUInt32(data, offset) & 0x00FF_FFFF;
                        //    offset += 3;
                        //    maskG = BitConverter.ToUInt32(data, offset) & 0x00FF_FFFF;
                        //    offset += 3;
                        //    maskB = BitConverter.ToUInt32(data, offset) & 0x00FF_FFFF;
                        //    offset += 3;
                        //}
                        //else
                        {
                            maskR = BitConverter.ToUInt32(data, offset);
                            offset += sizeof(uint);
                            maskG = BitConverter.ToUInt32(data, offset);
                            offset += sizeof(uint);
                            maskB = BitConverter.ToUInt32(data, offset);
                            offset += sizeof(uint);
                        }

                        // maskR | maskG | maskB == 1 << nbits - 1
                        // do these overlap, and do they add up to 0xFFFFFF
                        // }
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
                        hasAlphaChannel = true;
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
                                maskA = 0xff000000; // fake transparency?
                                break;
                            case 24:
                                maskR = 0xff0000;
                                maskG = 0xff00;
                                maskB = 0xff;
                                break;
                            case 16:
                                maskR = 0x7c00;
                                maskG = 0x03e0;
                                maskB = 0x001f;
                                maskA = 0x8000; // fake transparency?
                                break;
                        }
                        break;
                    case BitmapCompressionMode.BI_RLE4:
                        if (nbits != 4) throw new NotSupportedException("RLE4 encoded bitmaps must have 4 bpp");
                        if (bi.bV5Height < 0) throw new NotSupportedException("Top-down bitmaps are not supported with RLE compression.");
                        break;
                    case BitmapCompressionMode.BI_RLE8:
                        if (nbits != 8) throw new NotSupportedException("RLE8 encoded bitmaps must have 8 bpp");
                        if (bi.bV5Height < 0) throw new NotSupportedException("Top-down bitmaps are not supported with RLE compression.");
                        break;
                    case BitmapCompressionMode.BI_JPEG:
                        byte[] jpegImg = new byte[bi.bV5SizeImage];
                        var jpegOffset = (hasFileHeader ? size_fh : 0) + (int)bi.bV5Size;
                        if (jpegImg.Length == 0)
                            throw new NotSupportedException("Bitmap bV5SizeImage must be > 0 with BI_JPEG compression.");
                        Buffer.BlockCopy(data, jpegOffset, jpegImg, 0, jpegImg.Length);
                        var jpg = new JpegBitmapDecoder(new MemoryStream(jpegImg), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        return jpg.Frames[0];
                    case BitmapCompressionMode.BI_PNG:
                        byte[] pngImg = new byte[bi.bV5SizeImage];
                        var pngOffset = (hasFileHeader ? size_fh : 0) + (int)bi.bV5Size;
                        if (pngImg.Length == 0)
                            throw new NotSupportedException("Bitmap bV5SizeImage must be > 0 with BI_PNG compression.");
                        Buffer.BlockCopy(data, pngOffset, pngImg, 0, pngImg.Length);
                        var png = new PngBitmapDecoder(new MemoryStream(pngImg), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        return png.Frames[0];
                    default:
                        throw new NotSupportedException($"Bitmap with bV5Compression of '{bi.bV5Compression.ToString()}' is not supported.");
                }

                // The number of entries in the palette is either 2n (where n is the number of bits per pixel) or a smaller number specified in the header
                // always allocate at least 256 entries so we can ignore bad data which seeks past the end of palette data.

                var pallength = nbits < 16 ? (1 << nbits) : 0;
                if (bi.bV5ClrUsed > 0)
                    pallength = (int)bi.bV5ClrUsed;

                //var bitsperpal = is_os21x_ ? 3 : 4;
                //var palmax = (data.Length - offset - bi.bV5SizeImage) / bitsperpal;

                if (pallength > 65536)
                    throw new NotSupportedException("Bitmap has an oversized/invalid color palette.");

                RGBQUAD[] palette = new RGBQUAD[pallength];
                for (int i = 0; i < palette.Length; i++)
                {
                    if (is_os21x_)
                    {
                        var small = StructUtil.Deserialize<RGBTRIPLE>(bufferStart, ref offset);
                        palette[i] = new RGBQUAD { rgbBlue = small.rgbBlue, rgbGreen = small.rgbGreen, rgbRed = small.rgbRed };
                    }
                    else
                    {
                        palette[i] = StructUtil.Deserialize<RGBQUAD>(bufferStart, ref offset);
                    }
                }

                // lets use the v3/v4/v5 masks if present instead of BITFIELDS or RGB
                if (bi.bV5RedMask != 0) maskR = bi.bV5RedMask;
                if (bi.bV5BlueMask != 0) maskB = bi.bV5BlueMask;
                if (bi.bV5GreenMask != 0) maskG = bi.bV5GreenMask;
                if (bi.bV5AlphaMask != 0)
                {
                    maskA = bi.bV5AlphaMask;
                    hasAlphaChannel = true;
                }

                bool smBit = nbits == 1 || nbits == 2 || nbits == 4 || nbits == 8;
                bool lgBit = nbits == 16 || nbits == 24 || nbits == 32;

                if (!lgBit && !smBit)
                    throw new NotSupportedException($"Bitmap with bits per pixel of '{nbits}' are not supported.");

                if (lgBit && (maskR == 0 || maskB == 0 || maskG == 0))
                    throw new NotSupportedException($"Bitmap (bbp {nbits}) color masks could not be determined, this usually indicates a malformed bitmap file.");

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
                    width,
                    height,
                    pixelPerMeterToDpi(bi.bV5XPelsPerMeter),
                    pixelPerMeterToDpi(bi.bV5YPelsPerMeter),
                    PixelFormats.Bgra32,
                    BitmapPalettes.Halftone256Transparent);

                int source_stride = (nbits * width + 31) / 32 * 4; // = width * (nbits / 8) + (width % 4); // (width * nbits + 7) / 8;
                int dest_stride = bitmap.BackBufferStride;

                bitmap.Lock();

                if (hasFileHeader && fh.bfOffBits != 0)
                    offset = (int)fh.bfOffBits;

                restartLoop:

                RGBQUAD color;
                int pal = palette.Length;
                byte b, r, g, a;
                uint i32;
                byte i4;
                byte* source;
                uint* dest;
                int y, x, w, h = height, nbytes = (nbits / 8);

                if (bi.bV5Compression == BitmapCompressionMode.BI_RLE8)
                {
                    byte op1, op2;
                    y = x = 0;

                    for (; offset < data.Length;)
                    {
                        dest = (uint*)(bitmap.BackBuffer + ((height - y - 1) * dest_stride));
                        dest += x;

                        op1 = data[offset++];
                        op2 = data[offset++];

                        if (op1 == 0) // escape/control char
                        {
                            if (op2 == 0) // End of line
                            {
                                y++;
                                x = 0;
                                if (y > height) throw new InvalidOperationException("Invalid RLE Compression");
                            }
                            else if (op2 == 1) // End of Bitmap
                            {
                                break;
                            }
                            else if (op2 == 2) // Delta offset
                            {
                                x += data[offset++]; // X pos
                                y += data[offset++]; // Y pos
                                if (y > height) throw new InvalidOperationException("Invalid RLE Compression");
                                if (x > width) throw new InvalidOperationException("Invalid RLE Compression");
                            }
                            else
                            {
                                // absolute mode, op2 indicates number of raw pixels to read
                                for (int i = 0; i < op2; i++)
                                {
                                    i4 = data[offset + i];
                                    color = palette[i4 % pal];
                                    *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                                    x++;
                                    if (x > width) throw new InvalidOperationException("Invalid RLE Compression");
                                }
                                var add = (op2 + (op2 % 2)); // padding to WORD boundary?
                                offset += add;
                            }
                        }
                        else
                        {
                            // encoded mode, duplicate op2, op1 times.
                            color = palette[op2 % pal];
                            i32 = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                            for (int i = 0; i < op1; i++)
                            {
                                *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                                x++;
                                if (x > width) throw new InvalidOperationException("Invalid RLE Compression");
                            }
                        }
                    }
                }
                else if (bi.bV5Compression == BitmapCompressionMode.BI_RLE4)
                {
                    byte op1, op2, nibble0, nibble1;
                    y = x = 0;

                    for (; offset < data.Length;)
                    {
                        dest = (uint*)(bitmap.BackBuffer + ((height - y - 1) * dest_stride));
                        dest += x;

                        op1 = data[offset++];
                        op2 = data[offset++];

                        if (op1 == 0)
                        {
                            if (op2 == 0)
                            {
                                y++;
                                x = 0;
                                if (y > height) throw new InvalidOperationException("Invalid RLE Compression");
                            }
                            else if (op2 == 1)
                            {
                                break;
                            }
                            else if (op2 == 2) // offset
                            {
                                x += data[offset++]; // X pos
                                y += data[offset++]; // Y pos
                                if (y > height) throw new InvalidOperationException("Invalid RLE Compression");
                                if (x > width) throw new InvalidOperationException("Invalid RLE Compression");
                            }
                            else
                            {
                                int read = 0;
                                nibble1 = nibble0 = 0;
                                for (int k = 0; k < op2; k++)
                                {
                                    if ((k % 2) == 0)
                                    {
                                        read++;
                                        byte px = data[offset++];
                                        nibble0 = (byte)((px & 0xF0) >> 4);
                                        nibble1 = (byte)(px & 0x0F);
                                        i4 = nibble0;
                                    }
                                    else
                                    {
                                        i4 = nibble1;
                                    }

                                    color = palette[i4 % pal];
                                    *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                                    x++;
                                    if (x > width) throw new InvalidOperationException("Invalid RLE Compression");
                                }
                                if ((read & 0x01) > 0)
                                {
                                    offset++; // padding
                                }
                            }
                        }
                        else
                        {
                            nibble0 = (byte)((op2 & 0xF0) >> 4);
                            nibble1 = (byte)(op2 & 0x0F);
                            for (int l = 0; l < op1 && x < width; l++)
                            {
                                i4 = l % 2 == 0 ? nibble0 : nibble1;
                                color = palette[i4 % pal];
                                *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                                x++;
                                if (x > width) throw new InvalidOperationException("Invalid RLE Compression");
                            }
                        }
                    }
                }
                else if (nbits == 1)
                {
                    while (--h >= 0)
                    {
                        y = height - h - 1;
                        dest = (uint*)(bitmap.BackBuffer + ((upside_down ? y : h) * dest_stride));
                        source = bufferStart + offset + (y * source_stride);
                        for (x = 0; x < source_stride - 1; x++)
                        {
                            i4 = *source++;
                            for (int bit = 7; bit >= 0; bit--)
                            {
                                color = palette[(i4 & (1 << bit)) >> bit];
                                *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                            }
                        }

                        // last bits in a row might not make up a whole byte
                        i4 = *source++;
                        for (int bit = 7; bit >= 8 - (width - ((source_stride - 1) * 8)); bit--)
                        {
                            color = palette[(i4 & (1 << bit)) >> bit];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        }
                    }
                }
                else if (nbits == 2)
                {
                    var px_remain = width % 4;
                    if (px_remain == 0) px_remain = 4;

                    while (--h >= 0)
                    {
                        y = height - h - 1;
                        dest = (uint*)(bitmap.BackBuffer + ((upside_down ? y : h) * dest_stride));
                        source = bufferStart + offset + (y * source_stride);
                        for (x = 0; x < source_stride - 1; x++)
                        {
                            i4 = *source++;

                            color = palette[((i4 & 0b_1100_0000) >> 6) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));

                            color = palette[((i4 & 0b_0011_0000) >> 4) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));

                            color = palette[((i4 & 0b_0000_1100) >> 2) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));

                            color = palette[((i4 & 0b_0000_0011) >> 0) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        }

                        i4 = *source++;

                        if (px_remain > 0)
                        {
                            color = palette[((i4 & 0b_1100_0000) >> 6) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        }
                        if (px_remain > 1)
                        {
                            color = palette[((i4 & 0b_0011_0000) >> 4) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        }
                        if (px_remain > 2)
                        {
                            color = palette[((i4 & 0b_0000_1100) >> 2) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        }
                        if (px_remain > 3)
                        {
                            color = palette[((i4 & 0b_0000_0011) >> 0) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        }
                    }
                }
                else if (nbits == 4)
                {
                    var px_remain = width % 2;
                    while (--h >= 0)
                    {
                        y = height - h - 1;
                        dest = (uint*)(bitmap.BackBuffer + ((upside_down ? y : h) * dest_stride));
                        source = bufferStart + offset + (y * source_stride);
                        for (x = 0; x < source_stride - px_remain; x++)
                        {
                            i4 = *source++;
                            color = palette[((i4 & 0b_1111_0000) >> 4) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                            color = palette[((i4 & 0b_0000_1111) >> 0) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        }

                        if (px_remain > 0)
                        {
                            i4 = *source++;
                            color = palette[((i4 & 0b_1111_0000) >> 4) % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        }
                    }
                }
                else if (nbits == 8)
                {
                    while (--h >= 0)
                    {
                        y = height - h - 1;
                        dest = (uint*)(bitmap.BackBuffer + ((upside_down ? y : h) * dest_stride));
                        source = bufferStart + offset + (y * source_stride);
                        w = width;
                        while (--w >= 0)
                        {
                            i4 = *source++;
                            color = palette[i4 % pal];
                            *dest++ = (uint)((color.rgbBlue) | (color.rgbGreen << 8) | (color.rgbRed << 16) | (0xFF << 24));
                        }
                    }
                }
                else if (nbits >= 16 && hasAlphaChannel)
                {
                    while (--h >= 0)
                    {
                        y = height - h - 1;
                        dest = (uint*)(bitmap.BackBuffer + ((upside_down ? y : h) * dest_stride));
                        source = bufferStart + offset + (y * source_stride);
                        w = width;
                        while (--w >= 0)
                        {
                            i32 = *(uint*)source;

                            b = (byte)((((i32 & maskB) >> shiftB) * multB) >> 24);
                            g = (byte)((((i32 & maskG) >> shiftG) * multG) >> 24);
                            r = (byte)((((i32 & maskR) >> shiftR) * multR) >> 24);
                            a = (byte)((((i32 & maskA) >> shiftA) * multA) >> 24);

                            *dest++ = (uint)((b) | (g << 8) | (r << 16) | (a << 24));
                            source += nbytes;
                        }
                    }
                }
                else if (nbits >= 16 && maskA != 0) // hasAlpha = false, and maskA != 0 - we might have _fake_ alpha.. need to check for it
                {
                    while (--h >= 0)
                    {
                        y = height - h - 1;
                        dest = (uint*)(bitmap.BackBuffer + ((upside_down ? y : h) * dest_stride));
                        source = bufferStart + offset + (y * source_stride);
                        w = width;
                        while (--w >= 0)
                        {
                            i32 = *(uint*)source;

                            b = (byte)((((i32 & maskB) >> shiftB) * multB) >> 24);
                            g = (byte)((((i32 & maskG) >> shiftG) * multG) >> 24);
                            r = (byte)((((i32 & maskR) >> shiftR) * multR) >> 24);
                            a = (byte)((((i32 & maskA) >> shiftA) * multA) >> 24);

                            if (a != 0)
                            {
                                // this BMP should not have an alpha channel, but windows likes doing this and we need to detect it
                                hasAlphaChannel = true;
                                goto restartLoop;
                            }

                            *dest++ = (uint)((b) | (g << 8) | (r << 16) | (0xFF << 24));
                            source += nbytes;
                        }
                    }
                }
                else if (nbits >= 16) // simple bmp, no transparency
                {
                    while (--h >= 0)
                    {
                        y = height - h - 1;
                        dest = (uint*)(bitmap.BackBuffer + ((upside_down ? y : h) * dest_stride));
                        source = bufferStart + offset + (y * source_stride);
                        w = width;
                        while (--w >= 0)
                        {
                            i32 = *(uint*)source;
                            b = (byte)((((i32 & maskB) >> shiftB) * multB) >> 24);
                            g = (byte)((((i32 & maskG) >> shiftG) * multG) >> 24);
                            r = (byte)((((i32 & maskR) >> shiftR) * multR) >> 24);
                            a = (byte)((((i32 & maskA) >> shiftA) * multA) >> 24);
                            *dest++ = (uint)((b) | (g << 8) | (r << 16) | (0xFF << 24));
                            source += nbytes;
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException($"Bitmap combination of header length ({header_size}) compression ({bi.bV5Compression.ToString()}), palette length ({pal}), bits-per-pixel ({nbits}) is not supported.");
                }

                // transform any embedded color profile to sRGB
                if (bi.bV5CSType == PROFILE_EMBEDDED)
                {
                    var profileSize = bi.bV5ProfileSize;
                    var profileOffset = (hasFileHeader ? size_fh : 0) + (int)bi.bV5ProfileData;

                    var test = lcms.GetWpfContext(bufferStart + profileOffset, profileSize);

                    bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
                    bitmap.Unlock();
                    bitmap.Freeze(); // dispose back buffer
                    var bl = new ColorContext[] { test }.ToList();
                    return BitmapFrame.Create(bitmap, null, null, new System.Collections.ObjectModel.ReadOnlyCollection<ColorContext>(bl));

                    lcms.TransformEmbeddedBGRA(bufferStart + profileOffset, profileSize, bitmap.BackBuffer, width, height, dest_stride, lcms.Intent.Perceptual);
                }
                else if (bi.bV5CSType == LCS_CALIBRATED_RGB)
                {
                    var primaries = lcms.GetPrimariesFromBMPEndpoints(
                        bi.bV5Endpoints_1x, bi.bV5Endpoints_1y,
                        bi.bV5Endpoints_2x, bi.bV5Endpoints_2y,
                        bi.bV5Endpoints_3x, bi.bV5Endpoints_3y);

                    var whitepoint = lcms.GetWhitePoint_sRGB();

                    lcms.TransformCalibratedBGRA(
                        primaries, whitepoint,
                        bi.bV5GammaRed, bi.bV5GammaGreen, bi.bV5GammaBlue,
                        bitmap.BackBuffer, width, height, dest_stride, lcms.Intent.Perceptual);
                }

                bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
                bitmap.Unlock();
                bitmap.Freeze(); // dispose back buffer

                //var frame = BitmapFrame.Create(bitmap);
                //frame.ColorContexts

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
