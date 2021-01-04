using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of field nodes within a document.
    /// </summary>
    public class Fields : IEnumerable<Field>
    {
        private readonly Dictionary<string, Field> _fields;

        private Fields(Dictionary<string, Field> fields)
        {
            _fields = fields;
        }

        /// <summary>
        /// Returns a new instance that contains no field nodes.
        /// </summary>
        public static Fields Empty() => new Fields(new Dictionary<string, Field>());

        /// <summary>
        /// Adds a field node to the list.
        /// </summary>
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

        /// <inheritdoc/>
        public IEnumerator<Field> GetEnumerator() => _fields.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static implicit operator Dictionary<string, Field>(Fields fields) => fields._fields;
    }
}

