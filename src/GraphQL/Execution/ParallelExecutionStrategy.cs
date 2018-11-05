using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Execution
{
    public class ParallelExecutionStrategy : ExecutionStrategy
    {
        protected override async Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
        {
            // Nodes that are ready to be executed
            var pendingNodes = new List<ExecutionNode>
            {
                rootNode
            };

            // Currently executing nodes
            var currentTasks = new List<Task<ExecutionNode>>();

            // Nodes that have completed after each step
            var completedNodes = new List<ExecutionNode>();

            var executionStepTasks = new List<Task>();

            while (pendingNodes.Count > 0 || currentTasks.Count > 0)
            {
                // Start executing all pending nodes
                for (int i = 0; i < pendingNodes.Count; i++)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    var task = ExecuteNodeAsync(context, pendingNodes[i]);
                    currentTasks.Add(task);
                }

                pendingNodes.Clear();

                // This is used to dispatch any pending DataLoaders
                // But we don't need to wait for it to complete
                var executionStepTask = OnBeforeExecutionStepAwaitedAsync(context);
                executionStepTasks.Add(executionStepTask);

                // Wait for one or more of the current tasks to finish
                await Task.WhenAny(currentTasks)
                    .ConfigureAwait(false);

                // Remove each completed Task from the list and await them
                for (int i = 0; i < currentTasks.Count; i++)
                {
                    if (currentTasks[i].IsCompleted)
                    {
                        var task = currentTasks[i];

                        // Remove the completed task and adjust index
                        // so we don't skip the next item in the list
                        currentTasks.RemoveAt(i--);

                        var node = await task.ConfigureAwait(false);
                        completedNodes.Add(node);
                    }
                }

                // If the execution task(s) are complete, await them
                for (int i = 0; i < executionStepTasks.Count; i++)
                {
                    if (executionStepTasks[i].IsCompleted)
                    {
                        var task = executionStepTasks[i];
                        executionStepTasks.RemoveAt(i--);

                        await task.ConfigureAwait(false);
                    }
                }

                // Add child nodes to pending nodes to execute the next level in parallel
                var childNodes = completedNodes
                    .OfType<IParentExecutionNode>()
                    .SelectMany(x => x.GetChildNodes());

                pendingNodes.AddRange(childNodes);
                completedNodes.Clear();
            }

            // This shouldn't be necessary, but just in case
            await Task.WhenAll(executionStepTasks)
                .ConfigureAwait(false);
        }
    }
}
