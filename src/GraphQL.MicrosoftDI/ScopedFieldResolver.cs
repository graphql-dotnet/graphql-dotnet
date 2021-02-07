using System;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{
    public class ScopedFieldResolver<TReturnType> : FuncFieldResolver<TReturnType>
    {
        public ScopedFieldResolver(Func<IResolveFieldContext, TReturnType> resolver) : base(GetScopedResolver(resolver)) { }

        private static Func<IResolveFieldContext, TReturnType> GetScopedResolver(Func<IResolveFieldContext, TReturnType> resolver)
        {
            return context =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return resolver(new ScopedResolveFieldContextAdapter(context, scope.ServiceProvider));
                }
            };
        }
    }

    public class ScopedFieldResolver<TSourceType, TReturnType> : FuncFieldResolver<TReturnType>
    {
        public ScopedFieldResolver(Func<IResolveFieldContext<TSourceType>, TReturnType> resolver) : base(GetScopedResolver(resolver)) { }

        private static Func<IResolveFieldContext, TReturnType> GetScopedResolver(Func<IResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            return context =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return resolver(new ScopedResolveFieldContextAdapter<TSourceType>(context, scope.ServiceProvider));
                }
            };
        }
    }
}
