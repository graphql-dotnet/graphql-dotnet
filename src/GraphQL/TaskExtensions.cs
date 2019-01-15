using System.Threading.Tasks;

namespace GraphQL
{
    /// <summary>
    /// Task extensions.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Gets the result of a completed <see cref="Task&lt;TResult&gt;"/> when TResult is not known
        /// </summary>
        /// <remarks>
        /// The Task should already be awaited or this call will block.
        /// This will also throw an exception if the task is not Task&lt;TResult&gt;.
        /// </remarks>
        /// <param name="task">A task that has already been awaited</param>
        /// <returns></returns>
        internal static object GetResult(this Task task)
        {
            if (task is Task<object> to)
            {
                // Most performant if available
                return to.Result;
            }
            else
            {
                // Using dynamic is over 10x faster than reflection
                return ((dynamic)task).Result;
            }
        }
    }
}
