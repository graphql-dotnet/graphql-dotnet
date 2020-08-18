using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Resolvers;
using GraphQL.Types;
using static GraphQL.Execution.ExecutionHelper;

namespace GraphQL.Execution
{
    public abstract class ExecutionStrategy : IExecutionStrategy
    {
        public virtual async Task<ExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            var rootType = GetOperationRootType(context.Document, context.Schema, context.Operation);
            var rootNode = BuildExecutionRootNode(context, rootType);

            await ExecuteNodeTreeAsync(context, rootNode)
                .ConfigureAwait(false);

            // After the entire node tree has been executed, get the values
            var data = rootNode.ToValue();

            return new ExecutionResult
            {
                Data = data
            }.With(context);
        }

        protected abstract Task ExecuteNodeTreeAsync(ExecutionContext context, ObjectExecutionNode rootNode);

        public static RootExecutionNode BuildExecutionRootNode(ExecutionContext context, IObjectGraphType rootType)
        {
            var root = new RootExecutionNode(rootType)
            {
                Result = context.RootValue
            };

            var fields = CollectFields(
                context,
                rootType,
                context.Operation.SelectionSet);


            SetSubFieldNodes(context, root, fields);

            return root;
        }

        public static void SetSubFieldNodes(ExecutionContext context, ObjectExecutionNode parent)
        {
            var fields = CollectFields(context, parent.GetObjectGraphType(context.Schema), parent.Field?.SelectionSet);
            SetSubFieldNodes(context, parent, fields);
        }

        public static void SetSubFieldNodes(ExecutionContext context, ObjectExecutionNode parent, Dictionary<string, Field> fields)
        {
            var parentType = parent.GetObjectGraphType(context.Schema);

            var subFields = new Dictionary<string, ExecutionNode>(fields.Count);

            foreach (var kvp in fields)
            {
                var name = kvp.Key;
                var field = kvp.Value;

                if (!ShouldIncludeNode(context, field.Directives))
                    continue;

                var fieldDefinition = GetFieldDefinition(context.Document, context.Schema, parentType, field);

                if (fieldDefinition == null)
                    continue;

                var node = BuildExecutionNode(parent, fieldDefinition.ResolvedType, field, fieldDefinition);

                if (node == null)
                    continue;

                subFields[name] = node;
            }

            parent.SubFields = subFields;
        }

        public static void SetArrayItemNodes(ExecutionContext context, ArrayExecutionNode parent)
        {
            var listType = (ListGraphType)parent.GraphType;
            var itemType = listType.ResolvedType;

            if (itemType is NonNullGraphType nonNullGraphType)
                itemType = nonNullGraphType.ResolvedType;

            if (!(parent.Result is IEnumerable data))
            {
                throw new InvalidOperationException($"Expected an IEnumerable list though did not find one. Found: {parent.Result?.GetType().Name}");
            }

            var index = 0;
            var arrayItems = (data is ICollection collection)
                ? new List<ExecutionNode>(collection.Count)
                : new List<ExecutionNode>();

            foreach (var d in data)
            {
                if (d != null)
                {
                    var node = BuildExecutionNode(parent, itemType, parent.Field, parent.FieldDefinition, index);
                    node.Result = d;

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

            parent.Items = arrayItems;
        }

        public static ExecutionNode BuildExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, int? indexInParentNode = null)
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
        /// Execute a single node
        /// </summary>
        /// <remarks>
        /// Builds child nodes, but does not execute them
        /// </remarks>
        protected virtual async Task ExecuteNodeAsync(ExecutionContext context, ExecutionNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (node.IsResultSet)
                return;

            IResolveFieldContext resolveContext = null;

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
                            ?? throw new InvalidOperationException($"Unable to serialize '{node.Result}' to '{valueNode.GraphType.Name}'");
                    }
                }
            }
            catch (ExecutionError error)
            {
                error.AddLocation(node.Field, context.Document);
                error.Path = node.ResponsePath;
                context.Errors.Add(error);

                node.Result = null;
            }
            catch (Exception ex)
            {
                if (context.ThrowOnUnhandledException)
                    throw;

                UnhandledExceptionContext exceptionContext = null;
                if (context.UnhandledExceptionDelegate != null)
                {
                    exceptionContext = new UnhandledExceptionContext(context, resolveContext, ex);
                    context.UnhandledExceptionDelegate(exceptionContext);
                    ex = exceptionContext.Exception;
                }

                var error = ex is ExecutionError executionError ? executionError : new ExecutionError(exceptionContext?.ErrorMessage ?? $"Error trying to resolve field '{node.Name}'.", ex);
                error.AddLocation(node.Field, context.Document);
                error.Path = node.ResponsePath;
                context.Errors.Add(error);

                node.Result = null;
            }
        }

        protected virtual void ValidateNodeResult(ExecutionContext context, ExecutionNode node)
        {
            var result = node.Result;

            IGraphType fieldType = node.FieldDefinition.ResolvedType;
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

        protected virtual async Task OnBeforeExecutionStepAwaitedAsync(ExecutionContext context)
        {
            if (context.Listeners != null)
                foreach (var listener in context.Listeners)
                {
                    await listener.BeforeExecutionStepAwaitedAsync(context)
                        .ConfigureAwait(false);
                }
        }
    }
}
