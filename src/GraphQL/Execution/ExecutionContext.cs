using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides a mutable instance of <see cref="IExecutionContext"/>.
    /// </summary>
    public class ExecutionContext : IExecutionContext, IExecutionArrayPool, IDisposable
    {
        /// <inheritdoc/>
        public Document Document { get; set; }

        /// <inheritdoc/>
        public ISchema Schema { get; set; }

        /// <inheritdoc/>
        public object RootValue { get; set; }

        /// <inheritdoc/>
        public IDictionary<string, object> UserContext { get; set; }

        /// <inheritdoc/>
        public Operation Operation { get; set; }

        /// <inheritdoc/>
        public Fragments Fragments { get; set; } = new Fragments();

        /// <inheritdoc/>
        public Variables Variables { get; set; }

        /// <inheritdoc/>
        public ExecutionErrors Errors { get; set; } = new ExecutionErrors();

        /// <inheritdoc/>
        public CancellationToken CancellationToken { get; set; }

        /// <inheritdoc/>
        public Metrics Metrics { get; set; }

        /// <inheritdoc/>
        public List<IDocumentExecutionListener> Listeners { get; set; }

        /// <inheritdoc/>
        public bool ThrowOnUnhandledException { get; set; }

        /// <inheritdoc/>
        public Action<UnhandledExceptionContext> UnhandledExceptionDelegate { get; set; }

        /// <inheritdoc/>
        public int? MaxParallelExecutionCount { get; set; }

        /// <inheritdoc/>
        public Dictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();

        /// <inheritdoc/>
        public IServiceProvider RequestServices { get; set; }

        private readonly List<Array> _trackedArrays = new List<Array>();

        TElement[] IExecutionArrayPool.Rent<TElement>(int minimumLength) => RentSharedArray<TElement>(minimumLength);

        /// <inheritdoc cref="IExecutionArrayPool.Rent{TElement}(int)"/>
        public TElement[] RentSharedArray<TElement>(int minimumLength)
        {
            var array = ArrayPool<TElement>.Shared.Rent(minimumLength);
            lock (_trackedArrays)
                _trackedArrays.Add(array);
            return array;
        }

        /// <summary>
        /// Releases any rented arrays back to the backing memory pool.
        /// </summary>
        public void Dispose()
        {
            // lock is not required because at this time work with ExecutionContext has already been completed
            foreach (var array in _trackedArrays)
                array.Return();

            _trackedArrays.Clear();
        }
    }
}
