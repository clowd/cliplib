using ClipboardGapWpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ConsoleTests
{
    class ClowdDrawing
    {

    }

    class ClowdDrawingSerializer : ClipboardGapWpf.Formats.BytesDataConverterBase<ClowdDrawing>
    {
        public override ClowdDrawing ReadFromBytes(byte[] data)
        {
            throw new NotImplementedException();
        }

        public override byte[] WriteToBytes(ClowdDrawing obj)
        {
            throw new NotImplementedException();
        }
    }


    class Program
    {
        unsafe static void Main(string[] args)
        {

            //using (var handle = ClipboardHandle.Open())
            //{
            //    var bytes = handle.GetFormatData(ClipboardFormat.UnicodeText);
            //    fixed (byte* p = bytes)
            //    {
            //        var str = new string((char*)p);
            //        Console.WriteLine(str);
            //    }
            //}
            var data = "hello :)asd\0\0";
            var db = Encoding.Unicode.GetBytes(data);
            //using (var handle = ClipboardHandle.Open())
            //{

            //    //char[] chars = data.ToCharArray(0, data.Length);

            //    //byte[] bytes = new byte[chars.Length * 2];
            //    //Buffer.BlockCopy(chars, 0, bytes, 0, chars.Length * 2);


            //    handle.SetFormatData(ClipboardFormat.UnicodeText, db);
            //}

            //var drawingFormat = ClipboardFormat.CreateFormat("Clowd Drawing", new ClowdDrawingSerializer());
            // OpenClipboard
            using (var handle = ClipboardHandle.Open())
            {
                var formats = handle.GetPresentFormats();

                if (formats.Any(f => f.Id == 4))
                    handle.GetFormat(ClipboardFormat.GetFormat(4));

                Console.WriteLine("Formats: ");
                foreach (var f in formats)
                    Console.WriteLine(" - " + f.Name);

                //Console.WriteLine();
                //Console.WriteLine();

                //var bitmap = handle.GetFormat(ClipboardFormat.Png);
                //var image = handle.GetImage();

                //handle.SetFormat(drawingFormat, new ClowdDrawing());


                //var bitmap = handle.GetValue(ClipboardFormat.Jpeg);


                //handle.GetValue<string[]>(ClipboardFormat.)

                //handle.GetFormatData(ClipboardFormat.Jpeg);

                ////var bytes = handle.GetFormatData(ClipboardFormat.UnicodeText);
                ////fixed (byte* p = bytes)
                ////{
                ////    var str = new string((char*)p);
                ////    Console.WriteLine(str);
                ////}

                //handle.SetFormatData(ClipboardFormat.UnicodeText, db);

                //formats = handle.GetPresentFormats();
                //Console.WriteLine("Formats: ");
                //foreach (var f in formats)
                //    Console.WriteLine(" - " + f.Name);

                //Console.WriteLine();
                //Console.WriteLine();

                //bytes = handle.GetFormatData(ClipboardFormat.UnicodeText);
                //var test = Encoding.Unicode.GetString(db);
                //fixed (byte* p = bytes)
                //{
                //    var str = new string((char*)p);
                //    Console.WriteLine(str);
                //}
            }

            //using (var handle = ClipboardHandle.Open())
            //{
            //    var formats = handle.GetPresentFormats();
            //    Console.WriteLine("Formats: ");
            //    foreach (var f in formats)
            //        Console.WriteLine(" - " + f.Name);

            //    Console.WriteLine();
            //    Console.WriteLine();
            //}
            Console.WriteLine();
            Console.WriteLine("done");
            Console.Read();

            //return;
            //while (true)
            //{
            //    try
            //    {
            //        Console.Clear();
            //        var clip = ClipboardGapWpf.ClipboardDataObject.GetCurrentClipboard();

            //        //ClipboardDataObject.SetConsoleOnlyMode();

            //        //var clip = new ClipboardDataObject();

            //        //clip.SetText("hello from gaaaap222!");
            //        //clip.SetToClipboard();
            //        //return;

            //        //Console.ReadLine();hello from gaaaap!

            //        var formats = clip.GetFormats().ToList();
            //        Console.WriteLine("Formats:");
            //        formats.ForEach(f => Console.WriteLine("  - " + f.Name));

            //        //Console.WriteLine(String.Join(", ", formats.Select(f => f.Name)));
            //        Console.WriteLine();
            //        Console.WriteLine("Text:");
            //        var test = clip.GetText();
            //        Console.WriteLine(test);


            //        Console.WriteLine();
            //        Console.WriteLine("Drop:");
            //        var drop = clip.GetFileDropList();
            //        if (drop != null)
            //            foreach (var f in drop)
            //                Console.WriteLine(f);

            //        Console.WriteLine();
            //        Console.WriteLine("Image:");
            //        var img = clip.GetImage();

            //        if (img != null)
            //        {
            //            Console.WriteLine($"exists.. {img.PixelWidth}x{img.PixelHeight}");
            //            using (var fileStream = new FileStream("tmp.png", FileMode.Create))
            //            {
            //                BitmapEncoder encoder = new PngBitmapEncoder();
            //                encoder.Frames.Add(BitmapFrame.Create(img));
            //                encoder.Save(fileStream);
            //            }
            //            Console.WriteLine("wrote to tmp.png");
            //        }
            //        else
            //        {
            //            Console.WriteLine("null");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine((ex.ToString()));
            //    }
            //    Console.ReadLine();

            //}


        }
    }
}
