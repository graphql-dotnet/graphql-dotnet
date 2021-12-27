using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;

namespace GraphQL
{
    /// <summary>
    /// Task extensions.
    /// </summary>
    internal static class TaskExtensions
    {
        /// <summary>
        /// Gets the result of a completed <see cref="Task&lt;TResult&gt;"/> when TResult is not known
        /// </summary>
        /// <remarks>
        /// The Task should already be awaited or this call will block.
        /// This will also throw an exception if the task is not Task&lt;TResult&gt;.
        /// </remarks>
        /// <param name="task">A task that has already been awaited</param>
        internal static object? GetResult(this Task task)
        {
            if (task is Task<object> to)
            {
                // Most performant if available
                return to.Result;
            }
            else
            {
                // Using dynamic is over 10x faster than reflection but works only for public types (or with InternalsVisibleTo attribute)
                try
                {
                    return ((dynamic)task).Result;
                }
                catch (RuntimeBinderException)
                {
                    // it won't be any worse
                    return task.GetType().GetProperty("Result")!.GetValue(task, null);
                }
            }
        }
    }
}
