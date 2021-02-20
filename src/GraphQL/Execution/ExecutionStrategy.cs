using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Language.AST;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <summary>
    /// The base class for the included serial and parallel execution strategies.
    /// </summary>
    public abstract class ExecutionStrategy : IExecutionStrategy
    {
        /// <summary>
        /// Executes a GraphQL request and returns the result. The default implementation builds the root node
        /// and passes execution to <see cref="ExecuteNodeTreeAsync(ExecutionContext, ObjectExecutionNode)"/>.
        /// Once complete, the values are collected into an object that is ready to be serialized and returned
        /// within an <see cref="ExecutionResult"/>.
        /// </summary>
        public virtual async Task<ExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            var rootType = ExecutionHelper.GetOperationRootType(context.Document, context.Schema, context.Operation);
            var rootNode = BuildExecutionRootNode(context, rootType);

            await ExecuteNodeTreeAsync(context, rootNode).ConfigureAwait(false);

            // After the entire node tree has been executed, get the values
            object data = rootNode.PropagateNull() ? null : rootNode;

            return new ExecutionResult
            {
                Executed = true,
                Data = data,
                Query = context.Document.OriginalQuery,
                Document = context.Document,
                Operation = context.Operation,
                Extensions = context.Extensions
            };
        }

        /// <summary>
        /// Executes an execution node and all of its child nodes. This is typically only executed upon
        /// the root execution node.
        /// </summary>
        protected abstract Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode);

        /// <summary>
        /// Builds the root execution node.
        /// </summary>
        protected RootExecutionNode BuildExecutionRootNode(ExecutionContext context, IObjectGraphType rootType)
        {
            var root = new RootExecutionNode(rootType)
            {
                Result = context.RootValue
            };

            var fields = System.Threading.Interlocked.Exchange(ref context.ReusableFields, null) ?? new Fields();

            SetSubFieldNodes(context, root, fields.CollectFrom(context, rootType, context.Operation.SelectionSet));

            fields.Clear();
            System.Threading.Interlocked.CompareExchange(ref context.ReusableFields, fields, null);

            return root;
        }

        /// <summary>
        /// This method allows you to control the set of fields that the strategy will execute.
        /// <br/><br/>
        /// By default calls <see cref="ExecutionHelper.ShouldIncludeNode"/> to work as required
        /// by the specification. Override this method if you understand exactly what you are doing,
        /// because your actions may lead to the fact that the server's behavior ceases to comply
        /// with the specification requirements.
        /// </summary>
        public virtual bool ShouldIncludeNode(ExecutionContext context, IHaveDirectives directives) => ExecutionHelper.ShouldIncludeNode(context, directives.Directives);

        /// <summary>
        /// Creates execution nodes for child fields of an object execution node. Only run if
        /// the object execution node result is not <see langword="null"/>.
        /// </summary>
        private void SetSubFieldNodes(ExecutionContext context, ObjectExecutionNode parent)
        {
            var fields = System.Threading.Interlocked.Exchange(ref context.ReusableFields, null) ?? new Fields();

            SetSubFieldNodes(context, parent, fields.CollectFrom(context, parent.GetObjectGraphType(context.Schema), parent.Field?.SelectionSet));

            fields.Clear();
            System.Threading.Interlocked.CompareExchange(ref context.ReusableFields, fields, null);
        }

        /// <summary>
        /// Creates specified child execution nodes of an object execution node.
        /// </summary>
        private static void SetSubFieldNodes(ExecutionContext context, ObjectExecutionNode parent, Fields fields)
        {
            var parentType = parent.GetObjectGraphType(context.Schema);

            var subFields = new ExecutionNode[fields.Count];

            int i = 0;
            foreach (var kvp in fields)
            {
                var field = kvp.Value;

                var fieldDefinition = ExecutionHelper.GetFieldDefinition(context.Schema, parentType, field);

                if (fieldDefinition == null)
                    throw new InvalidOperationException($"Schema is not configured correctly to fetch field '{field.Name}' from type '{parentType.Name}'.");

                var node = BuildExecutionNode(parent, fieldDefinition.ResolvedType, field, fieldDefinition);

                subFields[i++] = node;
            }

            parent.SubFields = subFields;
        }

        /// <summary>
        /// Creates execution nodes for array elements of an array execution node. Only run if
        /// the array execution node result is not <see langword="null"/>.
        /// </summary>
        private void SetArrayItemNodes(ExecutionContext context, ArrayExecutionNode parent)
        {
            var listType = (ListGraphType)parent.GraphType;
            var itemType = listType.ResolvedType;

            if (itemType is NonNullGraphType nonNullGraphType)
                itemType = nonNullGraphType.ResolvedType;

            if (!(parent.Result is IEnumerable data))
            {
                throw new InvalidOperationException($"Expected an IEnumerable list though did not find one. Found: {parent.Result?.GetType().Name}");
            }

            int index = 0;
            var arrayItems = (data is ICollection collection)
                ? new List<ExecutionNode>(collection.Count)
                : new List<ExecutionNode>();

            if (data is IList list)
            {
                for (int i=0; i<list.Count; ++i)
                    SetArrayItemNode(list[i]);
            }
            else
            {
                foreach (object d in data)
                    SetArrayItemNode(d);
            }

            parent.Items = arrayItems;

            // local function uses 'struct closure' without heap allocation
            void SetArrayItemNode(object d)
            {
                if (d != null)
                {
                    var node = BuildExecutionNode(parent, itemType, parent.Field, parent.FieldDefinition, index);
                    node.Result = d;

                    if (!(d is IDataLoaderResult))
                    {
                        if (node is ObjectExecutionNode objectNode)
                        {
                            SetSubFieldNodes(context, objectNode);
                        }
                        else if (node is ArrayExecutionNode arrayNode)
                        {
                            SetArrayItemNodes(context, arrayNode);
                        }
                        else if (node is ValueExecutionNode valueNode)
                        {
                            node.Result = valueNode.GraphType.Serialize(d)
                                ?? throw new InvalidOperationException($"Unable to serialize '{d}' to '{valueNode.GraphType.Name}' for list index {index}.");
                        }
                    }

                    arrayItems.Add(node);
                }
                else
                {
                    if (listType.ResolvedType is NonNullGraphType)
                    {
                        throw new InvalidOperationException($"Cannot return a null member within a non-null list for list index {index}.");
                    }

                    var nullExecutionNode = new NullExecutionNode(parent, itemType, parent.Field, parent.FieldDefinition, index);
                    arrayItems.Add(nullExecutionNode);
                }

                index++;
            }
        }

        /// <summary>
        /// Builds an execution node with the specified parameters.
        /// </summary>
        protected static ExecutionNode BuildExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode = null)
        {
            if (graphType is NonNullGraphType nonNullFieldType)
                graphType = nonNullFieldType.ResolvedType;

            return graphType switch
            {
                ListGraphType _ => new ArrayExecutionNode(parent, graphType, field, fieldDefinition, indexInParentNode),
                IObjectGraphType _ => new ObjectExecutionNode(parent, graphType, field, fieldDefinition, indexInParentNode),
                IAbstractGraphType _ => new ObjectExecutionNode(parent, graphType, field, fieldDefinition, indexInParentNode),
                ScalarGraphType scalarGraphType => new ValueExecutionNode(parent, scalarGraphType, field, fieldDefinition, indexInParentNode),
                _ => throw new InvalidOperationException($"Unexpected type: {graphType}")
            };
        }

        /// <summary>
        /// Executes a single node. If the node does not return an <see cref="IDataLoaderResult"/>,
        /// it will pass execution to <see cref="CompleteNode(ExecutionContext, ExecutionNode)"/>.
        /// </summary>
        protected virtual async Task ExecuteNodeAsync(ExecutionContext context, ExecutionNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            // these are the only conditions upon which a node has already been executed when this method is called
            if (node is RootExecutionNode || node.Parent is ArrayExecutionNode)
                return;

            try
            {
                ReadonlyResolveFieldContext resolveContext = System.Threading.Interlocked.Exchange(ref context.ReusableReadonlyResolveFieldContext, null);
                resolveContext = resolveContext != null ? resolveContext.Reset(node, context) : new ReadonlyResolveFieldContext(node, context);

                var resolver = node.FieldDefinition.Resolver ?? NameFieldResolver.Instance;
                var result = resolver.Resolve(resolveContext);

                if (result is Task task)
                {
                    await task.ConfigureAwait(false);
                    result = task.GetResult();
                }

                node.Result = result;

                if (!(result is IDataLoaderResult))
                {
                    CompleteNode(context, node);
                    // for non-dataloader nodes that completed without throwing an error, we can re-use the context
                    resolveContext.Reset(null, null);
                    System.Threading.Interlocked.CompareExchange(ref context.ReusableReadonlyResolveFieldContext, resolveContext, null);
                }
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ExecutionError error)
            {
                SetNodeError(context, node, error);
            }
            catch (Exception ex)
            {
                if (ProcessNodeUnhandledException(context, node, ex))
                    throw;
            }
        }

        /// <summary>
        /// Completes a pending data loader node. If the node does not return an <see cref="IDataLoaderResult"/>,
        /// it will pass execution to <see cref="CompleteNode(ExecutionContext, ExecutionNode)"/>.
        /// </summary>
        protected virtual async Task CompleteDataLoaderNodeAsync(ExecutionContext context, ExecutionNode node)
        {
            if (!(node.Result is IDataLoaderResult dataLoaderResult))
                throw new InvalidOperationException("This execution node is not pending completion");

            try
            {
                node.Result = await dataLoaderResult.GetResultAsync(context.CancellationToken).ConfigureAwait(false);

                if (!(node.Result is IDataLoaderResult))
                {
                    CompleteNode(context, node);
                }
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ExecutionError error)
            {
                SetNodeError(context, node, error);
            }
            catch (Exception ex)
            {
                if (ProcessNodeUnhandledException(context, node, ex))
                    throw;
            }
        }

        /// <summary>
        /// Validates a node result. Builds child nodes via <see cref="SetSubFieldNodes(ExecutionContext, ObjectExecutionNode)">SetSubFieldNodes</see>
        /// and <see cref="SetArrayItemNodes(ExecutionContext, ArrayExecutionNode)">SetArrayItemNodes</see>, but does not execute them. For value
        /// execution nodes, it will run <see cref="ScalarGraphType.Serialize(object)"/> to serialize the result.
        /// </summary>
        protected virtual void CompleteNode(ExecutionContext context, ExecutionNode node)
        {
            try
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
                    else if (node is ValueExecutionNode valueNode)
                    {
                        node.Result = valueNode.GraphType.Serialize(node.Result)
                            ?? throw new InvalidOperationException($"Unable to serialize '{node.Result}' to '{valueNode.GraphType.Name}'.");
                    }
                }
            }
            catch (ExecutionError error)
            {
                SetNodeError(context, node, error);
            }
            catch (Exception ex)
            {
                if (ProcessNodeUnhandledException(context, node, ex))
                    throw;
            }
        }

        /// <summary>
        /// Sets the location and path information to the error and adds it to the document. Sets the node result to <see langword="null"/>.
        /// </summary>
        protected void SetNodeError(ExecutionContext context, ExecutionNode node, ExecutionError error)
        {
            error.AddLocation(node.Field, context.Document);
            error.Path = node.ResponsePath;
            context.Errors.Add(error);

            node.Result = null;
        }

        /// <summary>
        /// Processes unhandled field resolver exceptions.
        /// </summary>
        /// <returns>A value that indicates when the exception should be rethrown.</returns>
        protected bool ProcessNodeUnhandledException(ExecutionContext context, ExecutionNode node, Exception ex)
        {
            if (context.ThrowOnUnhandledException)
                return true;

            UnhandledExceptionContext exceptionContext = null;
            if (context.UnhandledExceptionDelegate != null)
            {
                // be sure not to re-use this instance of `IResolveFieldContext`
                var resolveContext = new ReadonlyResolveFieldContext(node, context);
                exceptionContext = new UnhandledExceptionContext(context, resolveContext, ex);
                context.UnhandledExceptionDelegate(exceptionContext);
                ex = exceptionContext.Exception;
            }

            var error = ex is ExecutionError executionError ? executionError : new UnhandledError(exceptionContext?.ErrorMessage ?? $"Error trying to resolve field '{node.Name}'.", ex);

            SetNodeError(context, node, error);

            return false;
        }

        /// <summary>
        /// Validates the <see cref="ExecutionNode.Result"/> to ensure that it is valid for the node.
        /// Errors typically occur when a null value is returned for a non-null graph type. Also validates the
        /// object type when <see cref="IObjectGraphType.IsTypeOf"/> is assigned, or when the graph type
        /// is an <see cref="IAbstractGraphType"/>.
        /// </summary>
        protected virtual void ValidateNodeResult(ExecutionContext context, ExecutionNode node)
        {
            var result = node.Result;

            IGraphType fieldType = node.ResolvedType;
            var objectType = fieldType as IObjectGraphType;

            if (fieldType is NonNullGraphType nonNullType)
            {
                if (result == null)
                {
                    throw new InvalidOperationException("Cannot return null for non-null type."
                        + $" Field: {node.Name}, Type: {nonNullType}.");
                }

                objectType = nonNullType.ResolvedType as IObjectGraphType;
            }

            if (result == null)
            {
                return;
            }

            if (fieldType is IAbstractGraphType abstractType)
            {
                objectType = abstractType.GetObjectType(result, context.Schema);

                if (objectType == null)
                {
                    throw new InvalidOperationException(
                        $"Abstract type {abstractType.Name} must resolve to an Object type at " +
                        $"runtime for field {node.Parent.GraphType.Name}.{node.Name} " +
                        $"with value '{result}', received 'null'.");
                }

                if (!abstractType.IsPossibleType(objectType))
                {
                    throw new InvalidOperationException($"Runtime Object type \"{objectType}\" is not a possible type for \"{abstractType}\".");
                }
            }

            if (objectType?.IsTypeOf != null && !objectType.IsTypeOf(result))
            {
                throw new InvalidOperationException($"\"{result}\" value of type \"{result.GetType()}\" is not allowed for \"{objectType.Name}\". Either change IsTypeOf method of \"{objectType.Name}\" to accept this value or return another value from your resolver.");
            }
        }

        /// <summary>
        /// If there are any <see cref="IDocumentExecutionListener"/>s specified within the <see cref="ExecutionContext"/>,
        /// runs the <see cref="IDocumentExecutionListener.BeforeExecutionStepAwaitedAsync(IExecutionContext)">BeforeExecutionStepAwaitedAsync</see>
        /// method on each of the registered document execution listeners.
        /// </summary>
        [Obsolete]
        protected virtual async Task OnBeforeExecutionStepAwaitedAsync(ExecutionContext context)
        {
            if (context.Listeners != null)
            {
                foreach (var listener in context.Listeners)
                {
                    await listener.BeforeExecutionStepAwaitedAsync(context)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
