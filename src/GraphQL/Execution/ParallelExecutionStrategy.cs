using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.DataLoader;

namespace GraphQL.Execution
{
    /// <inheritdoc cref="ParallelExecutionStrategy.ExecuteNodeTreeAsync(ExecutionContext, ObjectExecutionNode)"/>
    public class ParallelExecutionStrategy : ExecutionStrategy
    {
        /// <summary>
        /// Executes document nodes in parallel. Field resolvers must be designed for multi-threaded use.
        /// Nodes that return a <see cref="IDataLoaderResult"/> will execute once all other pending nodes
        /// have been completed.
        /// </summary>
        protected override Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
            => ExecuteNodeTreeAsync(context, rootNode);

        /// <inheritdoc cref="ExecuteNodeTreeAsync(ExecutionContext, ObjectExecutionNode)"/>
        protected async Task ExecuteNodeTreeAsync(ExecutionContext context, ExecutionNode rootNode)
        {
            var pendingNodes = new Queue<ExecutionNode>();
            pendingNodes.Enqueue(rootNode);
            var pendingDataLoaders = new Queue<ExecutionNode>();

            var currentTasks = new List<Task>();
            var currentNodes = new List<ExecutionNode>();
            while (pendingNodes.Count > 0 || pendingDataLoaders.Count > 0 || currentTasks.Count > 0)
            {
                while (pendingNodes.Count > 0 || currentTasks.Count > 0)
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
                            // Throw any caught exceptions
                            await pendingNodeTask;

                            // Node completed synchronously, so no need to add it to the list of currently executing nodes
                            // instead add any child nodes to the pendingNodes queue directly here
                            if (pendingNode.Result is IDataLoaderResult)
                            {
                                pendingDataLoaders.Enqueue(pendingNode);
                            }
                            else if (pendingNode is IParentExecutionNode parentExecutionNode)
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
                            currentNodes.Add(pendingNode);
                        }

                    }

                    await OnBeforeExecutionStepAwaitedAsync(context)
                        .ConfigureAwait(false);

                    // Await tasks for this execution step
                    await Task.WhenAll(currentTasks)
                        .ConfigureAwait(false);

                    // Add child nodes to pending nodes to execute the next level in parallel
                    foreach (var node in currentNodes)
                    {
                        if (node.Result is IDataLoaderResult)
                        {
                            pendingDataLoaders.Enqueue(node);
                        }
                        else if (node is IParentExecutionNode p)
                        {
                            foreach (var childNode in p.GetChildNodes())
                                pendingNodes.Enqueue(childNode);
                        }
                    }

                    currentTasks.Clear();
                    currentNodes.Clear();
                }

                //run pending data loaders
                while (pendingDataLoaders.Count > 0)
                {
                    var dataLoaderNode = pendingDataLoaders.Dequeue();
                    currentTasks.Add(CompleteDataLoaderNodeAsync(context, dataLoaderNode));
                    currentNodes.Add(dataLoaderNode);
                }
            }
        }
    }
}
