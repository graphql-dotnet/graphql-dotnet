using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    public class Fields : IEnumerable<Field>
    {
        private readonly Dictionary<string, Field> _fields;

        private Fields(Dictionary<string, Field> fields)
        {
            _fields = fields;
        }

        public static Fields Empty() => new Fields(new Dictionary<string, Field>());

        public void Add(Field field)
        {
            var name = field.Alias ?? field.Name;

            if (_fields.TryGetValue(name, out Field original))
            {
                _fields[name] = original.MergeSelectionSet(field);
            }
            else
            {
                _fields[name] = field;
            }
        }

        public IEnumerator<Field> GetEnumerator()
        {
            return _fields.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static implicit operator Dictionary<string, Field>(Fields fields) => fields._fields;
    }
}

