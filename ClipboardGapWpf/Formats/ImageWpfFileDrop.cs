using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClipboardGapWpf.Formats
{
    class ImageWpfFileDrop : IFormatHandleReader<BitmapSource>
    {
        private static string[] _knownImageExt = new[]
        {
            ".png", ".jpg", ".jpeg",".jpe", ".bmp",
            ".gif", ".tif", ".tiff", ".ico"
        };

        public BitmapSource ReadFromHandle(IntPtr ptr)
        {
            var reader = new FileDrop();
            var fileDropList = reader.ReadFromHandle(ptr);

            // if - there is a single file in the file drop list
            //    - the file in the file drop list is an image (file name ends with image extension)
            //    - the file exists on disk

            if (fileDropList != null && fileDropList.Length == 1)
            {
                var filePath = fileDropList[0];
                if (File.Exists(filePath) && _knownImageExt.Any(ext => filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    return new BitmapImage(new Uri(filePath));
                }
            }

            return null;
        }
    }
}
