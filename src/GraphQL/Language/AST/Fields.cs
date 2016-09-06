using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Fields : IEnumerable<Field>
    {
        private readonly List<Field> _fields = new List<Field>();

        public void Add(Field field)
        {
            _fields.Add(field);
        }

        public IEnumerator<Field> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
