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
                    currentTasks.Add(ExecuteNodeAsync(context, pendingNode));
                }

                await OnBeforeExecutionStepAwaitedAsync(context)
                    .ConfigureAwait(false);

                // Await tasks for this execution step
                var completedNodes = await Task.WhenAll(currentTasks)
                    .ConfigureAwait(false);
                currentTasks.Clear();

                // Add child nodes to pending nodes to execute the next level in parallel
                foreach (var childNode in completedNodes
                    .OfType<IParentExecutionNode>()
                    .SelectMany(x => x.GetChildNodes()))
                {
                    pendingNodes.Enqueue(childNode);
                }
            }
        }
    }
}
