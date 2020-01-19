using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    public interface IDataLoaderResult<T> : IDataLoaderResult
    {
        new Task<T> GetResultAsync(CancellationToken cancellationToken = default);
        //new TaskAwaiter<T> GetAwaiter();
    }

    public interface IDataLoaderResult
    {
        Task<object> GetResultAsync(CancellationToken cancellationToken = default);
        //TaskAwaiter<object> GetAwaiter();
        //TaskStatus Status { get; }
    }
}
