using System.Collections;
using GraphQLParser;

namespace GraphQL.Types
{
    /// <summary>
    /// An interface that represents a set of fields for any <see cref="IComplexGraphType"/>
    /// i.e <see cref="InputObjectGraphType{TSourceType}"/>, <see cref="InterfaceGraphType{TSourceType}"/> and <see cref="ObjectGraphType{TSourceType}"/>.
    /// </summary>
    public interface ITypeFields // intentionally does not implement IEnumerable to workaround CS0695 (see below)
    {
        /// <summary>
        /// Gets the count of fields.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Returns the <see cref="FieldType"/> for the field matching the specified name that
        /// is configured for this graph type, or <see langword="null"/> if none is found.
        /// </summary>
        FieldType? GetField(ROM name);

        /// <summary>
        /// Returns a set of fields as <see cref="IEnumerable{T}"/>.
        /// </summary>
        IEnumerable<FieldType> AsEnumerable();
    }

    /// <summary>
    /// A class that represents a set of fields for any <see cref="IComplexGraphType"/>
    /// i.e <see cref="InputObjectGraphType{TSourceType}"/>, <see cref="InterfaceGraphType{TSourceType}"/> and <see cref="ObjectGraphType{TSourceType}"/>.
    /// </summary>
    public class TypeFields<TFieldType> : ITypeFields, IEnumerable<TFieldType>
        where TFieldType : FieldType
    {
        internal List<TFieldType> List { get; } = new();

        /// <inheritdoc />
        public int Count => List.Count;

        internal void Add(TFieldType field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            if (!List.Contains(field))
                List.Add(field);
        }

        /// <summary>
        /// Searches the list for a field specified by its name and returns it.
        /// </summary>
        public TFieldType? Find(string name)
        {
            foreach (var field in List)
            {
                if (field.Name == name)
                    return field;
            }

            return null;
        }

        /// <summary>
        /// Searches the list for a field specified by its name and returns it.
        /// </summary>
        public TFieldType? Find(ROM name)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var field in List)
            {
                if (field.Name == name)
                    return field;
            }

            return null;
        }

        /// <inheritdoc/>
        public FieldType? GetField(ROM name) => Find(name);

        /// <summary>
        /// Determines if the specified field type is in the list.
        /// </summary>
        public bool Contains(TFieldType field) => List.Contains(field ?? throw new ArgumentNullException(nameof(field)));

        /// <summary>
        /// Determines if the specified field type is in the list.
        /// </summary>
        public bool Contains(IFieldType field) => List.Contains((TFieldType)field ?? throw new ArgumentNullException(nameof(field)));

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<TFieldType> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerable<FieldType> AsEnumerable() => this; // HACK: Error CS0695	'TypeFields<TFieldType>' cannot implement both 'IEnumerable<TFieldType>' and 'IEnumerable<FieldType>' because they may unify for some type parameter substitutions
    }
}
