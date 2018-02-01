using System.Threading.Tasks;

namespace GraphQL
{
    public static class TaskExtensions
    {

#if NETSTANDARD2_0
        public static Task CompletedTask => Task.CompletedTask;
#else
        public static Task CompletedTask { get; } = Task.FromResult(0);
#endif

    }
}
