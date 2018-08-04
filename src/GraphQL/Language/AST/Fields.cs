using System.Collections;
using System.Collections.Generic;
using GraphQL.Language.AST;

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
            _fields[name] = _fields.ContainsKey(name) ? MergeField(_fields[name], field) : field;
        }

        public IEnumerator<Field> GetEnumerator()
        {
            return _fields.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private Field MergeField(Field originalField, Field newField) => originalField.MergeSelectionSet(newField);

        public static implicit operator Dictionary<string, Field>(Fields fields) => fields._fields;
    }
}

