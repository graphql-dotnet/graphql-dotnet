using System.Threading.Tasks;

namespace GraphQL
{
    /// <summary>
    /// Task extensions.
    /// </summary>
    public static class TaskExtensions
    {

        /// <summary>
        /// Returns a completed task. Equivalent to Task.CompletedTask.
        /// </summary>
#if NETSTANDARD2_0
        public static Task CompletedTask => Task.CompletedTask;
#else
        public static Task CompletedTask { get; } = Task.FromResult(0);
#endif

    }
}
