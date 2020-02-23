using System;
using System.Linq.Expressions;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// Returns a value from the field's graph type's source object, based on a predefined expression
    /// </summary>
    public class ExpressionFieldResolver<TSourceType, TProperty> : IFieldResolver<TProperty>
    {
        private readonly Func<TSourceType, TProperty> _property;

        public ExpressionFieldResolver(Expression<Func<TSourceType, TProperty>> property)
        {
            _property = property.Compile();
        }

        public TProperty Resolve(IResolveFieldContext context)
        {
            return _property((TSourceType)context.Source);
        }

        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
