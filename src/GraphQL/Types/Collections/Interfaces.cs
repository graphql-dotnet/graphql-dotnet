using System.Collections;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents a set of instances of supported GraphQL interface types for <see cref="IImplementInterfaces"/> i.e <see cref="ObjectGraphType{TSourceType}"/>.
    /// </summary>
    public class Interfaces : IEnumerable<Type>
    {
        internal List<Type> List { get; } = new List<Type>();

        /// <summary>
        /// Gets the count of supported GraphQL interface types.
        /// </summary>
        public int Count => List.Count;

        /// <summary>
        /// Adds a GraphQL interface graph type to the list of GraphQL interfaces implemented by this graph type.
        /// </summary>
        public void Add<TInterface>()
            where TInterface : IInterfaceGraphType
        {
            if (!List.Contains(typeof(TInterface)))
                List.Add(typeof(TInterface));
        }

        /// <summary>
        /// Adds a GraphQL interface graph type to the list of GraphQL interfaces implemented by this graph type.
        /// </summary>
        public void Add(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(IInterfaceGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Interface '{type.Name}' must implement {nameof(IInterfaceGraphType)}", nameof(type));
            }

            if (!List.Contains(type))
                List.Add(type);
        }

        /// <summary>
        /// Determines if the specified interface type is in the list.
        /// </summary>
        public bool Contains(Type type) => List.Contains(type ?? throw new ArgumentNullException(nameof(type)));

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<Type> GetEnumerator() => List.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
