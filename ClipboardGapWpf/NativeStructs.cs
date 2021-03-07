using System;
using System.Runtime.InteropServices;

namespace ClipboardGapWpf
{
    internal enum BitmapCompressionMode : uint
    {
        BI_RGB = 0,
        BI_RLE8 = 1,
        BI_RLE4 = 2,
        BI_BITFIELDS = 3,
        BI_JPEG = 4,
        BI_PNG = 5,
        BI_ALPHABITFIELDS = 6,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPINFOHEADER
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
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPV5HEADER
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
    internal struct BITMAPFILEHEADER // size = 14
    {
        public ushort bfType;
        public uint bfSize;
        public ushort bfReserved1;
        public ushort bfReserved2;
        public uint bfOffBits;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
    }

    public delegate IntPtr WindowProcedureHandler(IntPtr hwnd, uint uMsg, IntPtr wparam, IntPtr lparam);

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowClass
    {
        public uint style;
        public WindowProcedureHandler lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
    }
}
