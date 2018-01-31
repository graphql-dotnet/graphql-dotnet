using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    public class ParallelExecutionStrategy : ExecutionStrategy
    {
        protected override async Task<IDictionary<string, object>> ExecuteFieldsAsync(ExecutionContext context, IObjectGraphType rootType, object source, Dictionary<string, Field> fields, IEnumerable<string> path)
        {
            var data = new ConcurrentDictionary<string, object>();

            var tasks = new List<Task>();

            foreach (var fieldCollection in fields)
            {
                var currentPath = path.Concat(new[] { fieldCollection.Key });

                var field = fieldCollection.Value;
                var fieldType = GetFieldDefinition(context.Document, context.Schema, rootType, field);

                // Process fields in parallel
                var task = ExtractFieldAsync(context, rootType, source, field, fieldType, data, currentPath);
                tasks.Add(task);
            }

            foreach (var listener in context.Listeners)
            {
                await listener.BeforeResolveLevelAwaitedAsync(context.UserContext, context.CancellationToken)
                    .ConfigureAwait(false);
            }

            await Task.WhenAll(tasks.ToArray())
                    .ConfigureAwait(false);

            return OrderData(data, fields);
        }

        protected static IDictionary<string, object> OrderData(IDictionary<string, object> data, IDictionary<string, Field> fields)
        {
            var ordered = new Dictionary<string, object>();

            foreach (var fieldCollection in fields)
            {
                var name = fieldCollection.Key;

                if (!data.ContainsKey(name))
                {
                    continue;
                }

                ordered.Add(name, data[name]);
            }

            return ordered;
        }

        //protected async Task ExtractFieldAsync(ExecutionContext context, IObjectGraphType rootType, object source,
        //    Field field, FieldType fieldType, IDictionary<string, object> data, IEnumerable<string> path)
        //{
        //    context.CancellationToken.ThrowIfCancellationRequested();

        //    var name = field.Alias ?? field.Name;

        //    if (data.ContainsKey(name))
        //    {
        //        return;
        //    }

        //    if (!ShouldIncludeNode(context, field.Directives))
        //    {
        //        return;
        //    }

        //    var result = await ResolveFieldAsync(context, rootType, source, field, path)
        //        .ConfigureAwait(false);

        //    if (result.Skip)
        //    {
        //        return;
        //    }

        //    data[name] = result.Value;
        //}

        ////protected bool CanResolveFromData(Field field, FieldType type)
        ////{
        ////    if (field == null || type == null)
        ////    {
        ////        return false;
        ////    }

        ////    if (type.Arguments != null &&
        ////        type.Arguments.Any())
        ////    {
        ////        return false;
        ////    }

        ////    if (!(type.ResolvedType is ScalarGraphType))
        ////    {
        ////        return false;
        ////    }

        ////    if (type.ResolvedType is NonNullGraphType)
        ////    {
        ////        return false;
        ////    }

        ////    return true;
        ////}

        /////// <summary>
        /////// Resolve simple fields in a performant manner
        /////// </summary>
        ////private static async Task<object> ResolveFieldFromDataAsync(ExecutionContext context, IObjectGraphType rootType, object source,
        ////    FieldType fieldType, Field field, IEnumerable<string> path)
        ////{
        ////    object result = null;

        ////    try
        ////    {
        ////        if (fieldType.Resolver != null)
        ////        {
        ////            var rfc = new ResolveFieldContext(context, field, fieldType, source, rootType, null, path);

        ////            result = fieldType.Resolver.Resolve(rfc);

        ////            result = await UnwrapResultAsync(result)
        ////                .ConfigureAwait(false);
        ////        }
        ////        else
        ////        {
        ////            result = NameFieldResolver.Resolve(source, field.Name);
        ////        }

        ////        if (result != null)
        ////        {
        ////            var scalarType = fieldType.ResolvedType as ScalarGraphType;

        ////            result = scalarType?.Serialize(result);
        ////        }
        ////    }
        ////    catch (Exception exc)
        ////    {
        ////        var error = new ExecutionError($"Error trying to resolve {field.Name}.", exc);
        ////        error.AddLocation(field, context.Document);
        ////        error.Path = path.ToList();
        ////        context.Errors.Add(error);

        ////        // If there was an exception, the value of result cannot be trusted
        ////        result = null;
        ////    }

        ////    return result;
        ////}

        //public async Task<ResolveFieldResult<object>> ResolveFieldAsync(ExecutionContext context, IObjectGraphType parentType, object source, Field field, IEnumerable<string> path)
        //{
        //    context.CancellationToken.ThrowIfCancellationRequested();

        //    var fieldPath = path?.ToList() ?? new List<string>();

        //    var resolveResult = new ResolveFieldResult<object>
        //    {
        //        Skip = false
        //    };

        //    var fieldDefinition = GetFieldDefinition(context.Document, context.Schema, parentType, field);
        //    if (fieldDefinition == null)
        //    {
        //        resolveResult.Skip = true;
        //        return resolveResult;
        //    }

        //    var arguments = GetArgumentValues(context.Schema, fieldDefinition.Arguments, field.Arguments, context.Variables);

        //    try
        //    {
        //        var resolveContext = new ResolveFieldContext
        //        {
        //            FieldName = field.Name,
        //            FieldAst = field,
        //            FieldDefinition = fieldDefinition,
        //            ReturnType = fieldDefinition.ResolvedType,
        //            ParentType = parentType,
        //            Arguments = arguments,
        //            Source = source,
        //            Schema = context.Schema,
        //            Document = context.Document,
        //            Fragments = context.Fragments,
        //            RootValue = context.RootValue,
        //            UserContext = context.UserContext,
        //            Operation = context.Operation,
        //            Variables = context.Variables,
        //            CancellationToken = context.CancellationToken,
        //            Metrics = context.Metrics,
        //            Errors = context.Errors,
        //            Path = fieldPath
        //        };

        //        resolveContext.SubFields = SubFieldsFor(context, fieldDefinition.ResolvedType, field);

        //        var resolver = fieldDefinition.Resolver ?? new NameFieldResolver();
        //        var result = resolver.Resolve(resolveContext);

        //        result = await UnwrapResultAsync(result)
        //            .ConfigureAwait(false);

        //        resolveResult.Value = await CompleteValueAsync(context, parentType, fieldDefinition.ResolvedType, field, result, fieldPath)
        //            .ConfigureAwait(false);

        //        return resolveResult;
        //    }
        //    catch (Exception exc)
        //    {
        //        return GenerateError(resolveResult, field, context, exc, path);
        //    }
        //}

        //protected static ResolveFieldResult<object> GenerateError(
        //    ResolveFieldResult<object> resolveResult,
        //    Field field,
        //    ExecutionContext context,
        //    Exception exc,
        //    IEnumerable<string> path)
        //{
        //    var error = new ExecutionError("Error trying to resolve {0}.".ToFormat(field.Name), exc);
        //    error.AddLocation(field, context.Document);
        //    error.Path = path;
        //    context.Errors.Add(error);
        //    resolveResult.Skip = false;
        //    return resolveResult;
        //}

        //public async Task<object> CompleteValueAsync(ExecutionContext context, IObjectGraphType parentType, IGraphType fieldType, Field field, object result, IEnumerable<string> path)
        //{
        //    var fieldName = field?.Name;

        //    if (fieldType is NonNullGraphType nonNullType)
        //    {
        //        var type = nonNullType.ResolvedType;
        //        var completed = await CompleteValueAsync(context, parentType, type, field, result, path).ConfigureAwait(false);
        //        if (completed == null)
        //        {
        //            var error = new ExecutionError("Cannot return null for non-null type. Field: {0}, Type: {1}!."
        //                .ToFormat(fieldName, type.Name));
        //            error.AddLocation(field, context.Document);
        //            throw error;
        //        }

        //        return completed;
        //    }

        //    if (result == null)
        //    {
        //        return null;
        //    }

        //    if (fieldType is ScalarGraphType)
        //    {
        //        var scalarType = fieldType as ScalarGraphType;
        //        var coercedValue = scalarType.Serialize(result);
        //        return coercedValue;
        //    }

        //    if (fieldType is ListGraphType)
        //    {
        //        return await ResolveListFromData(context, result, parentType, fieldType, field, path)
        //            .ConfigureAwait(false);
        //    }

        //    var objectType = fieldType as IObjectGraphType;

        //    if (fieldType is IAbstractGraphType)
        //    {
        //        var abstractType = fieldType as IAbstractGraphType;
        //        objectType = abstractType.GetObjectType(result);

        //        if (objectType == null)
        //        {
        //            var error = new ExecutionError(
        //                $"Abstract type {abstractType.Name} must resolve to an Object type at " +
        //                $"runtime for field {parentType.Name}.{fieldName} " +
        //                $"with value {result}, received 'null'.");
        //            error.AddLocation(field, context.Document);
        //            throw error;
        //        }

        //        if (!abstractType.IsPossibleType(objectType))
        //        {
        //            var error = new ExecutionError(
        //                "Runtime Object type \"{0}\" is not a possible type for \"{1}\""
        //                .ToFormat(objectType, abstractType));
        //            error.AddLocation(field, context.Document);
        //            throw error;
        //        }
        //    }

        //    if (objectType == null)
        //    {
        //        return null;
        //    }

        //    if (objectType.IsTypeOf != null && !objectType.IsTypeOf(result))
        //    {
        //        var error = new ExecutionError(
        //            "Expected value of type \"{0}\" but got: {1}."
        //            .ToFormat(objectType, result));
        //        error.AddLocation(field, context.Document);
        //        throw error;
        //    }

        //    var subFields = new Dictionary<string, Field>();
        //    var visitedFragments = new List<string>();

        //    subFields = CollectFields(context, objectType, field?.SelectionSet, subFields, visitedFragments);

        //    return await ExecuteFieldsAsync(context, objectType, result, subFields, path).ConfigureAwait(false);
        //}

        ///// <summary>
        ///// Resolve lists in a performant manner
        ///// </summary>
        ///// <param name="context"></param>
        ///// <param name="source"></param>
        ///// <param name="parentType"></param>
        ///// <param name="graphType"></param>
        ///// <param name="field"></param>
        ///// <param name="path"></param>
        ///// <returns></returns>
        //protected async Task<IList<object>> ResolveListFromData(ExecutionContext context, object source, IObjectGraphType parentType,
        //    IGraphType graphType, Field field, IEnumerable<string> path)
        //{
        //    var listInfo = graphType as ListGraphType;
        //    var subType = listInfo?.ResolvedType as IObjectGraphType;
        //    var data = source as IEnumerable;
        //    var visitedFragments = new List<string>();
        //    var subFields = CollectFields(context, subType, field.SelectionSet, null, visitedFragments);

        //    if (data == null)
        //    {
        //        var error = new ExecutionError("User error: expected an IEnumerable list though did not find one.");
        //        error.AddLocation(field, context.Document);
        //        throw error;
        //    }

        //    var index = 0;
        //    var tasks = new List<Task<object>>();

        //    foreach (var node in data)
        //    {
        //        var currentPath = path.Concat(new[] { $"{index++}" });

        //        if (subType != null)
        //        {
        //            var nodeTask = ExecuteFieldsAsync(context, subType, node, subFields, currentPath)
        //                .ContinueWith(task => (object)task.Result);

        //            tasks.Add(nodeTask);
        //        }
        //        else
        //        {
        //            var nodeTask = CompleteValueAsync(context, parentType, listInfo?.ResolvedType, field, node, currentPath);

        //            tasks.Add(nodeTask);
        //        }
        //    }

        //    return await Task.WhenAll(tasks)
        //        .ConfigureAwait(false);
        //}
    }
}
