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
            // Nodes that are ready to be executed
            var pendingNodes = new List<ExecutionNode>
            {
                rootNode
            };

            // Currently executing nodes
            var currentTasks = new LinkedList<Task<ExecutionNode>>();

            // Nodes that have completed after each step
            var completedNodes = new List<ExecutionNode>();

            var executionStepTasks = new LinkedList<Task>();

            while (pendingNodes.Count > 0 || currentTasks.Count > 0)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                // Start executing all pending nodes
                for (int i = 0; i < pendingNodes.Count; i++)
                {
                    var task = ExecuteNodeAsync(context, pendingNodes[i]);
                    currentTasks.AddLast(task);
                }

                pendingNodes.Clear();

                // This is used to dispatch any pending DataLoaders
                // But we don't need to wait for it to complete
                var executionStepTask = OnBeforeExecutionStepAwaitedAsync(context);
                executionStepTasks.AddLast(executionStepTask);

                // Wait for one or more of the current tasks to finish
                await Task.WhenAny(currentTasks);

                // Remove each completed Task from the list and await them
                for (var currentNode = currentTasks.First; currentNode != null;)
                {
                    var task = currentNode.Value;
                    var nextNode = currentNode.Next;

                    if (task.IsCompleted)
                    {
                        currentTasks.Remove(currentNode);

                        var node = await task;
                        completedNodes.Add(node);
                    }

                    currentNode = nextNode;
                }

                // If the execution task(s) are complete, await them
                for (var currentNode = executionStepTasks.First; currentNode != null;)
                {
                    var task = currentNode.Value;
                    var nextNode = currentNode.Next;

                    if (task.IsCompleted)
                    {
                        executionStepTasks.Remove(currentNode);

                        await task;
                    }

                    currentNode = nextNode;
                }

                // Add child nodes to pending nodes to execute the next level in parallel
                var childNodes = completedNodes
                    .OfType<IParentExecutionNode>()
                    .SelectMany(x => x.GetChildNodes());

                pendingNodes.AddRange(childNodes);
                completedNodes.Clear();
            }

            // This shouldn't be necessary, but just in case
            await Task.WhenAll(executionStepTasks);
        }
    }
}
