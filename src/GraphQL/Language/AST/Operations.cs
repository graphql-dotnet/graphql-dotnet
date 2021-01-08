using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of operation nodes within a document.
    /// </summary>
    public class Operations : IEnumerable<Operation>
    {
        private List<Operation> _operations;

        /// <summary>
        /// Returns the number of operation nodes the list contains.
        /// </summary>
        public int Count => _operations?.Count ?? 0;

        /// <summary>
        /// Adds an operation node to the list.
        /// </summary>
        public void Add(Operation operation) => (_operations ??= new List<Operation>()).Add(operation ?? throw new ArgumentNullException(nameof(operation)));

        /// <summary>
        /// Returns the first operation in the list that matches the specified name, or <see langword="null"/> if none are matched.
        /// </summary>
        public Operation WithName(string operationName)
        {
            // DO NOT USE LINQ ON HOT PATH
            if (_operations != null)
            {
                foreach (var op in _operations)
                {
                    if (op.Name == operationName)
                        return op;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public IEnumerator<Operation> GetEnumerator() => (_operations ?? Enumerable.Empty<Operation>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
