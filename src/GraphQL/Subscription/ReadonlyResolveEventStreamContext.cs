using GraphQL.Execution;

namespace GraphQL.Subscription
{
    /// <summary>
    /// A readonly implementation of <see cref="IResolveEventStreamContext{object}"/>
    /// </summary>
    public class ReadonlyResolveEventStreamContext : ReadonlyResolveFieldContext, IResolveEventStreamContext<object>
    {
        public ReadonlyResolveEventStreamContext(ExecutionNode node, ExecutionContext context) : base(node, context) { }
    }
}
