using System.Collections;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents a set of fields for <see cref="IComplexGraphType"/> i.e <see cref="ComplexGraphType{TSourceType}"/>.
    /// </summary>
    public class TypeFields : IEnumerable<FieldType>
    {
        internal List<FieldType> List { get; } = new List<FieldType>();

        /// <summary>
        /// Gets the count of fields.
        /// </summary>
        public int Count => List.Count;

        internal void Add(FieldType field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            if (!List.Contains(field))
                List.Add(field);
        }

        /// <summary>
        /// Searches the list for a field specified by its name and returns it.
        /// </summary>
        public FieldType? Find(string name)
        {
            foreach (var field in List)
            {
                if (field.Name == name)
                    return field;
            }

            return null;
        }

        /// <summary>
        /// Determines if the specified field type is in the list.
        /// </summary>
        public bool Contains(FieldType field) => List.Contains(field ?? throw new ArgumentNullException(nameof(field)));

        /// <summary>
        /// Determines if the specified field type is in the list.
        /// </summary>
        public bool Contains(IFieldType field) => List.Contains((FieldType)field ?? throw new ArgumentNullException(nameof(field)));

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<FieldType> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
