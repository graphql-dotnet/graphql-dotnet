using GraphQL.DI.DelayLoader;
using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static GraphQL.Execution.ExecutionHelper;

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
                    var taskWithNode = taskFunc(task, node);
                    //add this task to the list of tasks waiting to be completed
                    waitingTasks.Add(taskWithNode);
                }

                //start executing one synchronous task, if none is yet waiting to be completed
                if (nodes.Count > 0 && waitingSyncTask == null && waitingTasks.Count < maxTasks)
                {
                    //grab a synchronous node to execute
                    var node = nodes.Pop();
                    //execute it (asynchronously)
                    var task = ExecuteNodeAsync(context, node);
                    var taskWithNode = taskFunc(task, node);
                    //notate the synchronous task that is currently executing
                    waitingSyncTask = taskWithNode;
                    //add this task to the list of tasks waiting to be completed
                    waitingTasks.Add(taskWithNode);
                }

                //complete one or more tasks
                if (waitingTasks.Count > 0)
                {
                    //check if there are any listeners
                    IEnumerable<Task<ExecutionNode>> completedTasks;
                    if (context.Listeners.Count() == 0)
                    {
                        //wait for at least one task to complete
                        var completedTask = await Task.WhenAny(waitingTasks).ConfigureAwait(false);
                        //note: errors are not thrown here, but rather down at task.Result
                        completedTasks = new Task<ExecutionNode>[] { completedTask };
                        waitingTasks.Remove(completedTask);
                        if (waitingSyncTask == completedTask) waitingSyncTask = null;
                    }
                    else
                    {
                        //wait for listeners (this really makes no sense, and
                        //  is a really poor way of implementing an IDataLoader)
                        await OnBeforeExecutionStepAwaitedAsync(context)
                            .ConfigureAwait(false);

                        //execute all pending tasks
                        await Task.WhenAll(waitingTasks).ConfigureAwait(false);

                        completedTasks = waitingTasks;
                        waitingTasks = new List<Task<ExecutionNode>>();
                        waitingSyncTask = null;
                    }

                    //if the request was canceled, quit out now
                    context.CancellationToken.ThrowIfCancellationRequested();

                    //check each completed task
                    foreach (var task in completedTasks)
                    {
                        var node = task.Result;
                        //if the result of the node is an IDelayLoadedResult, then add this
                        //  node to a list of nodes to be loaded once everything else possible
                        //  has been loaded
                        if (node.Result is IDelayLoadedResult)
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
                        var task = CompleteNodeAsync(context, pendingNode);
                        await task.ConfigureAwait(false); //execute synchronously
                        waitingTasks.Add(task);
                    }
                    //if there were no pending nodes, there would be no waitingTasks, and that means there's no more nodes left to execute
                    if (waitingTasks.Count == 0) return;
                    //otherwise, the pending waitingTasks will all need to execute before the DelayLoaders attempt to execute again
                }
            }
        }


        /// <summary>
        /// Execute a single node
        /// </summary>
        /// <remarks>
        /// Except for IDelayLoadedResult, builds child nodes, but does not execute them
        /// </remarks>
        protected override async Task ExecuteNodeAsync(ExecutionContext context, ExecutionNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (node.IsResultSet) return;

            ReadonlyResolveFieldContext resolveContext = null;

            try
            {
                resolveContext = new ReadonlyResolveFieldContext(node, context);

                var resolver = node.FieldDefinition.Resolver ?? NameFieldResolver.Instance;
                var result = resolver.Resolve(resolveContext);

                if (result is Task task)
                {
                    await task.ConfigureAwait(false);
                    result = task.GetResult();
                }

                node.Result = result;

                if (!(result is IDelayLoadedResult))
                {
                    ValidateNodeResult(context, node);

                    // Build child nodes
                    if (node.Result != null)
                    {
                        if (node is ObjectExecutionNode objectNode)
                        {
                            SetSubFieldNodes(context, objectNode);
                        }
                        else if (node is ArrayExecutionNode arrayNode)
                        {
                            SetArrayItemNodes(context, arrayNode);
                        }
                    }
                }
            }
            catch (ExecutionError error)
            {
                error.AddLocation(node.Field, context.Document);
                error.Path = node.Path;
                context.Errors.Add(error);

                node.Result = null;
            }
            catch (Exception ex)
            {
                if (context.ThrowOnUnhandledException)
                    throw;

                if (context.UnhandledExceptionDelegate != null)
                {
                    var exceptionContext = new UnhandledExceptionContext(context, resolveContext, ex);
                    context.UnhandledExceptionDelegate(exceptionContext);
                    ex = exceptionContext.Exception;
                }

                var error = new ExecutionError($"Error trying to resolve {node.Name}.", ex);
                error.AddLocation(node.Field, context.Document);
                error.Path = node.Path;
                context.Errors.Add(error);

                node.Result = null;
            }
        }

        protected virtual async Task<ExecutionNode> CompleteNodeAsync(ExecutionContext context, ExecutionNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (!node.IsResultSet) throw new InvalidOperationException("This execution node has not yet been executed");
            if (!(node.Result is DelayLoader.IDelayLoadedResult delayLoadedResult)) throw new InvalidOperationException("This execution node is not pending completion");

            ResolveFieldContext resolveContext = null;

            try
            {

                node.Result = await delayLoadedResult.GetResultAsync();

                ValidateNodeResult(context, node);

                // Build child nodes
                if (node.Result != null)
                {
                    if (node is ObjectExecutionNode objectNode)
                    {
                        SetSubFieldNodes(context, objectNode);
                    }
                    else if (node is ArrayExecutionNode arrayNode)
                    {
                        SetArrayItemNodes(context, arrayNode);
                    }
                }
            }
            catch (ExecutionError error)
            {
                error.AddLocation(node.Field, context.Document);
                error.Path = node.Path;
                context.Errors.Add(error);

                node.Result = null;
            }
            catch (Exception ex)
            {
                if (context.ThrowOnUnhandledException)
                    throw;

                if (context.UnhandledExceptionDelegate != null)
                {
                    var exceptionContext = new UnhandledExceptionContext(context, resolveContext, ex);
                    context.UnhandledExceptionDelegate(exceptionContext);
                    ex = exceptionContext.Exception;
                }

                var error = new ExecutionError($"Error trying to resolve {node.Name}.", ex);
                error.AddLocation(node.Field, context.Document);
                error.Path = node.Path;
                context.Errors.Add(error);

                node.Result = null;
            }

            return node;
        }

    }
}
