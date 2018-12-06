using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.DataLoader
{
    // Implementation from: https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-2-asyncautoresetevent/
    public class AsyncAutoResetEvent
    {
        private static readonly Task Completed = Task.FromResult(true);

        private readonly Queue<TaskCompletionSource<bool>> _waits = new Queue<TaskCompletionSource<bool>>();
        private bool _signaled;

        public Task WaitAsync()
        {
            lock (_waits)
            {
                if (_signaled)
                {
                    _signaled = false;
                    return Completed;
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    _waits.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (_waits)
            {
                if (_waits.Count > 0)
                {
                    toRelease = _waits.Dequeue();
                }
                else if (!_signaled)
                {
                    _signaled = true;
                }
            }

            toRelease?.SetResult(true);
        }
    }
}
