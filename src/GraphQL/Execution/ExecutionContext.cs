using System;
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
        public Fragments Fragments { get; set; }

        /// <inheritdoc/>
        public Variables Variables { get; set; }

        /// <inheritdoc/>
        public ExecutionErrors Errors { get; set; }

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
        public Dictionary<string, object> Extensions { get; set; }

        /// <inheritdoc/>
        public IServiceProvider RequestServices { get; set; }

        /// <inheritdoc/>
        public TElement[] Rent<TElement>(int minimumLength)
        {
            var array = System.Buffers.ArrayPool<TElement>.Shared.Rent(minimumLength);
            lock (_trackedArrays)
                _trackedArrays.Add(array);
            return array;
        }

        private readonly List<Array> _trackedArrays = new List<Array>();

        /// <summary>
        /// Clears all state in this context.
        /// Releases any rented arrays back to the backing memory pool.
        /// </summary>
        public void Dispose()
        {
            ClearContext();
        }

        /// <summary>
        /// Clears all state in this context including any rented arrays.
        /// </summary>
        protected virtual void ClearContext()
        {
            //TODO:
            //Document = null;
            //Schema = null;
            //RootValue = null;
            //UserContext = null;
            //Operation = null;
            //Fragments = null;
            //Variables = null;
            //Errors = null;
            //CancellationToken = default;
            //Metrics = null;
            //Listeners = null;
            //ThrowOnUnhandledException = false;
            //UnhandledExceptionDelegate = null;
            //MaxParallelExecutionCount = null;
            //Extensions = null;
            //RequestServices = null;

            lock (_trackedArrays)
            {
                foreach (var array in _trackedArrays)
                    array.Return();
                _trackedArrays.Clear();
            }
        }
    }
}
