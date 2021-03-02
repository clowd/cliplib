using ClipboardGapWpf.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;


namespace ClipboardGapWpf
{
    interface IDataStoreEntry : IDisposable
    {
        FORMATETC Format { get; }
        DATADIR Direction { get; }
        void SaveToHandle(IntPtr ptr);
        int GetSize();
    }

    class ManagedDataStoreEntry<T> : IDataStoreEntry
    {
        public FORMATETC Format { get; }

        public DATADIR Direction { get; }

        public T Data { get; }

        public bool OwnsData { get; }

        public IFormatWriter<T> Writer { get; }

        public ManagedDataStoreEntry(FORMATETC format, DATADIR dir, T data, bool ownsData, IFormatWriter<T> writer)
        {
            Format = format;
            Direction = dir;
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
            Writer.SaveToHandle(Data, ptr);
        }

        public int GetSize()
        {
            throw new NotImplementedException();
        }
    }

    class NativeDataStoreEntry : IDataStoreEntry
    {
        public FORMATETC Format { get; }

        public DATADIR Direction { get; }

        public IntPtr DataHGlobal { get; private set; }

        private bool disposed = false;

        public NativeDataStoreEntry(FORMATETC format, DATADIR dir, IntPtr hglobal)
        {
            Format = format;
            Direction = dir;
            DataHGlobal = hglobal;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Marshal.FreeHGlobal(DataHGlobal);
                DataHGlobal = IntPtr.Zero;
                disposed = true;
            }
        }

        ~NativeDataStoreEntry()
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
            throw new NotImplementedException();
        }

        public int GetSize()
        {
            throw new NotImplementedException();
        }
    }


    class MyDataStore : IDataObject
    {
        private const int DV_E_FORMATETC = unchecked((int)0x80040064);
        private const int DV_E_LINDEX = unchecked((int)0x80040068);
        private const int DV_E_TYMED = unchecked((int)0x80040069);
        private const int DV_E_DVASPECT = unchecked((int)0x8004006B);
        private const int OLE_E_NOTRUNNING = unchecked((int)0x80040005);
        private const int OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003);
        private const int DATA_S_SAMEFORMATETC = 0x00040130;

        private Dictionary<short, IDataStoreEntry> _entries = new Dictionary<short, IDataStoreEntry>();

        private static readonly TYMED[] ALLOWED_TYMEDS = new TYMED[] {
            TYMED.TYMED_HGLOBAL,
            TYMED.TYMED_ISTREAM,
            TYMED.TYMED_ENHMF,
            TYMED.TYMED_MFPICT,
            TYMED.TYMED_GDI
        };

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

        public void GetData(ref FORMATETC formatetc, out STGMEDIUM medium)
        {
            medium = new STGMEDIUM();
            medium.tymed =
            GetDataHere(formatetc, medium);

            if (_entries.TryGetValue(formatetc.cfFormat, out var match))
            {

            }

            // no match found
            Marshal.ThrowExceptionForHR(DV_E_FORMATETC);

            // end

            foreach (var entry in _entries)
            {
                if (entry.Format.cfFormat == formatetc.cfFormat)
                {
                    medium.tymed = entry.Format.tymed;
                    medium.unionmember = OleUtil.CopyHGlobal(entry.DataHGlobal);
                }
            }

            if (GetTymedUseable(formatetc.tymed))
            {
                if ((formatetc.tymed & TYMED.TYMED_HGLOBAL) != 0)
                {
                    medium.tymed = TYMED.TYMED_HGLOBAL;
                    medium.unionmember = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE | NativeMethods.GMEM_ZEROINIT, 1);

                    if (medium.unionmember == IntPtr.Zero)
                    {
                        throw new OutOfMemoryException();
                    }

                    try
                    {
                        GetDataHere(ref formatetc, ref medium);
                    }
                    catch
                    {
                        UnsafeNativeMethods.GlobalFree(new HandleRef(medium, medium.unionmember));
                        medium.unionmember = IntPtr.Zero;
                        throw;
                    }
                }
                else
                {
                    medium.tymed = formatetc.tymed;
                    GetDataHere(ref formatetc, ref medium);
                }
            }
            else
            {
                Marshal.ThrowExceptionForHR(DV_E_TYMED);
            }
        }

        public void GetDataHere(ref FORMATETC format, ref STGMEDIUM medium)
        {
            GetDataIntoOleStructs(ref formatetc, ref medium);
        }

        public int QueryGetData(ref FORMATETC formatetc)
        {
            if (formatetc.dwAspect != DVASPECT.DVASPECT_CONTENT)
                return DV_E_DVASPECT;

            if (!GetTymedUseable(formatetc.tymed))
                return DV_E_TYMED;

            if (formatetc.cfFormat == 0)
                return NativeMethods.S_FALSE;

            if (_entries.TryGetValue(formatetc.cfFormat, out var match))
                return NativeMethods.S_OK; // we found a match

            return DV_E_FORMATETC;
        }

        public int GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
        {
            formatOut = new FORMATETC();
            return DATA_S_SAMEFORMATETC;
        }

        public void SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release)
        {
            throw new NotImplementedException();
        }

        public IEnumFORMATETC EnumFormatEtc(DATADIR direction)
        {
            throw new NotImplementedException();
        }

        public int DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection)
        {
            connection = 0;
            return NativeMethods.E_NOTIMPL;
        }

        public void DUnadvise(int connection)
        {
            Marshal.ThrowExceptionForHR(NativeMethods.E_NOTIMPL);
        }

        public int EnumDAdvise(out IEnumSTATDATA enumAdvise)
        {
            enumAdvise = null;
            return OLE_E_ADVISENOTSUPPORTED;
        }
    }
}
