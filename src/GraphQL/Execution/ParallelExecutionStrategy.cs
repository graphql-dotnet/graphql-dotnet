using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Execution
{
    public class ParallelExecutionStrategy : ExecutionStrategy
    {
        protected override Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
            => ExecuteNodeTreeAsync(context, rootNode);

        protected async Task ExecuteNodeTreeAsync(ExecutionContext context, ExecutionNode rootNode)
        {
            var pendingNodes = new Queue<ExecutionNode>();
            pendingNodes.Enqueue(rootNode);

            var currentTasks = new List<Task<ExecutionNode>>();
            while (pendingNodes.Count > 0)
            {
                // Start executing pending nodes, while limiting the maximum number of parallel executed nodes to the set limit
                while ((context.MaxParallelExecutionCount == null || currentTasks.Count < context.MaxParallelExecutionCount)
                    && pendingNodes.Count > 0)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    var pendingNode = pendingNodes.Dequeue();
                    var pendingNodeTask = ExecuteNodeAsync(context, pendingNode);
                    if (pendingNodeTask.IsCompleted)
                    {
                        // Node completed synchronously, so no need to add it to the list of currently executing nodes
                        // instead add any child nodes to the pendingNodes queue directly here
                        var result = await pendingNodeTask;
                        if (result is IParentExecutionNode parentExecutionNode)
                        {
                            foreach (var childNode in parentExecutionNode.GetChildNodes())
                            {
                                pendingNodes.Enqueue(childNode);
                            }
                        }
                    }
                    else
                    {
                        // Node is actually asynchronous, so add it to the list of current tasks being executed in parallel
                        currentTasks.Add(pendingNodeTask);
                    }

                }

                await OnBeforeExecutionStepAwaitedAsync(context)
                    .ConfigureAwait(false);

                // Await tasks for this execution step
                var completedNodes = await Task.WhenAll(currentTasks)
                    .ConfigureAwait(false);
                currentTasks.Clear();

                // Add child nodes to pending nodes to execute the next level in parallel
                foreach (var node in completedNodes)
                    if (node is IParentExecutionNode p)
                {
                    foreach (var childNode in p.GetChildNodes())
                        pendingNodes.Enqueue(childNode);
                }
            }
        }
    }
}
