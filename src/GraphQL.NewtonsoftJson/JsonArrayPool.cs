using System.Buffers;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson;

internal class JsonArrayPool : IArrayPool<char>
{
    private readonly ArrayPool<char> _inner;

    public JsonArrayPool(ArrayPool<char> inner)
    {
        _inner = inner;
    }

    public char[] Rent(int minimumLength)
    {
        return _inner.Rent(minimumLength);
    }

    public void Return(char[]? array)
    {
        if (array != null)
            _inner.Return(array);
    }
}
