using System;
using System.Linq.Expressions;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class ExpressionFieldResolver<TSourceType, TProperty> : IFieldResolver<TProperty>
    {
        private readonly Expression<Func<TSourceType, TProperty>> _property;

        public ExpressionFieldResolver(Expression<Func<TSourceType, TProperty>> property)
        {
            _property = property;
        }

        public TProperty Resolve(ResolveFieldContext context)
        {
            return _property.Compile()(context.As<TSourceType>().Source);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
