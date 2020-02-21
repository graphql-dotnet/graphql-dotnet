using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.DI
{
    public class DIScopedFieldResolver<TReturnType> : AsyncFieldResolver<TReturnType>
    {
        public DIScopedFieldResolver(Func<IResolveFieldContext, Task<TReturnType>> resolver) : base(ScopeResolver(resolver)) { }

        private static Func<IResolveFieldContext, Task<TReturnType>> ScopeResolver(Func<IResolveFieldContext, Task<TReturnType>> resolver)
        {
            return async (context) =>
            {
                var serviceProvider = AsyncServiceProvider.Current ?? throw new InvalidOperationException("No service provider defined in this context");
                try
                {
                    using (var newScope = serviceProvider.CreateScope())
                    {
                        AsyncServiceProvider.Current = newScope.ServiceProvider;
                        var ret = resolver(context);
                        return await ret.ConfigureAwait(false);
                    }
                }
                finally
                {
                    AsyncServiceProvider.Current = serviceProvider;
                }
            };
        }
    }

    public class DIScopedFieldResolver<TSourceType, TReturnType> : AsyncFieldResolver<TSourceType, TReturnType>
    {
        public DIScopedFieldResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver) : base(ScopeResolver(resolver)) { }

        private static Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> ScopeResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
        {
            return async (context) =>
            {
                var serviceProvider = AsyncServiceProvider.Current ?? throw new InvalidOperationException("No service provider defined in this context");
                try
                {
                    using (var newScope = serviceProvider.CreateScope())
                    {
                        AsyncServiceProvider.Current = newScope.ServiceProvider;
                        var ret = resolver(context);
                        return await ret.ConfigureAwait(false);
                    }
                }
                finally
                {
                    AsyncServiceProvider.Current = serviceProvider;
                }
            };
        }
    }
}
