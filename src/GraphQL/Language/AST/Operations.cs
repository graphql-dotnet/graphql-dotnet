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
        internal List<Operation>? List { get; private set; }

        /// <summary>
        /// Returns the number of operation nodes the list contains.
        /// </summary>
        public int Count => List?.Count ?? 0;

        /// <summary>
        /// Adds an operation node to the list.
        /// </summary>
        public void Add(Operation operation) => (List ??= new List<Operation>()).Add(operation ?? throw new ArgumentNullException(nameof(operation)));

        /// <summary>
        /// Returns the first operation in the list that matches the specified name, or <see langword="null"/> if none are matched.
        /// </summary>
        public Operation? WithName(string operationName)
        {
            // DO NOT USE LINQ ON HOT PATH
            if (List != null)
            {
                foreach (var op in List)
                {
                    if (op.Name == operationName)
                        return op;
                }
            }

            return null;
        }

        // This method avoids LINQ and 'List+Enumerator<Operation>' allocation
        internal Operation? FirstOrDefault() => List?[0];

        /// <inheritdoc/>
        public IEnumerator<Operation> GetEnumerator() => (List ?? Enumerable.Empty<Operation>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
