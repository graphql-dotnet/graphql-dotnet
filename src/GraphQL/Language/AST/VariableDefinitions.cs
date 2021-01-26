using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of variable definition nodes within a document.
    /// </summary>
    public class VariableDefinitions : IEnumerable<VariableDefinition>
    {
        internal List<VariableDefinition> List { get; private set; }

        internal VariableDefinitions(int capacity)
        {
            List = new List<VariableDefinition>(capacity);
        }

        /// <summary>
        /// Creates an instance of a list of variable definition nodes within a document.
        /// </summary>
        public VariableDefinitions()
        {
        }

        /// <summary>
        /// Adds a variable definition node to the list.
        /// </summary>
        public void Add(VariableDefinition variable) => (List ??= new List<VariableDefinition>()).Add(variable ?? throw new ArgumentNullException(nameof(variable)));

        /// <inheritdoc/>
        public IEnumerator<VariableDefinition> GetEnumerator() => (List ?? Enumerable.Empty<VariableDefinition>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override string ToString() => List?.Count > 0 ? $"VariableDefinitions{{{string.Join(", ", List)}}}" : "VariableDefinitions(Empty)";
    }
}
