using ClipboardGapWpf.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;


namespace ClipboardGapWpf
{
    interface IDataStoreEntry : IDisposable
    {
        //bool IsStream { get; }
        //FORMATETC Format { get; }
        //DATADIR Direction { get; }
        void SaveToHandle(IntPtr ptr);
        int GetSize();
    }

    class ManagedDataStoreEntry<T> : IDataStoreEntry
    {
        //public FORMATETC Format { get; }

        //public DATADIR Direction { get; }

        public T Data { get; }

        public bool OwnsData { get; }

        public IDataWriter<T> Writer { get; }

        private byte[] _streamCache;

        public ManagedDataStoreEntry(T data, bool ownsData, IDataWriter<T> writer)
        {
            //Format = format;
            //Direction = dir;
            Data = data;
            OwnsData = ownsData;
            Writer = writer;
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (OwnsData && Data is IDisposable d)
                        d.Dispose();
                }
            }
        }

        ~ManagedDataStoreEntry()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void SaveToHandle(IntPtr ptr)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(IDataStoreEntry));

            if (Writer is IDataStreamWriter<T> streamWriter)
            {
                var buffer = GetStreamBytes(streamWriter);
                Marshal.Copy(buffer, 0, ptr, buffer.Length);
            }
            else if (Writer is IDataHandleWriter<T> handleWriter)
            {
                handleWriter.WriteToHandle(Data, ptr);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(Writer));
            }
        }

        public int GetSize()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(IDataStoreEntry));

            if (Writer is IDataStreamWriter<T> streamWriter)
            {
                var buffer = GetStreamBytes(streamWriter);
                return buffer.Length;
            }
            else if (Writer is IDataHandleWriter<T> handleWriter)
            {
                return handleWriter.GetDataSize(Data);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(Writer));
            }
        }

        private byte[] GetStreamBytes(IDataStreamWriter<T> writer)
        {
            if (_streamCache == null)
            {
                var ms = new MemoryStream();
                writer.WriteToStream(Data, ms);
                _streamCache = ms.GetBuffer();
            }

            return _streamCache;
        }
    }

    //class NativeDataStoreEntry : IDataStoreEntry
    //{
    //    public FORMATETC Format { get; }

    //    public DATADIR Direction { get; }

    //    public IntPtr DataHGlobal { get; private set; }

    //    private bool disposed = false;

    //    public NativeDataStoreEntry(FORMATETC format, DATADIR dir, IntPtr hglobal)
    //    {
    //        Format = format;
    //        Direction = dir;
    //        DataHGlobal = hglobal;
    //    }

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (!disposed)
    //        {
    //            Marshal.FreeHGlobal(DataHGlobal);
    //            DataHGlobal = IntPtr.Zero;
    //            disposed = true;
    //        }
    //    }

    //    ~NativeDataStoreEntry()
    //    {
    //        Dispose(false);
    //    }

    //    public void Dispose()
    //    {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    public void SaveToHandle(IntPtr ptr)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int GetSize()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}


    class MyDataObject : IDataObject
    {
        private const int DV_E_FORMATETC = unchecked((int)0x80040064);
        private const int DV_E_LINDEX = unchecked((int)0x80040068);
        private const int DV_E_TYMED = unchecked((int)0x80040069);
        private const int DV_E_DVASPECT = unchecked((int)0x8004006B);
        private const int OLE_E_NOTRUNNING = unchecked((int)0x80040005);
        private const int OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003);
        private const int DATA_S_SAMEFORMATETC = 0x00040130;
        private const int STG_E_MEDIUMFULL = unchecked((int)0x80030070);

        private Dictionary<short, IDataStoreEntry> _entries = new Dictionary<short, IDataStoreEntry>();

        private static readonly TYMED[] ALLOWED_TYMEDS = new TYMED[] {
            TYMED.TYMED_HGLOBAL,
            //TYMED.TYMED_ISTREAM,
            //TYMED.TYMED_ENHMF,
            //TYMED.TYMED_MFPICT,
            //TYMED.TYMED_GDI
        };

        public void SetFormat<T>(ClipboardFormat format, T obj, IDataWriter<T> writer)
        {
            _entries[format.Id] = new ManagedDataStoreEntry<T>(obj, false, writer);
        }

        public T GetEntryType<T>()
        {
            return _entries
                .Select(e => e.Value as ManagedDataStoreEntry<T>)
                .Where(e => e != null)
                .Select(e => e.Data)
                .FirstOrDefault();
        }

        private bool GetTymedUseable(TYMED tymed)
        {
            for (int i = 0; i < ALLOWED_TYMEDS.Length; i++)
            {
                if ((tymed & ALLOWED_TYMEDS[i]) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        void IDataObject.GetData(ref FORMATETC formatetc, out STGMEDIUM medium)
        {
            Console.WriteLine($"GetData ({ClipboardFormat.GetFormat(formatetc.cfFormat)})");
            if (formatetc.dwAspect != DVASPECT.DVASPECT_CONTENT)
                Marshal.ThrowExceptionForHR(DV_E_DVASPECT);

            if (!GetTymedUseable(formatetc.tymed))
                Marshal.ThrowExceptionForHR(DV_E_TYMED);

            if (formatetc.cfFormat == 0)
                Marshal.ThrowExceptionForHR(NativeMethods.S_FALSE);

            if (!_entries.TryGetValue(formatetc.cfFormat, out var match))
                Marshal.ThrowExceptionForHR(DV_E_FORMATETC);

            var dataSize = match.GetSize();

            medium = new STGMEDIUM();
            medium.tymed = TYMED.TYMED_HGLOBAL;
            medium.unionmember = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, dataSize);

            try
            {
                (this as IDataObject).GetDataHere(ref formatetc, ref medium);
            }
            catch
            {
                // only if we failed to write data, free memory - else will be freed by the caller.
                NativeMethods.GlobalFree(medium.unionmember);
            }
        }

        void IDataObject.GetDataHere(ref FORMATETC formatetc, ref STGMEDIUM medium)
        {
            Console.WriteLine($"GetDataHere ({ClipboardFormat.GetFormat(formatetc.cfFormat)})");
            if (formatetc.dwAspect != DVASPECT.DVASPECT_CONTENT)
                Marshal.ThrowExceptionForHR(DV_E_DVASPECT);

            if (!GetTymedUseable(formatetc.tymed))
                Marshal.ThrowExceptionForHR(DV_E_TYMED);

            if (formatetc.cfFormat == 0)
                Marshal.ThrowExceptionForHR(NativeMethods.S_FALSE);

            if (!_entries.TryGetValue(formatetc.cfFormat, out var match))
                Marshal.ThrowExceptionForHR(DV_E_FORMATETC);

            if (medium.unionmember == IntPtr.Zero)
                Marshal.ThrowExceptionForHR(NativeMethods.E_OUTOFMEMORY);

            int availableMemory = NativeMethods.GlobalSize(medium.unionmember);

            if (match.GetSize() > availableMemory)
                Marshal.ThrowExceptionForHR(STG_E_MEDIUMFULL);

            var ptr = NativeMethods.GlobalLock(medium.unionmember);
            try
            {
                match.SaveToHandle(ptr);
            }
            finally
            {
                NativeMethods.GlobalUnlock(medium.unionmember);
            }
        }

        int IDataObject.QueryGetData(ref FORMATETC formatetc)
        {
            if (formatetc.dwAspect != DVASPECT.DVASPECT_CONTENT)
                return DV_E_DVASPECT;

            if (!GetTymedUseable(formatetc.tymed))
                return DV_E_TYMED;

            if (formatetc.cfFormat == 0)
                return NativeMethods.S_FALSE;

            if (!_entries.TryGetValue(formatetc.cfFormat, out var match))
                return DV_E_FORMATETC;

            return NativeMethods.S_OK; // we found a match
        }

        int IDataObject.GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
        {
            formatOut = new FORMATETC();
            return DATA_S_SAMEFORMATETC;
        }

        void IDataObject.SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release)
        {
            throw new NotImplementedException();
        }

        IEnumFORMATETC IDataObject.EnumFormatEtc(DATADIR direction)
        {
            var formats = _entries.Select(e => new FORMATETC
            {
                cfFormat = e.Key,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                ptd = IntPtr.Zero,
                tymed = TYMED.TYMED_HGLOBAL,
            });

            var formatA = formats.ToArray();

            return new FormatEnumeratorImpl(formatA);
        }

        int IDataObject.DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection)
        {
            connection = 0;
            return NativeMethods.E_NOTIMPL;
        }

        void IDataObject.DUnadvise(int connection)
        {
            Marshal.ThrowExceptionForHR(NativeMethods.E_NOTIMPL);
        }

        int IDataObject.EnumDAdvise(out IEnumSTATDATA enumAdvise)
        {
            enumAdvise = null;
            return OLE_E_ADVISENOTSUPPORTED;
        }
    }
}
