using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class Variables : IEnumerable<Variable>
    {
        private List<Variable> _variables;

        public void Add(Variable variable)
        {
            if (variable == null)
                throw new ArgumentNullException(nameof(variable));

            if (_variables == null)
                _variables = new List<Variable>();

            _variables.Add(variable);
        }

        public object ValueFor(string name)
        {
            var variable = _variables?.FirstOrDefault(v => v.Name == name);
            return variable?.Value;
        }

        /// <inheritdoc />
        public IEnumerator<Variable> GetEnumerator()
        {
            if (_variables == null)
                return Enumerable.Empty<Variable>().GetEnumerator();

            return _variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public override string ToString() => _variables?.Count > 0 ? $"Variables{{{string.Join(", ", _variables)}}}" : "Variables(Empty)";
    }

    public class VariableDefinitions : IEnumerable<VariableDefinition>
    {
        private List<VariableDefinition> _variables;

        public void Add(VariableDefinition variable)
        {
            if (variable == null)
                throw new ArgumentNullException(nameof(variable));

            if (_variables == null)
                _variables = new List<VariableDefinition>();

            _variables.Add(variable);
        }

        /// <inheritdoc />
        public IEnumerator<VariableDefinition> GetEnumerator()
        {
            if (_variables == null)
                return Enumerable.Empty<VariableDefinition>().GetEnumerator();

            return _variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public override string ToString() => _variables?.Count > 0 ? $"VariableDefinitions{{{string.Join(", ", _variables)}}}" : "VariableDefinitions(Empty)";
    }
}
