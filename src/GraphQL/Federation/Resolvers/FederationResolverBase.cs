using System.Collections;
using GraphQL.Types;

namespace GraphQL.Federation.Resolvers;

/// <inheritdoc cref="FederationResolverBase"/>
public abstract class FederationResolverBase<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TParsedType> : FederationResolverBase
{
    /// <inheritdoc cref="FederationResolverBase.FederationResolverBase"/>
    [RequiresUnreferencedCode("This uses reflection at runtime to deserialize the representation.")]
    protected FederationResolverBase()
    {
    }

    /// <inheritdoc/>
    public override Type SourceType => typeof(TParsedType);

    /// <inheritdoc/>
    public override ValueTask<object?> ResolveAsync(IResolveFieldContext context, IComplexGraphType graphType, object parsedRepresentation)
        => ResolveAsync(context, graphType, (TParsedType)parsedRepresentation);

    /// <inheritdoc cref="IFederationResolver.ResolveAsync(IResolveFieldContext, IComplexGraphType, object)"/>
    public abstract ValueTask<object?> ResolveAsync(IResolveFieldContext context, IComplexGraphType graphType, TParsedType parsedRepresentation);
}

/// <summary>
/// Provides an abstract implementation of <see cref="IFederationResolver"/> which includes parsing of the representation
/// into a specific CLR type.
/// </summary>
public abstract class FederationResolverBase : IFederationResolver
{
    /// <summary>
    /// Initializes a new instance of <see cref="FederationResolverBase"/>.
    /// </summary>
    [RequiresUnreferencedCode("This uses reflection at runtime to deserialize the representation.")]
    protected FederationResolverBase()
    {
    }

    /// <summary>
    /// Gets the CLR type of the representation that this resolver is responsible for.
    /// This property indicates the type to which the 'parsedRepresentation' parameter's representation
    /// will be converted before being passed to the <see cref="ResolveAsync(IResolveFieldContext, IComplexGraphType, object)"/> method.
    /// </summary>
    public abstract Type SourceType { get; }

    /// <inheritdoc/>
    public virtual bool MatchKeys(IDictionary<string, object?> representation) => true;

    /// <inheritdoc/>
    public abstract ValueTask<object?> ResolveAsync(IResolveFieldContext context, IComplexGraphType graphType, object parsedRepresentation);

    /// <inheritdoc/>
    public object ParseRepresentation(IComplexGraphType graphType, IDictionary<string, object?> representation, IValueConverter valueConverter)
    {
        // entity resolvers that derive from FederationResolverBase define a source CLR type (stored in SourceType)
        //   that the representation should be converted to (for convenience).
        // for object and dictionary types, we just pass the representation directly.
        // for other types, we attempt to deserialize the representation into the source type, matching each field being
        //   deserialized to a field on the object graph type, and using the field's graph type instance to deserialize the value.
        // this ensures that the scalar values are properly converted to the expected CLR types, using the scalar's conversion
        //   method as defined within the schema.

        //can't use ObjectExtensions.ToObject because that requires an input object graph type for
        //  deserialization mapping
        //value = rep.ToObject(resolver.SourceType, null!);
        if (SourceType == typeof(object) || SourceType == typeof(Dictionary<string, object?>) || SourceType == typeof(IDictionary<string, object?>))
            return representation;
        else
            return ToObject(SourceType, graphType, representation, valueConverter);
    }

    /// <summary>
    /// Deserializes an object based on properties provided in a dictionary, using graph type information from
    /// an output graph type. Requires that the object type has a parameterless constructor.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2067:Calling members with arguments having 'DynamicallyAccessedMembersAttribute' may break functionality when trimming application code.",
        Justification = "The constructor is marked with RequiresUnreferencedCodeAttribute.")]
    [UnconditionalSuppressMessage("AOT", "IL2070:Calling members with arguments having 'DynamicallyAccessedMembersAttribute' may break functionality when trimming application code.",
        Justification = "The constructor is marked with RequiresUnreferencedCodeAttribute.")]
    private static object ToObject(Type objectType, IComplexGraphType objectGraphType, IDictionary<string, object?> map, IValueConverter valueConverter)
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
            var value = Deserialize(item.Key, graphType, prop.PropertyType, item.Value, valueConverter);
            // convert the value to the property type if necessary using the ValueConverter (typical for ID fields that convert to numbers)
            if (value != null && !prop.PropertyType.IsInstanceOfType(value))
                value = valueConverter.ConvertTo(value, prop.PropertyType);
            // set the property value
            prop.SetValue(obj, value);
        }
        return obj;
    }

    /// <summary>
    /// Deserializes a value based on the graph type and value provided.
    /// </summary>
    private static object? Deserialize(string fieldName, IGraphType graphType, Type valueType, object? value, IValueConverter valueConverter)
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
            var listConverter = valueConverter.GetListConverter(valueType);
            var elementType = listConverter.ElementType;
            // deserialize each element in the array
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Deserialize(fieldName, listGraphType.ResolvedType!, elementType, array[i], valueConverter);
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

            return ToObject(valueType, objectGraphType, dic, valueConverter);
        }

        // union and interface types are not supported
        throw new InvalidOperationException($"The field '{fieldName}' is not a scalar or object graph type.");
    }
}
