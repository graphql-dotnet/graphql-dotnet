using System;
using System.Collections;
using System.Collections.Generic;
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

                subFields[kvp.Key] = node;
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
                var error = new ExecutionError("User error: expected an IEnumerable list though did not find one.");
                throw error;
            }

            var index = 0;
            var arrayItems = (data is ICollection collection)
                ? new List<ExecutionNode>(collection.Count)
                : new List<ExecutionNode>();

            foreach (var d in data)
            {
                var path = AppendPath(parent.Path, (index++).ToString());

                if (d != null)
                {
                    var node = BuildExecutionNode(parent, itemType, parent.Field, parent.FieldDefinition, path);
                    node.Result = d;

                    if (node is ObjectExecutionNode objectNode)
                    {
                        SetSubFieldNodes(context, objectNode);
                    }
                    else if (node is ArrayExecutionNode arrayNode)
                    {
                        SetArrayItemNodes(context, arrayNode);
                    }

                    arrayItems.Add(node);
                }
                else
                {
                    if (listType.ResolvedType is NonNullGraphType)
                    {
                        var error = new ExecutionError(
                            "Cannot return null for non-null type."
                            + $" Field: {parent.Name}, Type: {parent.FieldDefinition.ResolvedType}.");

                        error.AddLocation(parent.Field, context.Document);
                        error.Path = path;
                        context.Errors.Add(error);
                        return;
                    }

                    var valueExecutionNode = new ValueExecutionNode(parent, itemType, parent.Field, parent.FieldDefinition, path)
                    {
                        Result = null
                    };
                    arrayItems.Add(valueExecutionNode);
                }
            }

            parent.Items = arrayItems;
        }

        public static ExecutionNode BuildExecutionNode(ExecutionNode parent, IGraphType graphType, Field field, FieldType fieldDefinition, string[] path = null)
        {
            path = path ?? AppendPath(parent.Path, field.Name);

            if (graphType is NonNullGraphType nonNullFieldType)
                graphType = nonNullFieldType.ResolvedType;

            switch (graphType)
            {
                case ListGraphType listGraphType:
                    return new ArrayExecutionNode(parent, graphType, field, fieldDefinition, path);

                case IObjectGraphType objectGraphType:
                    return new ObjectExecutionNode(parent, graphType, field, fieldDefinition, path);

                case IAbstractGraphType abstractType:
                    return new ObjectExecutionNode(parent, graphType, field, fieldDefinition, path);

                case ScalarGraphType scalarType:
                    return new ValueExecutionNode(parent, graphType, field, fieldDefinition, path);

                default:
                    throw new InvalidOperationException($"Unexpected type: {graphType}");
            }
        }

        /// <summary>
        /// Execute a single node
        /// </summary>
        /// <remarks>
        /// Builds child nodes, but does not execute them
        /// </remarks>
        protected virtual async Task<ExecutionNode> ExecuteNodeAsync(ExecutionContext context, ExecutionNode node)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (node.IsResultSet)
                return node;

            try
            {
                var arguments = GetArgumentValues(context.Schema, node.FieldDefinition.Arguments, node.Field.Arguments, context.Variables);
                var subFields = SubFieldsFor(context, node.FieldDefinition.ResolvedType, node.Field);

                var resolveContext = new ResolveFieldContext
                {
                    FieldName = node.Field.Name,
                    FieldAst = node.Field,
                    FieldDefinition = node.FieldDefinition,
                    ReturnType = node.FieldDefinition.ResolvedType,
                    ParentType = node.GetParentType(context.Schema),
                    Arguments = arguments,
                    Source = node.Source,
                    Schema = context.Schema,
                    Document = context.Document,
                    Fragments = context.Fragments,
                    RootValue = context.RootValue,
                    UserContext = context.UserContext,
                    Operation = context.Operation,
                    Variables = context.Variables,
                    CancellationToken = context.CancellationToken,
                    Metrics = context.Metrics,
                    Errors = context.Errors,
                    Path = node.Path,
                    SubFields = subFields
                };

                var resolver = node.FieldDefinition.Resolver ?? new NameFieldResolver();
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

                var error = new ExecutionError($"Error trying to resolve {node.Name}.", ex);
                error.AddLocation(node.Field, context.Document);
                error.Path = node.Path;
                context.Errors.Add(error);

                node.Result = null;
            }

            return node;
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
                    throw new ExecutionError("Cannot return null for non-null type."
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
                    var error = new ExecutionError(
                        $"Abstract type {abstractType.Name} must resolve to an Object type at " +
                        $"runtime for field {node.Parent.GraphType.Name}.{node.Name} " +
                        $"with value '{result}', received 'null'.");
                    throw error;
                }

                if (!abstractType.IsPossibleType(objectType))
                {
                    var error = new ExecutionError($"Runtime Object type \"{objectType}\" is not a possible type for \"{abstractType}\"");
                    throw error;
                }
            }

            if (objectType?.IsTypeOf != null && !objectType.IsTypeOf(result))
            {
                var error = new ExecutionError($"Expected value of type \"{objectType}\" for \"{objectType.Name}\" but got: {result}.");
                throw error;
            }
        }

        protected virtual async Task OnBeforeExecutionStepAwaitedAsync(ExecutionContext context)
        {
            foreach (var listener in context.Listeners)
            {
                await listener.BeforeExecutionStepAwaitedAsync(context.UserContext, context.CancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
