using System.Linq.Expressions;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// Returns a value from the field's graph type's source object, based on a predefined expression.
    /// <br/><br/>
    /// Supports asynchronous return types.
    /// <br/><br/>
    /// Note: this class uses dynamic compilation and therefore allocates a relatively large amount of
    /// memory in managed heap, ~1KB. Do not use this class in cases with limited memory requirements.
    /// </summary>
    public class ExpressionFieldResolver<TSourceType, TProperty> : IFieldResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<object?>> _resolver;

        /// <summary>
        /// Initializes a new instance that runs the specified expression when resolving a field.
        /// </summary>
        public ExpressionFieldResolver(Expression<Func<TSourceType, TProperty>> property)
        {
            _resolver = MemberResolver.BuildFieldResolverInternal(property.Parameters[0], property.Body);
        }

        /// <inheritdoc/>
        ValueTask<object?> IFieldResolver.ResolveAsync(IResolveFieldContext context)
            => _resolver(context);
    }
}
