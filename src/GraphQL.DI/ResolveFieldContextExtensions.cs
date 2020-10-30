using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.DI
{
    public static class ResolveFieldContextExtensions
    {
        public static IResolveFieldContext<TSourceType> As<TSourceType>(this IResolveFieldContext context)
        {
            if (context is IResolveFieldContext<TSourceType> typedContext)
                return typedContext;

            return new ResolveFieldContextAdapter<TSourceType>(context);
        }
    }
}
