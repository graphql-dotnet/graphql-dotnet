using System;
using System.Linq.Expressions;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// Returns a value from the field's graph type's source object, based on a predefined expression.
    /// </summary>
    public class ExpressionFieldResolver<TSourceType, TProperty> : IFieldResolver<TProperty>
    {
        private readonly Func<TSourceType, TProperty> _property;

        /// <summary>
        /// Initializes a new instance that runs the specified expression.
        /// </summary>
        /// <param name="property"></param>
        public ExpressionFieldResolver(Expression<Func<TSourceType, TProperty>> property)
        {
            _property = property.Compile();
        }

        /// <inheritdoc/>
        public TProperty Resolve(IResolveFieldContext context) => _property((TSourceType)context.Source);

        object IFieldResolver.Resolve(IResolveFieldContext context) => Resolve(context);
    }
}
