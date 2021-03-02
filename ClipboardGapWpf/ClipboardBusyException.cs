using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf
{
    public class ClipboardBusyException : Exception
    {
        public string ClipboardOwnerName { get; set; }

        public ClipboardBusyException(string owner)
        {
            ClipboardOwnerName = owner;
        }
    }
}
