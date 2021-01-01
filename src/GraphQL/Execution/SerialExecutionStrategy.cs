using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.DataLoader;

namespace GraphQL.Execution
{
    /// <inheritdoc cref="SerialExecutionStrategy.ExecuteNodeTreeAsync(ExecutionContext, ObjectExecutionNode)"/>
    public class SerialExecutionStrategy : ExecutionStrategy
    {
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

                    await OnBeforeExecutionStepAwaitedAsync(context)
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
                        foreach (var child in parentNode.GetChildNodes().Reverse())
                        {
                            nodes.Push(child);
                        }
                    }
                }

                while (dataLoaderNodes.Count > 0)
                {
                    var node = dataLoaderNodes.Dequeue();
                    var task = CompleteDataLoaderNodeAsync(context, node);

                    await OnBeforeExecutionStepAwaitedAsync(context)
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
                        foreach (var child in parentNode.GetChildNodes().Reverse())
                        {
                            nodes.Push(child);
                        }
                    }
                }
            }
        }
    }
}
