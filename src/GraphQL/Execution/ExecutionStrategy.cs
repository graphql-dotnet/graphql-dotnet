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

        public static Dictionary<string, ExecutionNodeDefinition> GetDefinitionsForSubFields(ExecutionContext context, ObjectExecutionNode parent)
        {
            var fields = CollectFields(context, parent.GetObjectGraphType(context.Schema), parent.Field?.SelectionSet);
            var parentType = parent.GetObjectGraphType(context.Schema);
            return GetDefinitionsForSubFields(context, parentType, fields);
        }

        public static void SetSubFieldNodes(ExecutionContext context, ObjectExecutionNode parent, Dictionary<string, Field> fields)
        {
            var parentType = parent.GetObjectGraphType(context.Schema);
            var fieldsDefinition = GetDefinitionsForSubFields(context, parentType, fields);
            SetSubFieldNodes(parent, fieldsDefinition);
        }

        public static void SetSubFieldNodes(ObjectExecutionNode parent, Dictionary<string, ExecutionNodeDefinition> fieldsDefinition)
        {
            parent.SubFields = fieldsDefinition.ToDictionary(kvp => kvp.Key, kvp => BuildExecutionNode(kvp.Value, parent));
        }

        private static Dictionary<string, ExecutionNodeDefinition> GetDefinitionsForSubFields(ExecutionContext context, IObjectGraphType parentType, Dictionary<string, Field> fields)
        {
            var subFields = new Dictionary<string, ExecutionNodeDefinition>(fields.Count);
            foreach (var kvp in fields)
            {
                var name = kvp.Key;
                var field = kvp.Value;

                if (!ShouldIncludeNode(context, field.Directives))
                    continue;

                var fieldDefinition = GetFieldDefinition(context.Document, context.Schema, parentType, field);

                if (fieldDefinition == null)
                    continue;

                subFields[kvp.Key] = BuildExecutionNodeDefinition(fieldDefinition.ResolvedType, field, fieldDefinition);
            }
            return subFields;
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

            parent.Items = GetChildNodes(context, parent, itemType, data).ToList();
        }

        private static IEnumerable<ExecutionNode> GetChildNodes(ExecutionContext context, ArrayExecutionNode parent, IGraphType itemType, IEnumerable data)
        {
            var rootDefinition = BuildExecutionNodeDefinition(itemType, parent.Field, parent.FieldDefinition);
            var subFieldsDefinitionsCache = new Dictionary<string, Dictionary<string, ExecutionNodeDefinition>>();

            var index = 0;
            foreach (var d in data)
            {
                var path = AppendPath(parent.Path, (index++).ToString());
                if (d == null)
                {
                    yield return new ValueExecutionNode(rootDefinition, parent, path)
                    {
                        Result = null
                    };
                    continue;
                }

                var node = BuildExecutionNode(rootDefinition, parent, path);
                node.Result = d;
                SetSubfieldsIfNeeded(context, node, subFieldsDefinitionsCache);
                yield return node;
            }
        }

        private static void SetSubfieldsIfNeeded(ExecutionContext context, ExecutionNode parent, Dictionary<string, Dictionary<string, ExecutionNodeDefinition>> subFieldsDefinitionsCache)
        {
            if (parent is ObjectExecutionNode objectNode)
            {
                var objectType = objectNode.GetObjectGraphType(context.Schema);
                if (!subFieldsDefinitionsCache.TryGetValue(objectType.Name, out var definitions))
                {
                    definitions = GetDefinitionsForSubFields(context, objectNode);
                    subFieldsDefinitionsCache.Add(objectType.Name, definitions);
                }
                SetSubFieldNodes(objectNode, definitions);
            }
        }

        public static ExecutionNode BuildExecutionNode(ExecutionNodeDefinition definition, ExecutionNode parent, string[] path = null)
        {
            path = path ?? AppendPath(parent.Path, definition.Field.Name);
            switch (definition.GraphType)
            {
                case ListGraphType listGraphType:
                    return new ArrayExecutionNode(definition, parent, path);

                case IObjectGraphType objectGraphType:
                case IAbstractGraphType abstractType:
                    return new ObjectExecutionNode(definition, parent, path);

                case ScalarGraphType scalarType:
                    return new ValueExecutionNode(definition, parent, path);

                default:
                    throw new InvalidOperationException($"Unexpected type: {definition.GraphType}");
            }
        }

        public static ExecutionNodeDefinition BuildExecutionNodeDefinition(IGraphType graphType, Field field, FieldType fieldDefinition)
        {
            if (graphType is NonNullGraphType nonNullFieldType)
            {
                graphType = nonNullFieldType.ResolvedType;
            }

            return new ExecutionNodeDefinition(graphType, field, fieldDefinition);
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
                var resolver = node.FieldDefinition.Resolver ?? new NameFieldResolver();
                var result = resolver.Resolve(context, node);

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
                objectType = nonNullType?.ResolvedType as IObjectGraphType;

                if (result == null)
                {
                    var type = nonNullType.ResolvedType;

                    var error = new ExecutionError($"Cannot return null for non-null type. Field: {node.Name}, Type: {type.Name}!.");
                    throw error;
                }
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
