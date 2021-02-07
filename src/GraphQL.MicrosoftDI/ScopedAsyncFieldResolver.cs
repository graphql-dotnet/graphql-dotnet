using System;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{
    public class ScopedAsyncFieldResolver<TReturnType> : AsyncFieldResolver<TReturnType>
    {
        public ScopedAsyncFieldResolver(Func<IResolveFieldContext, Task<TReturnType>> resolver) : base(GetScopedResolver(resolver)) { }

        private static Func<IResolveFieldContext, Task<TReturnType>> GetScopedResolver(Func<IResolveFieldContext, Task<TReturnType>> resolver)
        {
            return async (context) =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return await resolver(new ScopedResolveFieldContextAdapter(context, scope.ServiceProvider));
                }
            };
        }
    }

    public class ScopedAsyncFieldResolver<TSourceType, TReturnType> : AsyncFieldResolver<TReturnType>
    {
        public ScopedAsyncFieldResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver) : base(GetScopedResolver(resolver)) { }

        private static Func<IResolveFieldContext, Task<TReturnType>> GetScopedResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            return async (context) =>
            {
                using (var scope = (context.RequestServices ?? throw new MissingRequestServicesException()).CreateScope())
                {
                    return await resolver(new ScopedResolveFieldContextAdapter<TSourceType>(context, scope.ServiceProvider));
                }
            };
        }
    }
}
