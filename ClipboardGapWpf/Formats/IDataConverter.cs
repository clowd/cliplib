using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf.Formats
{
    public interface IDataConverter { }
    public interface IDataConverter<T> : IDataConverter { }
    public interface IDataReader<T> : IDataConverter<T> { }
    public interface IDataWriter<T> : IDataConverter<T> { }

    public interface IDataHandleReader<T> : IDataReader<T>
    {
        T ReadFromHandle(IntPtr ptr, int memSize);
    }

    public interface IDataStreamReader<T> : IDataReader<T>
    {
        T ReadFromStream(Stream stream);
    }

    public interface IDataHandleWriter<T> : IDataWriter<T>
    {
        int GetDataSize(T obj);
        void WriteToHandle(T obj, IntPtr ptr);
    }

    public interface IDataStreamWriter<T> : IDataWriter<T>
    {
        void WriteToStream(T obj, Stream stream);
    }

    //class FormatSet<T> : List<FormatSetItem<T>>
    //{
    //    public void Add(ClipboardFormat format, IFormatReader<T> reader)
    //    {
    //        this.Add(new FormatSetItem<T>(format, reader, null));
    //    }

    //    public void Add(ClipboardFormat format, IFormatReader<T> reader, IFormatWriter<T> writer)
    //    {
    //        this.Add(new FormatSetItem<T>(format, reader, writer));
    //    }
    //}

    //class FormatSetItem<T>
    //{
    //    public ClipboardFormat Format { get; }
    //    public IFormatReader<T> Reader { get; }
    //    public IFormatWriter<T> Writer { get; }

    //    public FormatSetItem(ClipboardFormat format, IFormatReader<T> reader, IFormatWriter<T> writer)
    //    {
    //        Format = format;
    //        Reader = reader;
    //        Writer = writer;
    //    }
    //}
}
