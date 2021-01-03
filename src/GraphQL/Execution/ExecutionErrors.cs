using System;
using System.Collections;
using System.Collections.Generic;

namespace GraphQL
{
    /// <summary>
    /// Contains a list of execution errors.
    /// </summary>
    public class ExecutionErrors : IEnumerable<ExecutionError>
    {
        private readonly List<ExecutionError> _errors = new List<ExecutionError>();

        /// <summary>
        /// Adds an execution error to the list.
        /// </summary>
        public virtual void Add(ExecutionError error)
        {
            _errors.Add(error ?? throw new ArgumentNullException(nameof(error)));
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
        public int Count => _errors.Count;

        /// <summary>
        /// Returns the execution error at the specified index.
        /// </summary>
        public ExecutionError this[int index] => _errors[index];

        /// <inheritdoc/>
        public IEnumerator<ExecutionError> GetEnumerator() => _errors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Optimization for validation "green path" - does not allocate memory in managed heap.
    /// </summary>
    internal sealed class EmptyExecutionErrors : ExecutionErrors
    {
        private EmptyExecutionErrors() { }

        public static readonly EmptyExecutionErrors Instance = new EmptyExecutionErrors();

        public override void Add(ExecutionError error) => throw new NotSupportedException();

        public override void AddRange(IEnumerable<ExecutionError> errors) => throw new NotSupportedException();
    }
}
