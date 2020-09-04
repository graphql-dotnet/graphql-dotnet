using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Resolvers;

namespace GraphQL.DI
{
    public class DIExecutionStrategy : ExecutionStrategy
    {
        protected IServiceProvider _serviceProvider;

        public DIExecutionStrategy(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode)
        {
            Func<Task, ExecutionNode, Task<ExecutionNode>> taskFunc = async (task, node) => { await task; return node; };
            //set up the service provider
            AsyncServiceProvider.Current = _serviceProvider;

            var nodes = new Stack<ExecutionNode>(); //synchronous nodes to be executed
            nodes.Push(rootNode);
            var asyncNodes = new Stack<ExecutionNode>(); //asynchronous nodes to be executed
            var waitingTasks = new List<Task<ExecutionNode>>(); //nodes currently executing
            var pendingNodes = new Queue<ExecutionNode>(); //IDelayLoadedResult nodes pending completion
            Task waitingSyncTask = null;
            int maxTasks = context.MaxParallelExecutionCount ?? int.MaxValue;
            if (maxTasks < 1) throw new InvalidOperationException("Invalid maximum number of tasks");

            void DICompleteNode(ExecutionNode node)
            {
                //if the result of the node is an IDelayLoadedResult, then add this
                //  node to a list of nodes to be loaded once everything else possible
                //  has been loaded
                if (node.Result is IDataLoaderResult)
                {
                    pendingNodes.Enqueue(node);
                }
                else
                {
                    // Push any child nodes on top of the stack
                    if (node is IParentExecutionNode parentNode)
                    {
                        // Add in reverse order so fields are executed in the correct order (for synchronous tasks)
                        foreach (var child in parentNode.GetChildNodes().Reverse())
                        {
                            //add node to async list or sync list, as appropriate
                            if (child.FieldDefinition is DIFieldType fieldType && fieldType.Concurrent)
                            {
                                asyncNodes.Push(child);
                            }
                            else
                            {
                                nodes.Push(child);
                            }
                        }
                    }
                }
            }

            // Process each node in the queue
            while (true)
            {
                //start executing all asynchronous nodes
                while (asyncNodes.Count > 0 && waitingTasks.Count < maxTasks)
                {
                    //grab an asynchronous node to execute
                    var node = asyncNodes.Pop();
                    //execute it (asynchronously)
                    var task = ExecuteNodeAsync(context, node);
                    if (task.IsCompleted)
                    {
                        await task;
                        DICompleteNode(node);
                    }
                    else
                    {
                        var taskWithNode = taskFunc(task, node);
                        //add this task to the list of tasks waiting to be completed
                        waitingTasks.Add(taskWithNode);
                    }
                }

                //start executing one synchronous task, if none is yet waiting to be completed
                while (nodes.Count > 0 && waitingSyncTask == null && waitingTasks.Count < maxTasks)
                {
                    //grab a synchronous node to execute
                    var node = nodes.Pop();
                    //execute it (asynchronously)
                    var task = ExecuteNodeAsync(context, node);
                    if (task.IsCompleted)
                    {
                        await task;
                        DICompleteNode(node);
                    }
                    else
                    {
                        var taskWithNode = taskFunc(task, node);
                        //notate the synchronous task that is currently executing
                        waitingSyncTask = taskWithNode;
                        //add this task to the list of tasks waiting to be completed
                        waitingTasks.Add(taskWithNode);
                    }
                }

                //complete one or more asynchronously-executing tasks
                if (waitingTasks.Count == 1)
                {
                    await OnBeforeExecutionStepAwaitedAsync(context)
                        .ConfigureAwait(false);

                    DICompleteNode(await waitingTasks[0]);
                    waitingTasks.Clear();
                    waitingSyncTask = null;
                } 
                else if (waitingTasks.Count > 0)
                {
                    //wait for listeners (this really makes no sense, and
                    //  is a really poor way of implementing an IDataLoader)
                    await OnBeforeExecutionStepAwaitedAsync(context)
                        .ConfigureAwait(false);

                    //wait for at least one task to complete
                    var completedTask = await Task.WhenAny(waitingTasks).ConfigureAwait(false);
                    //note: errors are not thrown here, but rather down at task.Result
                    waitingTasks.Remove(completedTask);
                    if (waitingSyncTask == completedTask) waitingSyncTask = null;

                    //if the request was canceled, quit out now
                    context.CancellationToken.ThrowIfCancellationRequested();

                    //process the completed task
                    DICompleteNode(await completedTask);
                }

                //if there's no sync/async nodes being processed or waiting to be processed,
                //  then load any IDelayLoadedResult values
                if (nodes.Count == 0 && asyncNodes.Count == 0 && waitingTasks.Count == 0)
                {
                    //must be synchronously, as all DelayLoaders will exist in the same scope
                    //however, once a single node is resolved, all the rest of the tasks from the same DelayLoader will already be completed
                    //also, must execute all these nodes at once, otherwise
                    //  a child node might execute and queue a child dataloader node,
                    //  which may try to execute before this 'level' of dataloaders have executed
                    while (pendingNodes.Count > 0)
                    {
                        var pendingNode = pendingNodes.Dequeue();
                        var task = CompleteDataLoaderNodeAsync(context, pendingNode);
                        if (!task.IsCompleted)
                        {
                            //wait for listeners (this really makes no sense, and
                            //  is a really poor way of implementing an IDataLoader)
                            await OnBeforeExecutionStepAwaitedAsync(context)
                                .ConfigureAwait(false);
                        }
                        await task.ConfigureAwait(false); //execute synchronously
                        DICompleteNode(pendingNode);
                    }
                    //if there were no pending nodes, there would be no waitingTasks, and that means there's no more nodes left to execute
                    if (waitingTasks.Count == 0) return;
                    //otherwise, the pending waitingTasks will all need to execute before the DelayLoaders attempt to execute again
                }
            }
        }
    }
}
