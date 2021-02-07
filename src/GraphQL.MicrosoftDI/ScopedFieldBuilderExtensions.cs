using System;
using System.Threading.Tasks;
using GraphQL.Builders;

namespace GraphQL.Extensions.DI.Microsoft
{
    public static class ScopedFieldBuilderExtensions
    {
        public static FieldBuilder<TSourceType, TReturnType> ResolveScopedAsync<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
            => builder.Resolve(new ScopedAsyncFieldResolver<TSourceType, TReturnType>(resolver));

        public static FieldBuilder<TSourceType, TReturnType> ResolveScoped<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, TReturnType> resolver)
            => builder.Resolve(new ScopedFieldResolver<TSourceType, TReturnType>(resolver));

        public static ResolverBuilder<TSourceType, TReturnType> Resolve<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder)
            => new ResolverBuilder<TSourceType, TReturnType>(builder, false);
    }
}
