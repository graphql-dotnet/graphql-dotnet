using System.Collections;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents a set of instances of supported GraphQL interface types for <see cref="IImplementInterfaces"/> i.e <see cref="ObjectGraphType{TSourceType}"/>.
    /// </summary>
    public class ResolvedInterfaces : IEnumerable<IInterfaceGraphType>
    {
        internal List<IInterfaceGraphType> List { get; } = new List<IInterfaceGraphType>();

        /// <summary>
        /// Gets the count of supported GraphQL interface types.
        /// </summary>
        public int Count => List.Count;

        internal void Add(IInterfaceGraphType type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!List.Contains(type))
                List.Add(type);
        }

        /// <summary>
        /// Determines if the specified interface type is in the list.
        /// </summary>
        public bool Contains(IInterfaceGraphType type) => List.Contains(type ?? throw new ArgumentNullException(nameof(type)));

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<IInterfaceGraphType> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
