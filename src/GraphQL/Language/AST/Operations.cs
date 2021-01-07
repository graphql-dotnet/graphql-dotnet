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
        private readonly List<Operation> _operations = new List<Operation>();

        /// <summary>
        /// Returns the number of operation nodes the list contains.
        /// </summary>
        public int Count => _operations.Count;

        /// <summary>
        /// Adds an operation node to the list.
        /// </summary>
        /// <param name="operation"></param>
        public void Add(Operation operation)
        {
            _operations.Add(operation ?? throw new ArgumentNullException(nameof(operation)));
        }

        /// <summary>
        /// Returns the first operation in the list that matches the specified name, or <see langword="null"/> if none are matched.
        /// </summary>
        public Operation WithName(string operationName)
        {
            return _operations.FirstOrDefault(op => op.Name == operationName);
        }

        /// <inheritdoc/>
        public IEnumerator<Operation> GetEnumerator()
        {
            return _operations.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
