namespace GraphQL.Types
{
    /// <summary>
    /// Represents a default base class for all complex (that is, having their own properties) input and output graph types.
    /// </summary>
    public abstract class ComplexGraphType<TSourceType> : GraphType, IComplexGraphType
    {
        internal const string ORIGINAL_EXPRESSION_PROPERTY_NAME = nameof(ORIGINAL_EXPRESSION_PROPERTY_NAME);

        /// <inheritdoc/>
        protected ComplexGraphType()
        {
            if (typeof(IGraphType).IsAssignableFrom(typeof(TSourceType)) && GetType() != typeof(Introspection.__Type))
                throw new InvalidOperationException($"Cannot use graph type '{typeof(TSourceType).Name}' as a model for graph type '{GetType().Name}'. Please use a model rather than a graph type for {nameof(TSourceType)}.");

            Description ??= typeof(TSourceType).Description();
            DeprecationReason ??= typeof(TSourceType).ObsoleteMessage();
        }
    }
}
