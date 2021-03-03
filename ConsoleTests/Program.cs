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
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Console.Clear();
                    var clip = ClipboardGapWpf.ClipboardDataObject.GetCurrentClipboard();

                    //ClipboardDataObject.SetConsoleOnlyMode();

                    //var clip = new ClipboardDataObject();

                    //clip.SetText("hello from gaaaap222!");
                    //clip.SetToClipboard();
                    //return;

                    //Console.ReadLine();hello from gaaaap!

                    var formats = clip.GetFormats().ToList();
                    Console.WriteLine("Formats:");
                    formats.ForEach(f => Console.WriteLine("  - " + f.Name));

                    //Console.WriteLine(String.Join(", ", formats.Select(f => f.Name)));
                    Console.WriteLine();
                    Console.WriteLine("Text:");
                    var test = clip.GetText();
                    Console.WriteLine(test);


                    Console.WriteLine();
                    Console.WriteLine("Drop:");
                    var drop = clip.GetFileDropList();
                    if (drop != null)
                        foreach (var f in drop)
                            Console.WriteLine(f);

                    Console.WriteLine();
                    Console.WriteLine("Image:");
                    var img = clip.GetImage();

                    if (img != null)
                    {
                        Console.WriteLine($"exists.. {img.PixelWidth}x{img.PixelHeight}");
                        using (var fileStream = new FileStream("tmp.png", FileMode.Create))
                        {
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(img));
                            encoder.Save(fileStream);
                        }
                        Console.WriteLine("wrote to tmp.png");
                    }
                    else
                    {
                        Console.WriteLine("null");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine((ex.ToString()));
                }
                Console.ReadLine();

            }


        }
    }
}
