using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace ClipboardGapWpf
{
    public class FormatEnumeratorImpl : IEnumFORMATETC
    {
        private readonly FORMATETC[] _formats;
        private int _current;

        internal FormatEnumeratorImpl(FORMATETC[] formats)
        {
            _formats = formats;
        }

        private FormatEnumeratorImpl(FormatEnumeratorImpl formatEnumerator)
        {
            _formats = formatEnumerator._formats;
            _current = formatEnumerator._current;
        }

        public int Next(int celt, FORMATETC[] rgelt, int[] pceltFetched)
        {
            int fetched = 0;

            if (rgelt == null)
                return NativeMethods.E_INVALIDARG;

            for (int i = 0; i < celt && _current < _formats.Length; i++)
            {
                rgelt[i] = _formats[this._current];
                _current++;
                fetched++;
            }

            if (pceltFetched != null)
            {
                pceltFetched[0] = fetched;
            }

            return (fetched == celt) ? NativeMethods.S_OK : NativeMethods.S_FALSE;
        }

        public int Skip(int celt)
        {
            // Make sure we don't overflow on the skip.
            _current = _current + (int)Math.Min(celt, Int32.MaxValue - _current);
            return (_current < _formats.Length) ? NativeMethods.S_OK : NativeMethods.S_FALSE;
        }

        public int Reset()
        {
            _current = 0;
            return NativeMethods.S_OK;
        }

        public void Clone(out IEnumFORMATETC ppenum)
        {
            ppenum = new FormatEnumeratorImpl(this);
        }
    }
}
