using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        internal static async Task<IEnumerable<T>> WhereAsync<T>(this IEnumerable<T> items, Func<T, Task<bool>> predicate)
        {
            if (items == null)
            {
                return Enumerable.Empty<T>();
            }

            var itemTaskList = items.Select(item => new { Item = item, PredTask = predicate.Invoke(item) }).ToList();
            await Task.WhenAll(itemTaskList.Select(x => x.PredTask));
            return itemTaskList.Where(x => x.PredTask.Result).Select(x => x.Item);
        }
    }
}
