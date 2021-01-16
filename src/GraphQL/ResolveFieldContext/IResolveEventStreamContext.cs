namespace GraphQL
{
    public interface IResolveEventStreamContext : IResolveFieldContext
    {
    }

    public interface IResolveEventStreamContext<out TSource> : IResolveFieldContext<TSource>, IResolveEventStreamContext
    {
    }
}
