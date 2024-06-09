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

        // enumerate the requested representations, ensuring that the resulting list
        // returns the representations in the same order as the input list (spec requirement)
        var ret = new List<Representation>();
        foreach (var representation in representations)
        {
            // the representation should always be a dictionary, although the _Any scalar type
            // does not enforce that it is not a scalar or list
            if (representation is not IDictionary<string, object?> rep)
                throw new InvalidOperationException("Representation must be a dictionary.");

            // pull the __typename field from the representation, which will indicate the type
            if (!rep.TryGetValue("__typename", out var typeNameObj) || typeNameObj is not string typeName)
                throw new InvalidOperationException("Representation must contain a __typename field.");

            // now find the graph type instance for the type name, ensuring it is an object type and has an entity resolver
            var graphTypeInstance = schema.AllTypes[typeName]
                ?? throw new InvalidOperationException($"The type '{typeName}' could not be found.");
            if (graphTypeInstance is not IObjectGraphType objectGraphType)
                throw new InvalidOperationException($"The type '{typeName}' is not an object graph type.");
            var resolver = graphTypeInstance.GetMetadata<IFederationResolver>(RESOLVER_METADATA)
                ?? throw new InvalidOperationException($"The type '{typeName}' has not been configured for GraphQL Federation.");

            // the entity resolver defines a source CLR type that the representation should be converted to (for convenience).
            // for object and dictionary types, we just pass the representation directly.
            // for other types, we attempt to deserialize the representation into the source type, matching each field being
            //   deserialized to a field on the object graph type, and using the field's graph type instance to deserialize the value.
            // this ensures that the scalar values are properly converted to the expected CLR types, using the scalar's conversion
            //   method as defined within the schema.
            object value;
            try
            {
                //can't use ObjectExtensions.ToObject because that requires an input object graph type for
                //  deserialization mapping
                //value = rep.ToObject(resolver.SourceType, null!);
                if (resolver.SourceType == typeof(object) || resolver.SourceType == typeof(Dictionary<string, object>) || resolver.SourceType == typeof(IDictionary<string, object?>))
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
        return ret;
    }

    /// <summary>
    /// Deserializes an object based on properties provided in a dictionary, using graph type information from
    /// an output graph type. Requires that the object type has a parameterless constructor.
    /// </summary>
    private static object ToObject(Type objectType, IObjectGraphType objectGraphType, IDictionary<string, object?> map)
    {
        // create an instance of the target CLR type
        var obj = Activator.CreateInstance(objectType)!;

        // loop through each field in the map and deserialize the value to the corresponding property on the object
        foreach (var item in map)
        {
            // skip the __typename field as it was already used to find the object graph type and is not intended to be deserialized
            if (item.Key == "__typename")
                continue;

            // find the field on the object graph type, and the corresponding property on the object type
            var field = objectGraphType.Fields.Find(item.Key)
                ?? throw new InvalidOperationException($"Field '{item.Key}' not found in graph type '{objectGraphType.Name}'.");
            var graphType = field.ResolvedType!;
            var prop = objectType.GetProperty(item.Key, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public)
                ?? throw new InvalidOperationException($"Property '{item.Key}' not found in type '{objectType.GetFriendlyName()}'.");

            // deserialize the value
            var value = Deserialize(item.Key, graphType, prop.PropertyType, item.Value);
            // convert the value to the property type if necessary using the ValueConverter (typical for ID fields that convert to numbers)
            if (value != null && !prop.PropertyType.IsInstanceOfType(value))
                value = ValueConverter.ConvertTo(value, prop.PropertyType);
            // set the property value
            prop.SetValue(obj, value);
        }
        return obj;
    }

    /// <summary>
    /// Deserializes a value based on the graph type and value provided.
    /// </summary>
    private static object? Deserialize(string fieldName, IGraphType graphType, Type valueType, object? value)
    {
        // unwrap non-null graph types
        if (graphType is NonNullGraphType nonNullGraphType)
        {
            if (value == null)
                throw new InvalidOperationException($"The non-null field '{fieldName}' has a null value.");
            graphType = nonNullGraphType.ResolvedType!;
        }

        // handle null values
        if (value == null)
            return null;

        // loop through list graph types and deserialize each element in the list
        if (graphType is ListGraphType listGraphType)
        {
            // cast/convert value to an array (it should already be an array)
            // note: coercing scalars to an array of a single element is not applicable here
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

        // handle scalar graph types
        if (graphType is ScalarGraphType scalarGraphType)
            return scalarGraphType.ParseValue(value);

        // handle object graph types
        if (graphType is IObjectGraphType objectGraphType)
        {
            if (value is not Dictionary<string, object?> dic)
                throw new InvalidOperationException($"The field '{fieldName}' is an object graph type but the value is not a dictionary");

            return ToObject(valueType, objectGraphType, dic);
        }

        // union and interface types are not supported
        throw new InvalidOperationException($"The field '{fieldName}' is not a scalar or object graph type.");
    }

    private class RepresentationDataLoader(IResolveFieldContext Context, Representation Representation) : IDataLoaderResult
    {
        public Task<object?> GetResultAsync(CancellationToken cancellationToken = default) => Representation.Resolver.ResolveAsync(Context, Representation.Value).AsTask();
    }

    /// <inheritdoc/>
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        // require the representations argument to be converted to the proper type before hitting this code
        // e.g.: representationArgument.Parser += (value) => EntityResolver.Instance.ConvertRepresentations(schema, (System.Collections.IList)value);
        var representations = (IEnumerable<Representation>)context.Arguments![REPRESENTATIONS_ARGUMENT].Value!;

        // now that the representations have been validated, we can use them to resolve the entities using
        //   the resolvers provided by the representations

        // note: context.Copy is implicit due to the returned object being a list; otherwise it would be necessary,
        //   as the context is referenced within a delegate passed to the SimpleDataLoader (see below)
        //context = context.Copy();

        var results = new List<RepresentationDataLoader>();
        foreach (var representation in representations)
        {
            // using a data loader here causes the resolvers to run in serial or parallel based on the selected execution strategy.
            // unfortunately this requires extra allocations whereas if the strategy was known this code could be optimized by
            // either awaiting each resolver or collecting them and performing WaitAll. Note that this code counts on the fact
            // that the context instance will not be reused due to a list being returned from this method.
            results.Add(new RepresentationDataLoader(context, representation));
        }

        return new(results);
    }
}
