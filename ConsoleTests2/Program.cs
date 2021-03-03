using ClipboardGapWpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ConsoleTests2
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine(System.Threading.Thread.CurrentThread.GetApartmentState());


            ClipboardDataObject.SetConsoleOnlyMode();

            var clip2 = new ClipboardDataObject();

            BitmapSource bSource = new BitmapImage(new Uri("C:\\Users\\Caelan\\Desktop\\cw1.png"));

            clip2.SetImage(bSource);
            clip2.SetToClipboard();

            Console.Read();
        }
    }
}
