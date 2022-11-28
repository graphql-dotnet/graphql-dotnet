using GraphQL.DataLoader;

namespace GraphQL.Execution
{
    /// <inheritdoc cref="ExecuteNodeTreeAsync(ExecutionContext, ExecutionNode)"/>
    public class ParallelExecutionStrategy : ExecutionStrategy
    {
        // frequently reused objects
        private Queue<ExecutionNode>? _reusablePendingNodes;
        private Queue<ExecutionNode>? _reusablePendingDataLoaders;
        private List<Task>? _reusableCurrentTasks;
        private List<ExecutionNode>? _reusableCurrentNodes;

        /// <summary>
        /// Gets a static instance of <see cref="ParallelExecutionStrategy"/> strategy.
        /// </summary>
        public static ParallelExecutionStrategy Instance { get; } = new ParallelExecutionStrategy();

        /// <summary>
        /// Executes document nodes in parallel. Field resolvers must be designed for multi-threaded use.
        /// Nodes that return a <see cref="IDataLoaderResult"/> will execute once all other pending nodes
        /// have been completed.
        /// </summary>
        public override async Task ExecuteNodeTreeAsync(ExecutionContext context, ExecutionNode rootNode)
        {
            var pendingNodes = System.Threading.Interlocked.Exchange(ref _reusablePendingNodes, null) ?? new Queue<ExecutionNode>();
            pendingNodes.Enqueue(rootNode);
            var pendingDataLoaders = System.Threading.Interlocked.Exchange(ref _reusablePendingDataLoaders, null) ?? new Queue<ExecutionNode>();

            var currentTasks = System.Threading.Interlocked.Exchange(ref _reusableCurrentTasks, null) ?? new List<Task>();
            var currentNodes = System.Threading.Interlocked.Exchange(ref _reusableCurrentNodes, null) ?? new List<ExecutionNode>();

            try
            {
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
                                await pendingNodeTask.ConfigureAwait(false);

                                // Node completed synchronously, so no need to add it to the list of currently executing nodes
                                // instead add any child nodes to the pendingNodes queue directly here
                                if (pendingNode.Result is IDataLoaderResult)
                                {
                                    pendingDataLoaders.Enqueue(pendingNode);
                                }
                                else if (pendingNode is IParentExecutionNode parentExecutionNode)
                                {
                                    parentExecutionNode.ApplyToChildren((node, state) => state.Enqueue(node), pendingNodes);
                                }
                            }
                            else
                            {
                                // Node is actually asynchronous, so add it to the list of current tasks being executed in parallel
                                currentTasks.Add(pendingNodeTask);
                                currentNodes.Add(pendingNode);
                            }

                        }

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
                                p.ApplyToChildren((node, state) => state.Enqueue(node), pendingNodes);
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
            catch (Exception original)
            {
                if (currentTasks.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(currentTasks).ConfigureAwait(false);
                    }
                    catch (Exception awaited)
                    {
                        if (original.Data?.IsReadOnly == false)
                            original.Data["GRAPHQL_ALL_TASKS_AWAITED_EXCEPTION"] = awaited;
                    }
                }
                throw;
            }
            finally
            {
                pendingNodes.Clear();
                pendingDataLoaders.Clear();
                currentTasks.Clear();
                currentNodes.Clear();

                System.Threading.Interlocked.CompareExchange(ref _reusablePendingNodes, pendingNodes, null);
                System.Threading.Interlocked.CompareExchange(ref _reusablePendingDataLoaders, pendingDataLoaders, null);
                System.Threading.Interlocked.CompareExchange(ref _reusableCurrentTasks, currentTasks, null);
                System.Threading.Interlocked.CompareExchange(ref _reusableCurrentNodes, currentNodes, null);
            }
        }
    }
}
