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
        internal List<VariableDefinition> VariablesList { get; private set; }

        /// <summary>
        /// Adds a variable definition node to the list.
        /// </summary>
        public void Add(VariableDefinition variable) => (VariablesList ??= new List<VariableDefinition>()).Add(variable ?? throw new ArgumentNullException(nameof(variable)));

        /// <inheritdoc/>
        public IEnumerator<VariableDefinition> GetEnumerator() => (VariablesList ?? Enumerable.Empty<VariableDefinition>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override string ToString() => VariablesList?.Count > 0 ? $"VariableDefinitions{{{string.Join(", ", VariablesList)}}}" : "VariableDefinitions(Empty)";
    }
}
