using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language
{
    public class Variables : IEnumerable<Variable>
    {
        private readonly List<Variable> _variables = new List<Variable>();

        public void Add(Variable variable)
        {
            _variables.Add(variable);
        }

        public IEnumerator<Variable> GetEnumerator()
        {
            return _variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}