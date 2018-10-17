using System;
using System.Buffers;
using System.IO;

namespace GraphQL.Http
{
    public interface IByteResult : IDisposable
    {
        ArraySegment<byte> Result { get; }
    }

    public class PooledByteResult : IByteResult
    {
        private readonly byte[] _buffer;
        private readonly ArrayPool<byte> _pool;

        public PooledByteResult(ArrayPool<byte> pool, int minLength)
        {
            _pool = pool;
            _buffer = _pool.Rent(minLength);
            Stream = new MemoryStream(_buffer);
        }

        public ArraySegment<byte> Result { get; private set; }

        internal MemoryStream Stream { get; }

        internal void InitResponseFromCurrentStreamPosition()
        {
            Result = new ArraySegment<byte>(_buffer, 0, (int)Stream.Position);
        }

        private void ReleaseUnmanagedResources()
        {
            _pool.Return(_buffer);
            Stream.Dispose();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PooledByteResult()
        {
            ReleaseUnmanagedResources();
        }
    }
}
