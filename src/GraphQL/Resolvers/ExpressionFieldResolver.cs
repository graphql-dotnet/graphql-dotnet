using System;
using System.Linq.Expressions;
using GraphQL.Execution;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// Returns a value from the field's graph type's source object, based on a predefined expression.
    /// </summary>
    public class ExpressionFieldResolver<TSourceType, TProperty> : IFieldResolver<TProperty>, IOptimizedFieldResolver
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

        public object Resolve(ExecutionNode node, ExecutionContext context) => _property((TSourceType)node.Source);

        /// <inheritdoc/>
        public TProperty Resolve(IResolveFieldContext context) => _property((TSourceType)context.Source);
        
        object IFieldResolver.Resolve(IResolveFieldContext context) => Resolve(context);
    }
}
