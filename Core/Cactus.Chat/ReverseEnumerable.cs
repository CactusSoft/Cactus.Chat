using System;
using System.Collections;
using System.Collections.Generic;

namespace Cactus.Chat
{
    public static class ReverseEnumerableExtension
    {
        public static IEnumerable<T> ReverseEnumerable<T>(this IList<T> list)
        {
            return new ReverseEnumerable<T>(list);
        }
    }

    internal class ReverseEnumerable<T> : IEnumerable<T>
    {
        private readonly IList<T> _list;

        public ReverseEnumerable(IList<T> list)
        {
            _list = list;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ReverseEnumerator<T>(_list);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ReverseEnumerator<T>(_list);
        }
    }

    internal class ReverseEnumerator<T> : IEnumerator<T>
    {
        private readonly IList<T> _list;
        private int _index;

        public ReverseEnumerator(IList<T> list)
        {
            _list = list;
            Reset();
        }

        public T Current
        {
            get { return _list[_index]; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("ReverseEnumerator has disposed");
            }

            if (_index <= 0)
            {
                return false;
            }

            _index--;
            return true;
        }

        public void Reset()
        {
            _index = _list.Count;
        }

        #region IDisposable Support
        private bool isDisposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    // there's nothing to dispose actually
                }
                isDisposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
