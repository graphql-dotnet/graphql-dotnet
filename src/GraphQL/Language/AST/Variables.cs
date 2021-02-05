using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Contains a list of variables (name &amp; value tuples) that have been gathered from the document and attached <see cref="Inputs"/>.
    /// </summary>
    public class Variables : IEnumerable<Variable>
    {
        private List<Variable> _variables;

        /// <summary>
        /// Adds a variable to the list.
        /// </summary>
        public void Add(Variable variable)
        {
            if (variable == null)
                throw new ArgumentNullException(nameof(variable));

            _variables ??= new List<Variable>();

            _variables.Add(variable);
        }

        /// <summary>
        /// Returns the first variable with a matching name, or <paramref name="defaultValue"/> if none are found.
        /// </summary>
        public object ValueFor(string name, object defaultValue = null)
        {
            var variable = _variables?.FirstOrDefault(v => v.Name == name);
            return variable != null && variable.ValueSpecified
                ? variable.Value
                : defaultValue;
        }

        /// <inheritdoc/>
        public IEnumerator<Variable> GetEnumerator()
        {
            if (_variables == null)
                return Enumerable.Empty<Variable>().GetEnumerator();

            return _variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override string ToString() => _variables?.Count > 0 ? $"Variables{{{string.Join(", ", _variables)}}}" : "Variables(Empty)";
    }

}
