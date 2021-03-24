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
        BI_CMYK = 11,
        BI_CMYKRLE8 = 12,
        BI_CMYKRLE4 = 13,
        RLE24 = 98,
        HUFFMAN1D = 99,
    }

    internal struct CIEXYZd
    {
        public double ciexyzX;
        public double ciexyzY;
        public double ciexyzZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPCOREHEADER
    {
        // SIZE = 12
        public uint bcSize;
        public ushort bcWidth;
        public ushort bcHeight;
        public ushort bcPlanes;
        public ushort bcBitCount;
    }

    // https://d3s.mff.cuni.cz/legacy/teaching/principles_of_computers/Zkouska%20Principy%20pocitacu%202017-18%20-%20varianta%2002%20-%20priloha%20-%20format%20BMP%20z%20Wiki.pdf
    [StructLayout(LayoutKind.Sequential)]
    internal struct OS22XBITMAPHEADER
    {
        public uint Size;             /* Size of this structure in bytes */
        public uint Width;            /* Bitmap width in pixels */
        public uint Height;           /* Bitmap height in pixel */
        public ushort NumPlanes;        /* Number of bit planes (color depth) */
        public ushort BitsPerPixel;     /* Number of bits per pixel per plane */
        public BitmapCompressionMode Compression;      /* Bitmap compression scheme */
        public uint ImageDataSize;    /* Size of bitmap data in bytes */
        public uint XResolution;      /* X resolution of display device */
        public uint YResolution;      /* Y resolution of display device */
        public uint ColorsUsed;       /* Number of color table indices used */
        public uint ColorsImportant;  /* Number of important color indices */
        public ushort Units;            /* Type of units used to measure resolution */
        public ushort Reserved;         /* Pad structure to 4-byte boundary */
        public ushort Recording;        /* Recording algorithm */
        public ushort Rendering;        /* Halftoning algorithm used */
        public uint Size1;            /* Reserved for halftoning algorithm use */
        public uint Size2;            /* Reserved for halftoning algorithm use */
        public uint ColorEncoding;    /* Color model used in bitmap */
        public uint Identifier;       /* Reserved for application use */
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPINFOHEADER
    {
        // BITMAPINFOHEADER SIZE = 40
        public uint bV5Size; // uint
        public int bV5Width;
        public int bV5Height; // LONG
        public ushort bV5Planes; // WORD
        public ushort bV5BitCount;
        public BitmapCompressionMode bV5Compression;
        public uint bV5SizeImage;
        public int bV5XPelsPerMeter;
        public int bV5YPelsPerMeter;
        public uint bV5ClrUsed;
        public uint bV5ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPV3INFOHEADER
    {
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
        public uint bV5RedMask;
        public uint bV5GreenMask;
        public uint bV5BlueMask;
        public uint bV5AlphaMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPV4HEADER
    {
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
        public uint bV5RedMask;
        public uint bV5GreenMask;
        public uint bV5BlueMask;
        public uint bV5AlphaMask;
        public uint bV5CSType;
        public uint bV5Endpoints_1x;
        public uint bV5Endpoints_1y;
        public uint bV5Endpoints_1z;
        public uint bV5Endpoints_2x;
        public uint bV5Endpoints_2y;
        public uint bV5Endpoints_2z;
        public uint bV5Endpoints_3x;
        public uint bV5Endpoints_3y;
        public uint bV5Endpoints_3z;
        public uint bV5GammaRed;
        public uint bV5GammaGreen;
        public uint bV5GammaBlue;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPV5HEADER
    {
        public uint bV5Size;
        public int bV5Width;
        public int bV5Height;
        public ushort bV5Planes;
        public ushort bV5BitCount;
        // BITMAPCOREHEADER = 16

        public BitmapCompressionMode bV5Compression;
        public uint bV5SizeImage;
        public int bV5XPelsPerMeter;
        public int bV5YPelsPerMeter;
        public uint bV5ClrUsed;
        public uint bV5ClrImportant;
        // BITMAPINFOHEADER = 40

        public uint bV5RedMask;
        public uint bV5GreenMask;
        public uint bV5BlueMask;
        // BITMAPV2INFOHEADER = 52

        public uint bV5AlphaMask;
        // BITMAPV3INFOHEADER = 56

        public uint bV5CSType;
        public uint bV5Endpoints_1x;
        public uint bV5Endpoints_1y;
        public uint bV5Endpoints_1z;
        public uint bV5Endpoints_2x;
        public uint bV5Endpoints_2y;
        public uint bV5Endpoints_2z;
        public uint bV5Endpoints_3x;
        public uint bV5Endpoints_3y;
        public uint bV5Endpoints_3z;
        public uint bV5GammaRed;
        public uint bV5GammaGreen;
        public uint bV5GammaBlue;
        // BITMAPV4HEADER = 108

        public uint bV5Intent;
        public uint bV5ProfileData;
        public uint bV5ProfileSize;
        public uint bV5Reserved;
        // BITMAPV5HEADER = 124
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct RGBTRIPLE
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
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
