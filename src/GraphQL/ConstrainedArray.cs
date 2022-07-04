using System.Collections;

namespace GraphQL
{
    // Represents an array based on another array of equal or greater length.
    // The wrapped array is usually obtained from ArrayPool.
    internal sealed class ConstrainedArray<T> : IList<T>, IList
    {
        private readonly T[] _array;

        public ConstrainedArray(T[] array, int count)
        {
            if (count < 0 || count > array.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            _array = array;
            Count = count;
        }

        public T this[int index] { get => _array[index]; set => throw new NotSupportedException(); }

        object? IList.this[int index] { get => _array[index]; set => throw new NotImplementedException(); }

        public int Count { get; }

        public bool IsSynchronized => false;

        public object SyncRoot => _array.SyncRoot;

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public int IndexOf(T item) => throw new NotSupportedException();

        public int IndexOf(object? value) => throw new NotSupportedException();

        public bool Contains(T item) => throw new NotSupportedException();

        public bool Contains(object? value) => throw new NotSupportedException();

        public void CopyTo(T[] array, int arrayIndex) => _array.CopyTo(array, arrayIndex);

        public void CopyTo(Array array, int index) => _array.CopyTo(array, index);

        public IEnumerator<T> GetEnumerator() => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        public void Clear() => throw new NotSupportedException();

        public void Insert(int index, object? value) => throw new NotSupportedException();

        public bool Remove(T item) => throw new NotSupportedException();

        public void Remove(object? value) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        public void Insert(int index, T item) => throw new NotSupportedException();

        public void Add(T item) => throw new NotSupportedException();

        public int Add(object? value) => throw new NotSupportedException();
    }
}
