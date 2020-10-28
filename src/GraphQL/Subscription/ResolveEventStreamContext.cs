namespace GraphQL.Subscription
{
    public class ResolveEventStreamContext<T> : ResolveFieldContext<T>, IResolveEventStreamContext<T>
    {
        public ResolveEventStreamContext() { }

        public ResolveEventStreamContext(IResolveEventStreamContext context) : base(context) { }
    }

    public class ResolveEventStreamContext : ResolveEventStreamContext<object>, IResolveEventStreamContext
    {
    }
}
