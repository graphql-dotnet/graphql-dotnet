namespace GraphQL.Types
{
    /// <summary>
    /// Represents an interface for all object (that is, having their own properties) output graph types.
    /// </summary>
    public interface IObjectGraphType : IComplexGraphType, IImplementInterfaces
    {
        /// <summary>
        /// Gets or sets a delegate that determines if the specified object is valid for this graph type.
        /// </summary>
        Func<object, bool>? IsTypeOf { get; set; }

        /// <summary>
        /// Adds an instance of <see cref="IInterfaceGraphType"/> to the list of interface instances supported by this object graph type.
        /// </summary>
        void AddResolvedInterface(IInterfaceGraphType graphType);
    }

    /// <summary>
    /// Represents a default base class for all object (that is, having their own properties) output graph types.
    /// </summary>
    /// <typeparam name="TSourceType">Typically the type of the object that this graph represents. More specifically, the .NET type of the source property within field resolvers for this graph.</typeparam>
    public class ObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IObjectGraphType
    {
        /// <inheritdoc/>
        public Func<object, bool>? IsTypeOf { get; set; }

        /// <inheritdoc/>
        public ObjectGraphType()
        {
            if (typeof(TSourceType) != typeof(object))
                IsTypeOf = instance => instance is TSourceType;
        }

        /// <inheritdoc/>
        public void AddResolvedInterface(IInterfaceGraphType graphType)
        {
            if (graphType == null)
                throw new ArgumentNullException(nameof(graphType));

            _ = graphType.IsValidInterfaceFor(this, throwError: true);
            ResolvedInterfaces.Add(graphType);
        }

        /// <inheritdoc/>
        public Interfaces Interfaces { get; } = new Interfaces();

        /// <inheritdoc/>
        public ResolvedInterfaces ResolvedInterfaces { get; } = new ResolvedInterfaces();

        /// <summary>
        /// Adds a GraphQL interface graph type to the list of GraphQL interfaces implemented by this graph type.
        /// </summary>
        public void Interface<TInterface>()
            where TInterface : IInterfaceGraphType
            => Interfaces.Add<TInterface>();

        /// <summary>
        /// Adds a GraphQL interface graph type to the list of GraphQL interfaces implemented by this graph type.
        /// </summary>
        public void Interface(Type type) => Interfaces.Add(type);
    }

    /// <summary>
    /// Represents a default base class for all object (that is, having their own properties) output graph types.
    /// </summary>
    public class ObjectGraphType : ObjectGraphType<object?>
    {
    }
}
