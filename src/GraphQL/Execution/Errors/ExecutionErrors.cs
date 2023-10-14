using System.Collections;

namespace GraphQL
{
    /// <summary>
    /// Contains a list of execution errors. Thread safe except <see cref="IEnumerable{T}"/> methods.
    /// </summary>
    public class ExecutionErrors : IEnumerable<ExecutionError>
    {
        private readonly object _lock = new();
        internal List<ExecutionError>? List;

        internal ExecutionErrors(int capacity)
        {
            List = new List<ExecutionError>(capacity);
        }

        /// <summary>
        /// Creates an instance of <see cref="ExecutionErrors"/>.
        /// </summary>
        public ExecutionErrors()
        {
        }

        /// <summary>
        /// Adds an execution error to the list.
        /// </summary>
        public virtual void Add(ExecutionError error)
        {
            lock (_lock)
                (List ??= new()).Add(error ?? throw new ArgumentNullException(nameof(error)));
        }

        /// <summary>
        /// Adds a list of execution errors to the list.
        /// </summary>
        public virtual void AddRange(IEnumerable<ExecutionError> errors)
        {
            foreach (var error in errors)
                Add(error);
        }

        /// <summary>
        /// Returns the number of execution errors in the list.
        /// </summary>
        public int Count => List?.Count ?? 0;

        /// <summary>
        /// Returns the execution error at the specified index.
        /// </summary>
        public ExecutionError this[int index] => List != null ? List[index] : throw new IndexOutOfRangeException();

        /// <inheritdoc/>
        public IEnumerator<ExecutionError> GetEnumerator() => (List ?? Enumerable.Empty<ExecutionError>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Optimization for validation "green path" - does not allocate memory in managed heap.
    /// </summary>
    internal sealed class EmptyExecutionErrors : ExecutionErrors
    {
        private EmptyExecutionErrors() { }

        public static readonly EmptyExecutionErrors Instance = new();

        public override void Add(ExecutionError error) => throw new NotSupportedException();

        public override void AddRange(IEnumerable<ExecutionError> errors) => throw new NotSupportedException();
    }
}
