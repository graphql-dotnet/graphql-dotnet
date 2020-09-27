using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.Logging
{
    /// <summary>
    /// Awaitable auto reset event - credit https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-2-asyncautoresetevent/
    /// </summary>
    public class AsyncAutoResetEvent
    {
        private static readonly Task _completed = Task.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> _waits = new Queue<TaskCompletionSource<bool>>();
        private bool _signaled;

        /// <summary>
        /// Waits until the event has been triggered
        /// </summary>
        /// <returns></returns>
        public Task WaitAsync()
        {
            lock (_waits)
            {
                if (_signaled)
                {
                    _signaled = false;
                    return _completed;
                }

                var tcs = new TaskCompletionSource<bool>();
                _waits.Enqueue(tcs);
                return tcs.Task;
            }
        }

        /// <summary>
        /// Sets the trigger to allow waiting threads to continue
        /// </summary>
        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;

            lock (_waits)
            {
                if (_waits.Count > 0)
                    toRelease = _waits.Dequeue();
                else if (!_signaled)
                    _signaled = true;
            }

            toRelease?.SetResult(true);
        }
    }
}
