using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// A value converter optimized for AOT scenarios that doesn't perform automatic list converter registrations.
/// This class requires explicit registration of all list converters needed by the application.
/// </summary>
public class ValueConverterAot : ValueConverterBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValueConverterAot"/> with basic type conversions registered.
    /// </summary>
    public ValueConverterAot()
    {
        RegisterBasics();
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">
    /// This method is not supported in AOT scenarios. Use <see cref="ValueConverterBase.RegisterListConverter{TListType, TElementType}(Func{IEnumerable{TElementType}, TListType}?)"/> instead.
    /// </exception>
    [RequiresUnreferencedCode(
        "For generic list types, the constructed implementation type (e.g. List<T>) must be rooted for trimming. " +
        "If the closed generic type is only referenced via reflection, the trimmer may remove its required constructors " +
        "or other members, which can cause runtime failures.")]
    public override void RegisterListConverterFactory(Type listType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type implementationType)
    {
        throw new NotSupportedException(
            "RegisterListConverterFactory(Type, Type) is not supported in AOT scenarios. " +
            "Use RegisterListConverter<TListType, TElementType>(Func<IEnumerable<TElementType>, TListType>) to explicitly register list converters for each element type needed.");
    }

    /// <inheritdoc/>
    public override object ToObject(IDictionary<string, object?> source, Type type, IInputObjectGraphType inputGraphType)
    {
        var conversion = GetConversion(typeof(IDictionary<string, object?>), type);
        if (conversion != null)
            return conversion(source);

        throw new InvalidOperationException($"No conversion registered from '{typeof(IDictionary<string, object?>).GetFriendlyName()}' to '{type.GetFriendlyName()}'.");
    }
}
