using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Execution
{
    public class SerialExecutionStrategy : ExecutionStrategy
    {
        protected override async Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
        {
            // Use a stack to track all nodes in the tree that need to be executed
            var nodes = new Stack<ExecutionNode>();
            nodes.Push(rootNode);

            // Process each node on the stack one by one
            while (nodes.Count > 0)
            {
                var node = nodes.Pop();
                var task = ExecuteNodeAsync(context, node);

                await OnBeforeExecutionStepAwaitedAsync(context)
                    .ConfigureAwait(false);

                await task.ConfigureAwait(false);

                // Push any child nodes on top of the stack
                if (node is IParentExecutionNode parentNode)
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
