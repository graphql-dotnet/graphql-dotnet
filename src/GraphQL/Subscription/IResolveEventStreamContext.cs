#nullable enable

namespace GraphQL.Subscription
{
    public interface IResolveEventStreamContext : IResolveFieldContext
    {
    }

    public interface IResolveEventStreamContext<out TSource> : IResolveFieldContext<TSource>, IResolveEventStreamContext
    {
    }
}
