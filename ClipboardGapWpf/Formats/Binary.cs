using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf.Formats
{
    class Binary : IDataStreamReader<Stream>, IDataStreamReader<byte[]>, IDataStreamWriter<Stream>, IDataStreamWriter<byte[]>
    {
        public Stream ReadFromStream(Stream stream)
        {
            return stream;
        }

        public void WriteToStream(Stream obj, Stream stream)
        {
            obj.CopyTo(stream);
        }

        public void WriteToStream(byte[] obj, Stream stream)
        {
            stream.Write(obj, 0, obj.Length);
        }

        byte[] IDataStreamReader<byte[]>.ReadFromStream(Stream stream)
        {
            return stream.ReadAllBytes();
        }
    }
}
