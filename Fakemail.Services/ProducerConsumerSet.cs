using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Fakemail.Services
{
    public class ProducerConsumerSet<T> : IProducerConsumerCollection<T>
    {
        private SortedSet<T> _items = new SortedSet<T>();
        private object _lock = new object();

        public int Count
        { 
            get
            {
                lock (_lock)
                {
                    return _items.Count;
                }
            }
        }

        public bool IsSynchronized => true;

        public object SyncRoot => _lock;

        public void CopyTo(T[] array, int index)
        {
            lock (_lock)
            {
                _items.CopyTo(array, index);
            }
        }

        public void CopyTo(Array array, int index)
        {
            lock (_lock)
            {
                ((ICollection)_items).CopyTo(array, index);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public T[] ToArray()
        {
            lock (_lock)
            {
                return _items.ToArray();
            }
        }

        public bool TryAdd(T item)
        {
            lock (_lock)
            {
                _items.Add(item);
            }

            return true;
        }

        public bool TryTake([MaybeNullWhen(false)] out T item)
        {
            lock (_lock)
            {
                if (_items.Any())
                {
                    item = _items.First();
                    _items.Remove(item);
                    return true;
                }
                item = default(T);
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
