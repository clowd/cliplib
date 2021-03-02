using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf.Formats
{

    interface IDataReader<T>
    {
        T Deserialize(byte[] buffer);
    }

    interface IDataWriter<T>
    {
        byte[] Serialize(T obj);
    }

    interface IFormatReader<TOut>
    {
    }

    interface IFormatHandleReader<TOut> : IFormatReader<TOut>
    {
        TOut ReadFromHandle(IntPtr ptr);
    }

    interface IFormatStreamReader<TOut> : IFormatReader<TOut>
    {
        TOut ReadFromStream(Stream stream);
    }

    interface IFormatWriter<TIn>
    {
        int GetByteSize(TIn data);
        void SaveToHandle(TIn data, IntPtr ptr);
    }

    class FormatSet<T> : List<FormatSetItem<T>>
    {
        public void Add(ClipboardFormat format, IFormatReader<T> reader)
        {
            this.Add(new FormatSetItem<T>(format, reader, null));
        }

        public void Add(ClipboardFormat format, IFormatReader<T> reader, IFormatWriter<T> writer)
        {
            this.Add(new FormatSetItem<T>(format, reader, writer));
        }
    }

    class FormatSetItem<T>
    {
        public ClipboardFormat Format { get; }
        public IFormatReader<T> Reader { get; }
        public IFormatWriter<T> Writer { get; }

        public FormatSetItem(ClipboardFormat format, IFormatReader<T> reader, IFormatWriter<T> writer)
        {
            Format = format;
            Reader = reader;
            Writer = writer;
        }
    }
}
