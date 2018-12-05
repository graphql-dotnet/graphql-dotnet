using System;
using System.Linq.Expressions;
using GraphQL.Execution;
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

        public TProperty Resolve(ResolveFieldContext context)
        {
            return _property((TSourceType)context.Source);
        }

        public TProperty Resolve(ExecutionContext context, ExecutionNode node)
        {
            return _property((TSourceType)node.Source);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }

        object IFieldResolver.Resolve(ExecutionContext context, ExecutionNode node)
        {
            return Resolve(context, node);
        }
    }
}
