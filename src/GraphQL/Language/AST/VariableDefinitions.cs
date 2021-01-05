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
        private List<VariableDefinition> _variables;

        /// <summary>
        /// Adds a variable definition node to the list.
        /// </summary>
        public void Add(VariableDefinition variable)
        {
            if (variable == null)
                throw new ArgumentNullException(nameof(variable));

            (_variables ??= new List<VariableDefinition>()).Add(variable);
        }

        /// <inheritdoc/>
        public IEnumerator<VariableDefinition> GetEnumerator() => _variables?.GetEnumerator() ?? Enumerable.Empty<VariableDefinition>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override string ToString() => _variables?.Count > 0 ? $"VariableDefinitions{{{string.Join(", ", _variables)}}}" : "VariableDefinitions(Empty)";
    }
}
