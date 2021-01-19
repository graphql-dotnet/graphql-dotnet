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
                // Sets a new field selection node with the child field selection nodes merged with another field's child field selection nodes.
                _fields[name] = new Field(original.AliasNode, original.NameNode)
                {
                    Arguments = original.Arguments,
                    SelectionSet = original.SelectionSet.Merge(field.SelectionSet),
                    Directives = original.Directives,
                    SourceLocation = original.SourceLocation,
                };
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

