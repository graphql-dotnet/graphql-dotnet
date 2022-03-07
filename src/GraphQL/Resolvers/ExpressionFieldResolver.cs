using System.Linq.Expressions;
using System.Reflection;

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
        /// <see cref="Task{TResult}"/> and <see cref="ValueTask{TResult}"/> return types are also supported.
        /// </summary>
        public ExpressionFieldResolver(Expression<Func<TSourceType, TProperty>> property)
        {
            var param = Expression.Parameter(typeof(IResolveFieldContext), "context");
            var source = Expression.MakeMemberAccess(param, _sourcePropertyInfo);
            var cast = Expression.Convert(source, typeof(TSourceType));
            var body = property.Body.Replace(property.Parameters[0], cast);
            _resolver = MemberResolver.BuildFieldResolverInternal(param, body);
        }

        private static readonly PropertyInfo _sourcePropertyInfo = typeof(IResolveFieldContext).GetProperty(nameof(IResolveFieldContext.Source))!;

        /// <inheritdoc/>
        ValueTask<object?> IFieldResolver.ResolveAsync(IResolveFieldContext context)
            => _resolver(context);
    }
}
