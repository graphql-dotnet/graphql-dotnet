using GraphQL.Execution;

namespace GraphQL.Subscription
{
    internal class ResolveEventStreamContextAdapter<T> : ResolveFieldContextAdapter<T>, IResolveEventStreamContext<T>
    {
        public ResolveEventStreamContextAdapter(IResolveFieldContext context) : base(context) { }
    }
}
