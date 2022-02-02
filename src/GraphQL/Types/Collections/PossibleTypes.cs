using System.Collections;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents a set of possible types for <see cref="IAbstractGraphType"/> i.e. <see cref="InterfaceGraphType"/> or <see cref="UnionGraphType"/>.
    /// </summary>
    public class PossibleTypes : IEnumerable<IObjectGraphType>
    {
        internal List<IObjectGraphType> List { get; } = new List<IObjectGraphType>();

        /// <summary>
        /// Gets the count of possible types.
        /// </summary>
        public int Count => List.Count;

        internal void Add(IObjectGraphType type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!List.Contains(type))
                List.Add(type);
        }

        /// <summary>
        /// Determines if the specified graph type is in the list.
        /// </summary>
        public bool Contains(IObjectGraphType type) => List.Contains(type ?? throw new ArgumentNullException(nameof(type)));

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<IObjectGraphType> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
