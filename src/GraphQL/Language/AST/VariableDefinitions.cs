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

            _variables ??= new List<VariableDefinition>();

            _variables.Add(variable);
        }

        /// <inheritdoc/>
        public IEnumerator<VariableDefinition> GetEnumerator()
        {
            if (_variables == null)
                return Enumerable.Empty<VariableDefinition>().GetEnumerator();

            return _variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override string ToString() => _variables?.Count > 0 ? $"VariableDefinitions{{{string.Join(", ", _variables)}}}" : "VariableDefinitions(Empty)";
    }
}
