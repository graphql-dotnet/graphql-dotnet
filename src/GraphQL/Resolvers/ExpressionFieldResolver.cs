using System.Linq.Expressions;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// Returns a value from the field's graph type's source object, based on a predefined expression.
    /// <br/><br/>
    /// Note: this class uses dynamic compilation and therefore allocates a relatively large amount of
    /// memory in managed heap, ~1KB. Do not use this class in cases with limited memory requirements.
    /// </summary>
    public class ExpressionFieldResolver<TSourceType, TProperty> : IFieldResolver<TProperty>
    {
        private readonly Func<TSourceType, TProperty> _property;

        /// <summary>
        /// Initializes a new instance that runs the specified expression when resolving a field.
        /// </summary>
        /// <param name="property"></param>
        public ExpressionFieldResolver(Expression<Func<TSourceType, TProperty>> property)
        {
            _property = property.Compile();
        }

        /// <inheritdoc/>
        public ValueTask<TProperty?> ResolveAsync(IResolveFieldContext context)
            => new ValueTask<TProperty?>(_property((TSourceType)context.Source!));

        ValueTask<object?> IFieldResolver.ResolveAsync(IResolveFieldContext context)
            => new ValueTask<object?>(_property((TSourceType)context.Source!));
    }
}
