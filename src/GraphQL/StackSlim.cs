using System.Buffers;

namespace GraphQL;

/// <summary>
/// A stack that stores two entries inline and rents an array for additional entries.
/// </summary>
/// <remarks>
/// Dispose the stack to return its rented storage. Do not copy a stack that owns rented storage.
/// </remarks>
internal ref struct StackSlim<T> : IDisposable
{
    private T? _item0;
    private T? _item1;
    private T[]? _array;

    /// <summary>
    /// Gets the number of entries in the stack.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Adds an entry to the top of the stack.
    /// </summary>
    public void Push(T item)
    {
        if (Count == 0)
            _item0 = item;
        else if (Count == 1)
            _item1 = item;
        else
            PushToArray(item);

        ++Count;
    }

    private void PushToArray(T item)
    {
        int index = Count - 2;
        if (_array == null)
        {
            _array = ArrayPool<T>.Shared.Rent(16);
        }
        else if (index == _array.Length)
        {
            var larger = ArrayPool<T>.Shared.Rent(checked(_array.Length * 2));
            Array.Copy(_array, larger, _array.Length);
            ArrayPool<T>.Shared.Return(_array, clearArray: true);
            _array = larger;
        }

        _array[index] = item;
    }

    /// <summary>
    /// Removes and returns the top entry, clearing its storage, or returns false if the stack is empty.
    /// </summary>
    public bool TryPop([MaybeNullWhen(false)] out T item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }

        int index = --Count;
        if (index == 0)
        {
            item = _item0!;
            _item0 = default;
        }
        else if (index == 1)
        {
            item = _item1!;
            _item1 = default;
        }
        else
        {
            item = _array![index - 2];
            _array[index - 2] = default!;
        }

        return true;
    }

    /// <summary>
    /// Removes all entries, clearing their storage while retaining any rented array for reuse.
    /// </summary>
    public void Clear()
    {
        _item0 = default;
        _item1 = default;
        if (Count > 2)
            Array.Clear(_array!, 0, Count - 2);
        Count = 0;
    }

    /// <summary>
    /// Removes all entries and returns any rented array to the pool, resetting the stack to its default state.
    /// </summary>
    public void Dispose()
    {
        var array = _array;
        this = default;
        if (array != null)
            ArrayPool<T>.Shared.Return(array, clearArray: true);
    }
}
