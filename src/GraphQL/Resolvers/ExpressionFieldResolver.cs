using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class ExpressionFieldResolver<TSourceType, TProperty> : IFieldResolver<TProperty>
    {
        private readonly Func<TSourceType, TProperty> _property;

        public ExpressionFieldResolver(Expression<Func<TSourceType, TProperty>> property)
        {
            _property = property.Compile();
        }

        public Task<TProperty> ResolveAsync(IResolveFieldContext context)
        {
            return Task.FromResult(_property((TSourceType)context.Source));
        }

        Task<object> IFieldResolver.ResolveAsync(IResolveFieldContext context)
        {
            return Task.FromResult((object)_property((TSourceType)context.Source));
        }
    }
}
