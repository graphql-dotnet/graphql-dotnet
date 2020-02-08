using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class ExpressionFieldResolver<TSourceType, TProperty> : IFieldResolverInternal
    {
        private readonly Func<TSourceType, TProperty> _property;

        public ExpressionFieldResolver(Expression<Func<TSourceType, TProperty>> property)
        {
            _property = property.Compile();
        }

        public Task SetResultAsync(IResolveFieldContext context)
        {
            context.Result = _property((TSourceType)context.Source);
            return Task.CompletedTask;
        }
    }
}
