using System.Collections;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using static GraphQL.Federation.Extensions.FederationHelper;

namespace GraphQL.Federation;

/// <summary>
/// Resolves the _entity field for GraphQL Federation.
/// </summary>
public class EntityResolver : IFieldResolver
{
    /// <summary>
    /// Returns the static instance of <see cref="EntityResolver"/>.
    /// </summary>
    public static EntityResolver Instance { get; } = new EntityResolver();

    /// <inheritdoc/>
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        //context.Copy is implicit due to the returned object being a list; otherwise it would be necessary (see below)
        //context = context.Copy();

        // BUG: Authorization only works at a field level. Authorization at GraphType level doesn't work.
        //      i.e. _entities(representations: [{ __typename: "MyType", id: 1 }]) { myField }
        //      will only check authorization for "myField" but not for "MyType".
        var repMaps = context.GetArgument<List<Dictionary<string, object>>>("representations");
        var results = new List<object>();
        foreach (var repMap in repMaps)
        {
            var typeName = repMap["__typename"].ToString();
            var graphTypeInstance = context.Schema.AllTypes[typeName]
                ?? throw new InvalidOperationException($"The type '{typeName}' could not be found.");
            if (graphTypeInstance is not IObjectGraphType objectGraphType)
                throw new InvalidOperationException($"The type '{typeName}' is not an object graph type.");
            var resolver = graphTypeInstance.GetMetadata<IFederationResolver>(RESOLVER_METADATA)
                ?? throw new NotImplementedException($"ResolveReference() was not provided for {graphTypeInstance.Name}.");

            //var rep = repMap!.ToObject(resolver.SourceType, null!);
            var rep = ToObject(resolver.SourceType, objectGraphType, repMap);
            // usng a data loader here causes the resolvers to run in serial or parallel based on the selected execution strategy.
            // unfortunately this requires extra allocations whereas if the strategy was known this code could be optimized by
            // either awaiting each resolver or collecting them and performing WaitAll. note that this code counts on the fact
            // that the context instance will not be reused due to a list being returned from this method.
            var result = new SimpleDataLoader(_ => resolver.ResolveAsync(context, rep).AsTask()!);
            results.Add(result);
        }
        return new(results);

        object ToObject(Type objectType, IObjectGraphType objectGraphType, Dictionary<string, object> map)
        {
            var obj = Activator.CreateInstance(objectType)!;
            foreach (var item in map)
            {
                if (item.Key != "__typename")
                {
                    var field = objectGraphType.Fields.Find(item.Key)
                        ?? throw new InvalidOperationException($"Field '{item.Key}' not found in graph type '{objectGraphType.Name}'.");
                    var graphType = field.ResolvedType!;
                    var prop = objectType.GetProperty(item.Key, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public)
                        ?? throw new InvalidOperationException($"Property '{item.Key}' not found in type '{objectType.GetFriendlyName()}'.");
                    var value = Deserialize(item.Key, graphType, prop.PropertyType, item.Value);
                    prop.SetValue(obj, item.Value);
                }
            }
            return obj;
        }

        object? Deserialize(string fieldName, IGraphType graphType, Type valueType, object value)
        {
            if (graphType is NonNullGraphType nonNullGraphType)
            {
                if (value == null)
                    throw new InvalidOperationException($"The non-null field '{fieldName}' has a null value.");
                graphType = nonNullGraphType.ResolvedType!;
            }

            if (value == null)
                return null;

            if (graphType is ListGraphType listGraphType)
            {
                if (value is not IList list)
                    throw new InvalidOperationException($"The field '{fieldName}' is a list graph type but the value is not a list");
                var ret = new List<object?>();
                var (isArray, isList, elementType) = valueType.GetListType();
                foreach (var listValue in list)
                {
                    ret.Add(Deserialize(fieldName, listGraphType.ResolvedType!, elementType, listValue));
                }
                if (isArray)
                    return ret.ToArray();
                return ret;
            }

            if (graphType is ScalarGraphType scalarGraphType)
            {
                return scalarGraphType.ParseValue(value);
            }

            if (graphType is IObjectGraphType objectGraphType)
            {
                if (value is not Dictionary<string, object> dic)
                    throw new InvalidOperationException($"The field '{fieldName}' is an object graph type but the value is not an object");

                return ToObject(valueType, objectGraphType, dic);
            }

            throw new InvalidOperationException($"The field '{fieldName}' is not a scalar or object graph type.");
        }
    }

    private record SimpleDataLoader(Func<CancellationToken, Task<object?>> Resolver) : IDataLoaderResult
    {
        public Task<object?> GetResultAsync(CancellationToken cancellationToken = default) => Resolver(cancellationToken);
    }
}
