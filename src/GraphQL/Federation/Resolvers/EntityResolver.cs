using System.Collections;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation.Resolvers;

/// <summary>
/// Resolves the <c>_entities</c> field for GraphQL Federation.
/// </summary>
public sealed class EntityResolver : IFieldResolver
{
    /// <inheritdoc/>
    private EntityResolver()
    {
    }

    /// <summary>
    /// Returns the static instance of <see cref="EntityResolver"/>.
    /// </summary>
    public static EntityResolver Instance { get; } = new EntityResolver();

    /// <summary>
    /// Converts representations to a list of <see cref="Representation"/> objects.
    /// This should occur during field validation so that the representations can be validated.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Occurs when the requested type cannot be found, is not an object graph type, has not been
    /// configured for GraphQL Federation, or cannot be converted to the source type.
    /// </exception>
    /// <remarks>
    /// Exceptions thrown within this method are expected to be returned to the caller as a validation error
    /// (aka Input Error), not logged as a server error (aka Processing Error).
    /// </remarks>
    public IEnumerable<Representation> ConvertRepresentations(ISchema schema, IList representations)
    {
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));
        if (representations == null)
            throw new ArgumentNullException(nameof(representations));

        var ret = new List<Representation>();
        foreach (var representation in representations)
        {
            if (representation is IDictionary<string, object> rep)
            {
                var typeName = rep["__typename"].ToString();
                var graphTypeInstance = schema.AllTypes[typeName]
                    ?? throw new InvalidOperationException($"The type '{typeName}' could not be found.");
                if (graphTypeInstance is not IObjectGraphType objectGraphType)
                    throw new InvalidOperationException($"The type '{typeName}' is not an object graph type.");
                var resolver = graphTypeInstance.GetMetadata<IFederationResolver>(RESOLVER_METADATA)
                    ?? throw new InvalidOperationException($"The type '{typeName}' has not been configured for GraphQL Federation.");

                object value;
                try
                {
                    //can't use ObjectExtensions.ToObject because that requires an input object graph type for
                    //  deserialization mapping
                    //value = rep.ToObject(resolver.SourceType, null!);
                    if (resolver.SourceType == typeof(object) || resolver.SourceType == typeof(Dictionary<string, object>) || resolver.SourceType == typeof(IDictionary<string, object>))
                        value = rep;
                    else
                        value = ToObject(resolver.SourceType, objectGraphType, rep);
                }
                catch (Exception ex)
                {
                    // mask the underlying exception to prevent leaking implementation details
                    // the InnerException can be read for debugging purposes
                    throw new InvalidOperationException($"Error converting representation for type '{typeName}'.", ex);
                }

                ret.Add(new Representation(objectGraphType, resolver, value));
            }
            else
            {
                throw new InvalidOperationException("Representation must be a dictionary.");
            }
        }
        return ret;
    }

    /// <inheritdoc/>
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        //context.Copy is implicit due to the returned object being a list; otherwise it would be necessary,
        //  as the context is referenced within a delegate passed to the SimpleDataLoader (see below)
        //context = context.Copy();

        // require the representations argument to be converted to the proper type before hitting this code
        var representations = (IEnumerable<Representation>)context.Arguments![REPRESENTATIONS_ARGUMENT].Value!;

        var results = new List<object>();
        foreach (var representation in representations)
        {
            // using a data loader here causes the resolvers to run in serial or parallel based on the selected execution strategy.
            // unfortunately this requires extra allocations whereas if the strategy was known this code could be optimized by
            // either awaiting each resolver or collecting them and performing WaitAll. Note that this code counts on the fact
            // that the context instance will not be reused due to a list being returned from this method.
            var result = new SimpleDataLoader(_ => representation.Resolver.ResolveAsync(context, representation.Value).AsTask()!);

            results.Add(result);
        }

        return new(results);
    }

    /// <summary>
    /// Deserializes an object based on properties provided in a dictionary, using graph type information from
    /// an output graph type. Requires that the object type has a parameterless constructor.
    /// </summary>
    private static object ToObject(Type objectType, IObjectGraphType objectGraphType, IDictionary<string, object> map)
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
                if (value != null && !prop.PropertyType.IsInstanceOfType(value))
                    value = ValueConverter.ConvertTo(value, prop.PropertyType);
                prop.SetValue(obj, value);
            }
        }
        return obj;
    }

    private static object? Deserialize(string fieldName, IGraphType graphType, Type valueType, object? value)
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
            // cast/convert value to an array (it should already be an array)
            var array = (value as IEnumerable
                ?? throw new InvalidOperationException($"The field '{fieldName}' is a list graph type but the value is not a list"))
                .ToObjectArray();
            // get the list converter and element type for the list type
            var listConverter = ValueConverter.GetListConverter(valueType);
            var elementType = listConverter.ElementType;
            // deserialize each element in the array
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Deserialize(fieldName, listGraphType.ResolvedType!, elementType, array[i]);
            }
            // convert the array to the intended list type
            return listConverter.Convert(array);
        }

        if (graphType is ScalarGraphType scalarGraphType)
            return scalarGraphType.ParseValue(value);

        if (graphType is IObjectGraphType objectGraphType)
        {
            if (value is not Dictionary<string, object> dic)
                throw new InvalidOperationException($"The field '{fieldName}' is an object graph type but the value is not an object");

            return ToObject(valueType, objectGraphType, dic);
        }

        // below condition only used by FederationResolverAttribute.FederationStaticResolver.Context.ctor
        if (graphType is IInputObjectGraphType inputObjectGraphType)
        {
            if (value is not Dictionary<string, object?> dic)
                throw new InvalidOperationException($"The field '{fieldName}' is an object graph type but the value is not an object");

            return inputObjectGraphType.ParseDictionary(dic);
        }

        throw new InvalidOperationException($"The field '{fieldName}' is not a scalar or object graph type.");
    }

    private class SimpleDataLoader(Func<CancellationToken, Task<object?>> Resolver) : IDataLoaderResult
    {
        public Task<object?> GetResultAsync(CancellationToken cancellationToken = default) => Resolver(cancellationToken);
    }
}
