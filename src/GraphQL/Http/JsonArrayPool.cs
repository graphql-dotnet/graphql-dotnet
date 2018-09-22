using System.Buffers;
using Newtonsoft.Json;

namespace GraphQL.Http
{
    public class JsonArrayPool : IArrayPool<char>
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

        public void Return(char[] array)
        {
            _inner.Return(array);
        }
    }
}
