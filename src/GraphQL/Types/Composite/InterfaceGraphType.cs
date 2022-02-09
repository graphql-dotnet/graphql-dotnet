namespace GraphQL.Types
{
    /// <summary>
    /// Represents a GraphQL interface graph type.
    /// </summary>
    public interface IInterfaceGraphType : IAbstractGraphType, IComplexGraphType
    {
    }

    /// <inheritdoc cref="InterfaceGraphType"/>
    public class InterfaceGraphType<TSource> : ComplexGraphType<TSource>, IInterfaceGraphType
    {
        /// <inheritdoc/>
        public PossibleTypes PossibleTypes { get; } = new PossibleTypes();

        /// <inheritdoc/>
        public Func<object, IObjectGraphType?>? ResolveType { get; set; }

        /// <inheritdoc/>
        public void AddPossibleType(IObjectGraphType type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            this.IsValidInterfaceFor(type, throwError: true);
            PossibleTypes.Add(type);
        }
    }

    /// <inheritdoc cref="IInterfaceGraphType"/>
    public class InterfaceGraphType : InterfaceGraphType<object>
    {
    }
}
