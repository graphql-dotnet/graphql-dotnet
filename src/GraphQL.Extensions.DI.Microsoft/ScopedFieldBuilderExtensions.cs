using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Builders;
using GraphQL.Types;

namespace GraphQL.Extensions.DI.Microsoft
{
    public static class ScopedFieldBuilderExtensions
    {
        public static FieldBuilder<TSourceType, TReturnType> ResolveAsyncScoped<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
            => builder.Resolve(new ScopedAsyncFieldResolver<TSourceType, TReturnType>(resolver));

        public static FieldBuilder<TSourceType, TReturnType> ResolveScoped<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, TReturnType> resolver)
            => builder.Resolve(new ScopedFieldResolver<TSourceType, TReturnType>(resolver));
    }
}
