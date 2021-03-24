using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf.Formats
{
    public static class lcms
    {
        public enum Intent : uint
        {
            Perceptual = 0,
            RelativeColorimetric = 1,
            Saturation = 2,
            AbsoluteColorimetric = 3,
        }

        [Flags]
        private enum PixelType : uint
        {
            Any = 0,    // Don't check colorspace
                        // Enumeration values 1 & 2 are reserved
            Gray = 3,
            RGB = 4,
            CMY = 5,
            CMYK = 6,
            YCbCr = 7,
            YUV = 8,    // Lu'v'
            XYZ = 9,
            Lab = 10,
            YUVK = 11,  // Lu'v'K
            HSV = 12,
            HLS = 13,
            Yxy = 14,
            MCH1 = 15,
            MCH2 = 16,
            MCH3 = 17,
            MCH4 = 18,
            MCH5 = 19,
            MCH6 = 20,
            MCH7 = 21,
            MCH8 = 22,
            MCH9 = 23,
            MCH10 = 24,
            MCH11 = 25,
            MCH12 = 26,
            MCH13 = 27,
            MCH14 = 28,
            MCH15 = 29,
            LabV2 = 30
        }

        private static uint FLOAT_SH(uint s) { return s << 22; }
        private static uint OPTIMIZED_SH(uint s) { return s << 21; }
        private static uint COLORSPACE_SH(PixelType s) { return Convert.ToUInt32(s) << 16; }
        private static uint SWAPFIRST_SH(uint s) { return s << 14; }
        private static uint FLAVOR_SH(uint s) { return s << 13; }
        private static uint PLANAR_SH(uint s) { return s << 12; }
        private static uint ENDIAN16_SH(uint s) { return s << 11; }
        private static uint DOSWAP_SH(uint s) { return s << 10; }
        private static uint EXTRA_SH(uint s) { return s << 7; }
        private static uint CHANNELS_SH(uint s) { return s << 3; }
        private static uint BYTES_SH(uint s) { return s; }

        private static readonly uint TYPE_BGRA_8
            = COLORSPACE_SH(PixelType.RGB) | EXTRA_SH(1) | CHANNELS_SH(3) | BYTES_SH(1) | DOSWAP_SH(1) | SWAPFIRST_SH(1);

        [StructLayout(LayoutKind.Sequential)]
        public struct CIExyY
        {
            [MarshalAs(UnmanagedType.R8)]
            public double x;
            [MarshalAs(UnmanagedType.R8)]
            public double y;
            [MarshalAs(UnmanagedType.R8)]
            public double Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CIExyYTRIPLE
        {
            public CIExyY Red;
            public CIExyY Green;
            public CIExyY Blue;

            public static CIExyYTRIPLE FromHandle(IntPtr handle)
            {
                return Marshal.PtrToStructure<CIExyYTRIPLE>(handle);
            }
        }

        private const string
            Liblcms = "lcms2",
            WindowsCodecs = "WindowsCodecs",
            MilCore = "wpfgfx_v0400";

        private const int WINCODEC_SDK_VERSION = 0x0236;

        [DllImport(Liblcms, EntryPoint = "cmsDoTransformLineStride", CallingConvention = CallingConvention.StdCall)]
        private unsafe static extern void DoTransformLineStride(
            IntPtr transform,
            void* inputBuffer,
            void* outputBuffer,
            [MarshalAs(UnmanagedType.U4)] int pixelsPerLine,
            [MarshalAs(UnmanagedType.U4)] int lineCount,
            [MarshalAs(UnmanagedType.U4)] int bytesPerLineIn,
            [MarshalAs(UnmanagedType.U4)] int bytesPerLineOut,
            [MarshalAs(UnmanagedType.U4)] int bytesPerPlaneIn,
            [MarshalAs(UnmanagedType.U4)] int bytesPerPlaneOut);

        [DllImport(Liblcms, EntryPoint = "cmsCreateTransform", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateTransform(
            IntPtr inputProfile,
            [MarshalAs(UnmanagedType.U4)] uint inputFormat,
            IntPtr outputProfile,
            [MarshalAs(UnmanagedType.U4)] uint outputFormat,
            [MarshalAs(UnmanagedType.U4)] uint intent,
            [MarshalAs(UnmanagedType.U4)] uint flags);

        [DllImport(Liblcms, EntryPoint = "cmsCreate_sRGBProfile", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr Create_sRGBProfile();

        [DllImport(Liblcms, EntryPoint = "cmsCreateRGBProfile", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateRGBProfile(
            in CIExyY whitePoint,
            in CIExyYTRIPLE primaries,
            IntPtr[] transferFunction);

        [DllImport(Liblcms, EntryPoint = "cmsCloseProfile", CallingConvention = CallingConvention.StdCall)]
        private static extern int CloseProfile(IntPtr handle);

        [DllImport(Liblcms, EntryPoint = "cmsOpenProfileFromMem", CallingConvention = CallingConvention.StdCall)]
        private unsafe static extern IntPtr OpenProfileFromMem(/*const*/ void* memPtr, [MarshalAs(UnmanagedType.U4)] int memSize);

        [DllImport(Liblcms, EntryPoint = "cmsDeleteTransform", CallingConvention = CallingConvention.StdCall)]
        private static extern void DeleteTransform(IntPtr transform);

        [DllImport(Liblcms, EntryPoint = "cmsBuildGamma", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr BuildGammaCurve(IntPtr handle, [MarshalAs(UnmanagedType.R8)] double gamma);

        [DllImport(Liblcms, EntryPoint = "cmsFreeToneCurve", CallingConvention = CallingConvention.StdCall)]
        private static extern void FreeToneCurve(IntPtr handle);

        [DllImport(WindowsCodecs, EntryPoint = "WICCreateImagingFactory_Proxy")]
        private static extern int CreateImagingFactory(UInt32 SDKVersion, out IntPtr ppICodecFactory);

        [DllImport(WindowsCodecs, EntryPoint = "WICCreateColorContext_Proxy")]
        private static extern int /* HRESULT */ CreateColorContext(IntPtr pICodecFactory, out IntPtr /* IWICColorContext */ ppColorContext);

        [DllImport(WindowsCodecs, EntryPoint = "IWICColorContext_InitializeFromMemory_Proxy")]
        private unsafe static extern int /* HRESULT */ InitializeFromMemory(IntPtr THIS_PTR, void* pbBuffer, uint cbBufferSize);

        public static bool CheckLibAvailble()
        {
            try
            {
                DeleteTransform(IntPtr.Zero);
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }

        public static CIExyYTRIPLE GetPrimariesFromBMPEndpoints(uint red_x, uint red_y, uint green_x, uint green_y, uint blue_x, uint blue_y)
        {
            double fxpt2dot30_to_float(uint fxpt2dot30) => fxpt2dot30 * 9.31322574615478515625e-10f;
            double rx = fxpt2dot30_to_float(red_x);
            double ry = fxpt2dot30_to_float(red_y);
            double gx = fxpt2dot30_to_float(green_x);
            double gy = fxpt2dot30_to_float(green_y);
            double bx = fxpt2dot30_to_float(blue_x);
            double by = fxpt2dot30_to_float(blue_y);

            return new CIExyYTRIPLE
            {
                Red = new CIExyY { x = rx, y = ry, Y = 1 },
                Green = new CIExyY { x = gx, y = gy, Y = 1 },
                Blue = new CIExyY { x = bx, y = by, Y = 1 }
            };
        }

        public static CIExyY GetWhitePoint_sRGB()
        {
            double kD65x = 0.31271;
            double kD65y = 0.32902;
            return new CIExyY { x = kD65x, y = kD65y, Y = 1, };
        }

        public unsafe static void TransformEmbeddedBGRA(void* profilePtr, uint profileSize, IntPtr data, int width, int height, int stride, Intent intent)
        {
            var source = OpenProfileFromMem(profilePtr, (int)profileSize);
            var target = Create_sRGBProfile();
            var transform = CreateTransform(source, TYPE_BGRA_8, target, TYPE_BGRA_8, (uint)intent, 0);

            try
            {
                if (source == IntPtr.Zero)
                    throw new Exception("Unable to read source color profile.");
                if (target == IntPtr.Zero)
                    throw new Exception("Unable to create target sRGB color profile.");
                if (transform == IntPtr.Zero)
                    throw new Exception("Unable to create color transform.");

                DoTransformLineStride(transform, (void*)data, (void*)data, width, height, stride, stride, 0, 0);
            }
            finally
            {
                DeleteTransform(transform);
                CloseProfile(target);
                CloseProfile(source);
            }
        }

        public unsafe static System.Windows.Media.ColorContext GetWpfContext(void* profilePtr, uint profileSize)
        {
            IntPtr factoryPtr, colorContextPtr;

            var hr = CreateImagingFactory(WINCODEC_SDK_VERSION, out factoryPtr);
            if (hr != 0) throw new Win32Exception(hr);

            try
            {
                hr = CreateColorContext(factoryPtr, out colorContextPtr);
                if (hr != 0) throw new Win32Exception(hr);

                hr = InitializeFromMemory(colorContextPtr, profilePtr, profileSize);
                if (hr != 0) throw new Win32Exception(hr);

                var colorContextType = typeof(System.Windows.Media.ColorContext);
                var milHandleType = colorContextType.Assembly.GetType("System.Windows.Media.SafeMILHandle");

                var milHandle = Activator.CreateInstance(milHandleType, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { colorContextPtr }, null);
                var colorContext = Activator.CreateInstance(colorContextType, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { milHandle }, null);

                return (System.Windows.Media.ColorContext)colorContext;
            }
            finally
            {
                Marshal.Release(factoryPtr);
            }
        }

        public unsafe static void TransformCalibratedBGRA(CIExyYTRIPLE primaries, CIExyY whitePoint, uint red_gamma, uint green_gamma, uint blue_gamma,
            IntPtr data, int width, int height, int stride, Intent intent)
        {
            // https://github.com/chromium/chromium/blob/99314be8152e688bafbbf9a615536bdbb289ea87/third_party/blink/renderer/platform/image-decoders/bmp/bmp_image_reader.cc#L355
            double SkFixedToFloat(uint z) => ((z) * 1.52587890625e-5f);

            var tr = BuildGammaCurve(IntPtr.Zero, SkFixedToFloat(red_gamma));
            var tg = BuildGammaCurve(IntPtr.Zero, SkFixedToFloat(green_gamma));
            var tb = BuildGammaCurve(IntPtr.Zero, SkFixedToFloat(blue_gamma));
            var source = CreateRGBProfile(whitePoint, primaries, new[] { tr, tg, tb });
            var target = Create_sRGBProfile();
            var transform = CreateTransform(source, TYPE_BGRA_8, target, TYPE_BGRA_8, (uint)intent, 0);

            try
            {
                if (source == IntPtr.Zero)
                    throw new Exception("Unable to read source color profile.");
                if (target == IntPtr.Zero)
                    throw new Exception("Unable to create target sRGB color profile.");
                if (transform == IntPtr.Zero)
                    throw new Exception("Unable to create color transform.");

                DoTransformLineStride(transform, (void*)data, (void*)data, width, height, stride, stride, 0, 0);
            }
            finally
            {
                DeleteTransform(transform);
                CloseProfile(target);
                CloseProfile(source);
                FreeToneCurve(tr);
                FreeToneCurve(tg);
                FreeToneCurve(tb);
            }
        }
    }
}
