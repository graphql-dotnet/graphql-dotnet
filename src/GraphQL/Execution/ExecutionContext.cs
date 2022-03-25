using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides a mutable instance of <see cref="IExecutionContext"/>.
    /// </summary>
    public class ExecutionContext : IExecutionContext, IExecutionArrayPool, IDisposable
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public ExecutionContext()
        {
        }

        /// <summary>
        /// Clones reusable state information from an existing instance; not any properties that
        /// hold result information. Specifically, <see cref="Errors"/>, <see cref="Metrics"/>,
        /// <see cref="OutputExtensions"/>, array pool reservations and internal reusable references
        /// are not cloned.
        /// </summary>
        public ExecutionContext(ExecutionContext context)
        {
            ExecutionStrategy = context.ExecutionStrategy;
            Document = context.Document;
            Schema = context.Schema;
            RootValue = context.RootValue;
            UserContext = context.UserContext;
            Operation = context.Operation;
            Variables = context.Variables;
            CancellationToken = context.CancellationToken;
            Listeners = context.Listeners;
            ThrowOnUnhandledException = context.ThrowOnUnhandledException;
            UnhandledExceptionDelegate = context.UnhandledExceptionDelegate;
            MaxParallelExecutionCount = context.MaxParallelExecutionCount;
            InputExtensions = context.InputExtensions;
            RequestServices = context.RequestServices;
        }

        /// <inheritdoc/>
        public IExecutionStrategy ExecutionStrategy { get; set; }

        /// <inheritdoc/>
        public GraphQLDocument Document { get; set; }

        /// <inheritdoc/>
        public ISchema Schema { get; set; }

        /// <inheritdoc/>
        public object? RootValue { get; set; }

        /// <inheritdoc/>
        public IDictionary<string, object?> UserContext { get; set; }

        /// <inheritdoc/>
        public GraphQLOperationDefinition Operation { get; set; }

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
        public Func<UnhandledExceptionContext, Task> UnhandledExceptionDelegate { get; set; } = _ => Task.CompletedTask;

        /// <inheritdoc/>
        public int? MaxParallelExecutionCount { get; set; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, object?> InputExtensions { get; set; }

        /// <inheritdoc/>
        public Dictionary<string, object?> OutputExtensions { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <inheritdoc/>
        public IServiceProvider? RequestServices { get; set; }

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
            // clearing or re-using the context will break any instances of ReadonlyResolveFieldContext from being
            // able to access many of their properties. This is not typically a problem since the context is re-used
            // once a field resolver finishes executing. However, a ReadonlyResolveFieldContext instance is not re-used
            // when an exception within a field resolver is thrown, and the FAQ says that calls to UnhandledExceptionDelegate
            // will be provided with a context that is not re-used. If we clear or re-use execution contexts, we should
            // at least provide UnhandledExceptionDelegate with a copy (e.g. create one with ReadonlyResolveFieldContext
            // and then Copy it) so that it is unaffected by clearing the execution context. Also note that subscription
            // execution will be affected by clearing the execution context.

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

            // arrays rented after the execution context has been 'disposed' will still rent just fine, but will
            // not be returned to the pool (since Dispose has already been run) and will be garbage collected.
            lock (_trackedArrays)
            {
                foreach (var array in _trackedArrays)
                    array.Return();
                _trackedArrays.Clear();
            }
        }

        /// <summary>
        /// Allows for an execution strategy to reuse an instance of <see cref="ReadonlyResolveFieldContext"/>.
        /// This field may be accessed by multiple threads at the same time, so
        /// access is restricted to <see cref="System.Threading.Interlocked.Exchange{T}(ref T, T)"/>
        /// and <see cref="System.Threading.Interlocked.CompareExchange{T}(ref T, T, T)"/>.
        /// </summary>
        internal ReadonlyResolveFieldContext? ReusableReadonlyResolveFieldContext;

        /// <summary>
        /// Allows for an execution strategy to reuse an instance of <see cref="Dictionary{TKey, TValue}"/>.
        /// This field may be accessed by multiple threads at the same time, so
        /// access is restricted to <see cref="System.Threading.Interlocked.Exchange{T}(ref T, T)"/>
        /// and <see cref="System.Threading.Interlocked.CompareExchange{T}(ref T, T, T)"/>.
        /// </summary>
        internal Dictionary<string, (GraphQLField field, FieldType fieldType)>? ReusableFields;
    }
}
