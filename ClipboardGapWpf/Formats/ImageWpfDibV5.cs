using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace ClipboardGapWpf.Formats
{
    public enum BitmapCompressionMode : uint
    {
        BI_RGB = 0,
        BI_RLE8 = 1,
        BI_RLE4 = 2,
        BI_BITFIELDS = 3,
        BI_JPEG = 4,
        BI_PNG = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPV5HEADER
    {
        // BITMAPINFOHEADER SIZE = 40
        public uint bV5Size;
        public int bV5Width;
        public int bV5Height;
        public ushort bV5Planes;
        public ushort bV5BitCount;
        public BitmapCompressionMode bV5Compression;
        public uint bV5SizeImage;
        public int bV5XPelsPerMeter;
        public int bV5YPelsPerMeter;
        public uint bV5ClrUsed;
        public uint bV5ClrImportant;

        // BITMAPV4HEADER SIZE - 40+68 = 108
        public uint bV5RedMask;
        public uint bV5GreenMask;
        public uint bV5BlueMask;
        public uint bV5AlphaMask;
        public uint bV5CSType;

        public uint bV5Endpoints_1;
        public uint bV5Endpoints_2;
        public uint bV5Endpoints_3;
        public uint bV5Endpoints_4;
        public uint bV5Endpoints_5;
        public uint bV5Endpoints_6;
        public uint bV5Endpoints_7;
        public uint bV5Endpoints_8;
        public uint bV5Endpoints_9;

        public uint bV5GammaRed;
        public uint bV5GammaGreen;
        public uint bV5GammaBlue;

        // BITMAPV5HEADER 108+16 = 124
        public uint bV5Intent;
        public uint bV5ProfileData;
        public uint bV5ProfileSize;
        public uint bV5Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct BITMAPFILEHEADER // size = 14
    {
        public ushort bfType;
        public uint bfSize;
        public ushort bfReserved1;
        public ushort bfReserved2;
        public uint bfOffBits;
    }

    //[StructLayout(LayoutKind.Sequential)]
    //public struct RGBQUAD
    //{
    //    public byte rgbBlue;
    //    public byte rgbGreen;
    //    public byte rgbRed;
    //    public byte rgbReserved;
    //}

    class ImageWpfDibV5 : IFormatStreamReader<BitmapSource>
    {
        // https://docs.microsoft.com/en-us/windows/win32/gdi/bitmap-header-types
        // https://gitlab.idiap.ch/bob/bob.io.image/blob/c7ee46c80ae24b9e74cbf8ff76168605186271db/bob/io/image/bmp.cpp#L866
        // https://en.wikipedia.org/wiki/BMP_file_format#/media/File:BMPfileFormat.png
        // https://en.wikipedia.org/wiki/BMP_file_format
        // https://docs.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapv5header

        // write with BmpBitmapEncoder and then strip first 14 bytes

        public BitmapSource ReadFromStream(Stream stream)
        {
            var buffer = stream.ReadAllBytes();
            var header = StructUtil.Deserialize<BITMAPV5HEADER>(buffer, 0);
            var fileSize = 14 + buffer.Length;

            if (header.bV5Size == 40) // V1
            {
                Console.WriteLine("READING DIBv1");

            }
            else if (header.bV5Size == 108) // V4
            {
                Console.WriteLine("READING DIBv4");

            }
            else if (header.bV5Size == 124) // V5
            {
                Console.WriteLine("READING DIBv5");
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(header.bV5Size));
            }

            var file = new BITMAPFILEHEADER();
            file.bfType = 0x4d42; // "BM"
            file.bfSize = (uint)fileSize;
            file.bfReserved1 = 0;
            file.bfReserved2 = 0;
            file.bfOffBits = 14 + header.bV5Size + header.bV5ClrUsed * 4;

            var fileBytes = StructUtil.Serialize(file);

            var ms = new MemoryStream();
            ms.Write(fileBytes, 0, fileBytes.Length);
            ms.Write(buffer, 0, buffer.Length);
            ms.Seek(0, SeekOrigin.Begin);

            var bitmap = BitmapFrame.Create(ms);
            return bitmap;
        }

        //private BitmapSource CF_DIBV5ToBitmapSource(IntPtr hBitmap)
        //{
        //    IntPtr scan0 = IntPtr.Zero;
        //    var bmi = (BITMAPV5HEADER)Marshal.PtrToStructure(hBitmap, typeof(BITMAPV5HEADER));

        //    int stride = (int)(bmi.bV5SizeImage / bmi.bV5Height);
        //    long offset = bmi.bV5Size + bmi.bV5ClrUsed * Marshal.SizeOf<RGBQUAD>();
        //    if (bmi.bV5Compression == BitmapCompressionMode.BI_BITFIELDS)
        //    {
        //        offset += 12; //bit masks follow the header
        //    }

        //    scan0 = new IntPtr(hBitmap.ToInt64() + offset);

        //    BitmapSource bmpSource = BitmapSource.Create(
        //        bmi.bV5Width, bmi.bV5Height,
        //        bmi.bV5XPelsPerMeter, bmi.bV5YPelsPerMeter,
        //        System.Windows.Media.PixelFormats.Bgra32, null,
        //        scan0, (int)bmi.bV5SizeImage, stride);

        //    return bmpSource;
        //}

        //public BitmapSource ReadFromHandle(IntPtr ptr)
        //{
        //    return CF_DIBV5ToBitmapSource(ptr);
        //}

        //private BitmapSource dib5legacy(IntPtr ptr)
        //{
        //    var header = (BITMAPV5HEADER)Marshal.PtrToStructure(ptr, typeof(BITMAPV5HEADER));
        //    using (var bitmap = CreateDrawingBitmapFromVersionOnePointer(ptr, header))
        //    {
        //        IntPtr ip = bitmap.GetHbitmap();
        //        BitmapSource bs = null;
        //        try
        //        {
        //            bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
        //               IntPtr.Zero, System.Windows.Int32Rect.Empty,
        //               System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        //        }
        //        finally
        //        {
        //            NativeMethods.DeleteObject(ip);
        //        }
        //        return bs;
        //    }
        //}




        //public BitmapSource ReadFromStream(Stream stream)
        //{
        //    var buffer = ReadToEnd(stream);

        //    GCHandle pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);

        //    var header = (BITMAPV5HEADER)Marshal.PtrToStructure(pin.AddrOfPinnedObject(), typeof(BITMAPV5HEADER));

        //    if (header.bV5Compression == BitmapCompressionMode.BI_RGB)
        //    {
        //    }

        //    throw new NotSupportedException();

        //    //int count = Marshal.SizeOf(typeof(BITMAPV5HEADER));
        //    //byte[] readBuffer = new byte[count];
        //}

        //public byte[] UnwrapStructure(uint format)
        //{
        //    //TODO: it is very sad that we invoke System.Drawing here to get the job done. probably not very optimal.




        //    var bitmapVersionFivePointer = ClipboardApi.GetClipboardData(ClipboardApi.CF_DIBV5);
        //    var bitmapVersionFiveHeader = (BITMAPV5HEADER)Marshal.PtrToStructure(bitmapVersionFivePointer, typeof(BITMAPV5HEADER));
        //    if (bitmapVersionFiveHeader.bV5Compression == BI_RGB)
        //    {
        //        var bitmapVersionOneBytes = ClipboardApi.GetClipboardDataBytes(ClipboardApi.CF_DIB);
        //        var bitmapVersionOneHeader = GeneralApi.ByteArrayToStructure<BITMAPINFOHEADER>(bitmapVersionOneBytes);

        //        return HandleBitmapVersionOne(bitmapVersionOneBytes, bitmapVersionOneHeader);
        //    }
        //    else
        //    {
        //        return HandleBitmapVersionFive(bitmapVersionFivePointer, bitmapVersionFiveHeader);
        //    }
        //}

        //byte[] HandleBitmapVersionOne(byte[] bitmapVersionOneBytes, BITMAPINFOHEADER bitmapVersionOneHeader)
        //{
        //    var bitmap = CreateBitmapVersionOne(bitmapVersionOneBytes, bitmapVersionOneHeader);
        //    return imagePersistenceService.ConvertBitmapSourceToByteArray(bitmap);
        //}

        //static BitmapFrame CreateBitmapVersionOne(byte[] bitmapVersionOneBytes, BITMAPINFOHEADER bitmapVersionOneHeader)
        //{
        //    var fileHeaderSize = Marshal.SizeOf(typeof(BITMAPFILEHEADER));
        //    var infoHeaderSize = bitmapVersionOneHeader.biSize;
        //    var fileSize = fileHeaderSize + bitmapVersionOneHeader.biSize + bitmapVersionOneHeader.biSizeImage;

        //    var fileHeader = new BITMAPFILEHEADER();
        //    fileHeader.bfType = BITMAPFILEHEADER.BM;
        //    fileHeader.bfSize = fileSize;
        //    fileHeader.bfReserved1 = 0;
        //    fileHeader.bfReserved2 = 0;
        //    fileHeader.bfOffBits = fileHeaderSize + infoHeaderSize + bitmapVersionOneHeader.biClrUsed * 4;

        //    var fileHeaderBytes = GeneralApi.StructureToByteArray(fileHeader);

        //    var bitmapStream = new MemoryStream();
        //    bitmapStream.Write(fileHeaderBytes, 0, fileHeaderSize);
        //    bitmapStream.Write(bitmapVersionOneBytes, 0, bitmapVersionOneBytes.Length);
        //    bitmapStream.Seek(0, SeekOrigin.Begin);

        //    var bitmap = BitmapFrame.Create(bitmapStream);
        //    return bitmap;
        //}

        //byte[] HandleBitmapVersionFive(IntPtr pointer, BITMAPV5HEADER infoHeader)
        //{
        //    using (var drawingBitmap = CreateDrawingBitmapFromVersionOnePointer(pointer, infoHeader))
        //    {
        //        var renderTargetBitmapSource = new RenderTargetBitmap(infoHeader.bV5Width,
        //                                                  infoHeader.bV5Height,
        //                                                  96, 96, PixelFormats.Pbgra32);
        //        var visual = new DrawingVisual();
        //        var drawingContext = visual.RenderOpen();

        //        drawingContext.DrawImage(CreateBitmapSourceFromBitmap(drawingBitmap),
        //                                 new Rect(0, 0, infoHeader.bV5Width,
        //                                          infoHeader.bV5Height));

        //        renderTargetBitmapSource.Render(visual);

        //        return imagePersistenceService.ConvertBitmapSourceToByteArray(renderTargetBitmapSource);
        //    }
        //}

        //static Bitmap ConvertBitmapTo32Bit(Bitmap sourceBitmap)
        //{
        //    if (sourceBitmap.PixelFormat == DrawingPixelFormat.Format32bppPArgb)
        //    {
        //        return sourceBitmap;
        //    }

        //    var targetBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height, DrawingPixelFormat.Format32bppPArgb);
        //    using (var graphics = Graphics.FromImage(targetBitmap))
        //    {
        //        graphics.DrawImage(sourceBitmap, new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height));
        //    }
        //    sourceBitmap.Dispose();
        //    return targetBitmap;
        //}

        //static Bitmap CreateDrawingBitmapFromVersionOnePointer(IntPtr pointer, BITMAPV5HEADER bmi)
        //{
        //    //var bitmap = new Bitmap(
        //    //    bmi.bV5Width,
        //    //    bmi.bV5Height,
        //    //    -(int)(bmi.bV5SizeImage / bmi.bV5Height),
        //    //    bmi.bV5BitCount == 24 ? DrawingPixelFormat.Format24bppRgb : DrawingPixelFormat.Format32bppPArgb,
        //    //    new IntPtr(pointer.ToInt64() + bmi.bV5Size));

        //    var bitmap = new Bitmap(
        //        (int)bmi.bV5Width,
        //        (int)bmi.bV5Height,
        //        -(int)(bmi.bV5SizeImage / bmi.bV5Height),
        //        bmi.bV5BitCount == 24 ? DrawingPixelFormat.Format24bppRgb : DrawingPixelFormat.Format32bppPArgb,
        //        new IntPtr(pointer.ToInt64() + bmi.bV5Size + (bmi.bV5Height - 1) * (int)(bmi.bV5SizeImage / bmi.bV5Height)));

        //    return ConvertBitmapTo32Bit(bitmap);
        //}



        //static BitmapSource CreateBitmapSourceFromBitmap(Bitmap bitmap)
        //{
        //    if (bitmap == null)
        //        throw new ArgumentNullException(nameof(bitmap));

        //    var bitmapHandle = bitmap.GetHbitmap();

        //    try
        //    {
        //        return Imaging.CreateBitmapSourceFromHBitmap(
        //            bitmapHandle,
        //            IntPtr.Zero,
        //            Int32Rect.Empty,
        //            BitmapSizeOptions.FromEmptyOptions());
        //    }
        //    finally
        //    {
        //        DeleteObject(bitmapHandle);
        //    }
        //}
    }
}
