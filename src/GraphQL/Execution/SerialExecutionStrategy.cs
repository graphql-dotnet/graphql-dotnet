using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.DataLoader;

namespace GraphQL.Execution
{
    /// <inheritdoc cref="ExecuteNodeTreeAsync(ExecutionContext, ObjectExecutionNode)"/>
    public class SerialExecutionStrategy : ExecutionStrategy
    {
        /// <summary>
        /// Gets a static instance of <see cref="SerialExecutionStrategy"/> strategy.
        /// </summary>
        public static SerialExecutionStrategy Instance { get; } = new SerialExecutionStrategy();

        /// <summary>
        /// Executes document nodes serially. Nodes that return a <see cref="IDataLoaderResult"/> will
        /// execute once all other pending nodes have been completed.
        /// </summary>
        protected override async Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
        {
            // Use a stack to track all nodes in the tree that need to be executed
            var nodes = new Stack<ExecutionNode>();
            nodes.Push(rootNode);
            var dataLoaderNodes = new Queue<ExecutionNode>();

            // Process each node on the stack one by one
            while (nodes.Count > 0 || dataLoaderNodes.Count > 0)
            {
                while (nodes.Count > 0)
                {
                    var node = nodes.Pop();
                    var task = ExecuteNodeAsync(context, node);

#pragma warning disable CS0612 // Type or member is obsolete
                    await OnBeforeExecutionStepAwaitedAsync(context)
#pragma warning restore CS0612 // Type or member is obsolete
                        .ConfigureAwait(false);

                    await task.ConfigureAwait(false);

                    // Push any child nodes on top of the stack
                    if (node.Result is IDataLoaderResult)
                    {
                        dataLoaderNodes.Enqueue(node);
                    }
                    else if (node is IParentExecutionNode parentNode)
                    {
                        // Add in reverse order so fields are executed in the correct order
                        parentNode.ApplyToChildren((node, state) => state.Push(node), nodes, reverse: true);
                    }
                }

                while (dataLoaderNodes.Count > 0)
                {
                    var node = dataLoaderNodes.Dequeue();
                    var task = CompleteDataLoaderNodeAsync(context, node);

#pragma warning disable CS0612 // Type or member is obsolete
                    await OnBeforeExecutionStepAwaitedAsync(context)
#pragma warning restore CS0612 // Type or member is obsolete
                        .ConfigureAwait(false);

                    await task.ConfigureAwait(false);

                    // Push any child nodes on top of the stack
                    if (node.Result is IDataLoaderResult)
                    {
                        dataLoaderNodes.Enqueue(node);
                    }
                    else if (node is IParentExecutionNode parentNode)
                    {
                        // Add in reverse order so fields are executed in the correct order
                        parentNode.ApplyToChildren((node, state) => state.Push(node), nodes, reverse: true);
                    }
                }
            }
        }
    }
}
