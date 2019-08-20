using GraphQL.DataLoader;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Execution
{
    public class ParallelExecutionStrategy : ExecutionStrategy
    {
        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
        protected override Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
            => ExecuteNodeTreeAsync(context, rootNode);

        private Task DataLoaderDispatchNeeded(ExecutionContext context)
        {
            var dataLoaderDocumentListeners = context.Listeners.OfType<DataLoaderDocumentListener>();
            if (dataLoaderDocumentListeners.Any())
            {
                // Return a task that completes when any dataloader in the DataLoaderDocumentListener is in need of dispatch
                return (dataLoaderDocumentListeners.Count() == 1) ? dataLoaderDocumentListeners.First().DispatchNeeded :
                    Task.WhenAny(dataLoaderDocumentListeners.Select(l => l.DispatchNeeded));
            }
            // No DataLoaderDocumentListener registered, so just return a task that never completes
            // since this means no dataloaders will ever be awaited and in need of dispatch.
            return _tcs.Task;
        }

        protected async Task ExecuteNodeTreeAsync(ExecutionContext context, ExecutionNode rootNode)
        {
            var pendingNodes = new List<ExecutionNode>
            {
                rootNode
            };

            while (pendingNodes.Count > 0)
            {
                var currentTasks = new Task<ExecutionNode>[pendingNodes.Count];

                // Start executing all pending nodes
                for (int i = 0; i < pendingNodes.Count; i++)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    currentTasks[i] = ExecuteNodeAsync(context, pendingNodes[i]);
                }

                pendingNodes.Clear();
                
                // Create a task that completes when all currentTasks are complete
                var commonTask = Task.WhenAll(currentTasks);
                
                // Await tasks for this execution step, use loop to keep dispatching dataloaders
                // in case resolvers uses multiple dataloaders, or the dataloader is not the first async method awaited.
                while (!commonTask.IsCompleted)
                {
                    // Dispatches any waiting dataloaders
                    await OnBeforeExecutionStepAwaitedAsync(context)
                        .ConfigureAwait(false);

                    // Wait for either all currentTask to complete, or any dataloader(s) in need of dispatching
                    // which will continue the loop and dispatch the waiting dataloader(s)
                    await Task.WhenAny(commonTask, DataLoaderDispatchNeeded(context))
                        .ConfigureAwait(false);
                }
                var completedNodes = await commonTask
                    .ConfigureAwait(false);

                // Add child nodes to pending nodes to execute the next level in parallel
                var childNodes = completedNodes
                    .OfType<IParentExecutionNode>()
                    .SelectMany(x => x.GetChildNodes());

                pendingNodes.AddRange(childNodes);
            }
        }
    }
}
