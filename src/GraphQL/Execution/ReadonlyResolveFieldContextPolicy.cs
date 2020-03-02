using Microsoft.Extensions.ObjectPool;

namespace GraphQL.Execution
{
    internal sealed class ReadonlyResolveFieldContextPolicy : IPooledObjectPolicy<ReadonlyResolveFieldContext>
    {
        public ReadonlyResolveFieldContext Create() => new ReadonlyResolveFieldContext();

        public bool Return(ReadonlyResolveFieldContext obj)
        {
            obj.Clear();
            return true;
        }
    }
}
