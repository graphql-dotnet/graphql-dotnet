#nullable enable

using System.Threading.Tasks;

namespace GraphQL.Validation
{
    internal static class TaskHelper
    {
        public static Task<INodeVisitor> ToTask(this INodeVisitor visitor) => Task.FromResult(visitor);
    }
}
