using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

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
