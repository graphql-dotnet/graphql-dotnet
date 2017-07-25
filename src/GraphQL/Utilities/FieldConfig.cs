using System;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class FieldConfig : MetadataProvider
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IFieldResolver ResolverValue { get; set; }

        public void Resolver<TSourceType, TReturnType>(Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            ResolverValue = new FuncFieldResolver<TSourceType, TReturnType>(resolver);
        }

        public void Resolver<TReturnType>(Func<ResolveFieldContext, TReturnType> resolver)
        {
            ResolverValue = new FuncFieldResolver<TReturnType>(resolver);
        }
    }
}