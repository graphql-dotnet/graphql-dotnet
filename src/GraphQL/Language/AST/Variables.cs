using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Execution;

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
        public void Add(Variable variable) => (_variables ??= new List<Variable>()).Add(variable ?? throw new ArgumentNullException(nameof(variable)));

        /// <summary>
        /// Returns the first variable with a matching name, or <paramref name="defaultValue"/> if none are found.
        /// </summary>
        public object ValueFor(string name, object defaultValue = null)
        {
            return ValueFor(name, out var value) ? value : defaultValue;
        }

        public bool ValueFor(string name, out object value)
        {
            // DO NOT USE LINQ ON HOT PATH
            if (_variables != null)
            {
                foreach (var v in _variables)
                {
                    if (v.Name == name)
                    {
                        value = v.Value;
                        return v.ValueSpecified;
                    }
                }
            }

            value = null;
            return false;
        }

        /// <inheritdoc/>
        public IEnumerator<Variable> GetEnumerator() => (_variables ?? Enumerable.Empty<Variable>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override string ToString() => _variables?.Count > 0 ? $"Variables{{{string.Join(", ", _variables)}}}" : "Variables(Empty)";
    }

}
