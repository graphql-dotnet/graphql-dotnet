using System.Collections;
using GraphQL.Conversion;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// This class provides value conversions between objects of different types.
/// Conversions are registered in a thread safe dictionary and are used for a specific schema instance.
/// <br/><br/>
/// Each ScalarGraphType calls <see cref="ValueConverterExtensions.ConvertTo(IValueConverter, object?, Type)">ConvertTo</see> method to return correct value
/// type from its <see cref=" GraphQL.Types.ScalarGraphType.ParseValue(object)">ParseValue</see> method.
/// Also conversions may be useful in advanced <see cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)">GetArgument</see>
/// use cases when deserialization from the values dictionary to the complex input argument is required.
/// </summary>
public class ValueConverter : ValueConverterBase
{
    /// <summary>
    /// Register built-in conversions. This list is expected to grow over time.
    /// </summary>
    [RequiresUnreferencedCode("Creates list and array types dynamically as needed.")]
    [RequiresDynamicCode("Creates list and array types dynamically as needed.")]
    public ValueConverter()
    {
        RegisterScalarConversions();

        // check if running under AOT
        var dynamicCodeCompiled =
#if NETSTANDARD2_0
            true;
#else
            System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled;
#endif

        // types that return an array
        RegisterListConverterFactory(typeof(ICollection), Conversion.ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IEnumerable), Conversion.ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IList), Conversion.ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IList<>), Conversion.ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IEnumerable<>), Conversion.ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(ICollection<>), Conversion.ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IReadOnlyList<>), Conversion.ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IReadOnlyCollection<>), Conversion.ArrayListConverterFactory.Instance);

        if (dynamicCodeCompiled)
        {
            // types that return a List<T>
            RegisterListConverterFactory(typeof(List<>), Conversion.DefaultListConverterFactory.Instance);

            // types that return a HashSet<T>
            RegisterListConverterFactory(typeof(ISet<>), HashSetListConverterFactory.Instance);
            RegisterListConverterFactory(typeof(HashSet<>), HashSetListConverterFactory.Instance);
#if NET5_0_OR_GREATER
            RegisterListConverterFactory(typeof(IReadOnlySet<>), HashSetListConverterFactory.Instance);
#endif
        }
        else // AOT scenarios
        {
            // CustomListConverterFactory.DefaultInstance contains custom logic for list types
            //   that implement IList when running under AOT. This includes List<T> and provides
            //   the best possible performance for List<T> in that scenario. It may not work as expected
            //   or may work slowly for other list types, such as HashSet<T>.

            // add mapping for hash set interface types
            RegisterListConverterFactory(typeof(ISet<>), new CustomListConverterFactory(typeof(HashSet<>)));
#if NET5_0_OR_GREATER
            RegisterListConverterFactory(typeof(IReadOnlySet<>), new CustomListConverterFactory(typeof(HashSet<>)));
#endif
        }
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode(
        "For generic list types, the constructed implementation type (e.g. List<T>) must be rooted for trimming. " +
        "If the closed generic type is only referenced via reflection, the trimmer may remove its required constructors " +
        "or other members, which can cause runtime failures.")]
    [RequiresDynamicCode("Compiles code at runtime to populate the specified type.")]
    public override void RegisterListConverterFactory(Type listType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type implementationType)
    {
        // check if running under AOT
        var dynamicCodeCompiled =
#if NETSTANDARD2_0
            true;
#else
            System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled;
#endif

        if (dynamicCodeCompiled && implementationType == typeof(List<>))
            RegisterListConverterFactory(listType, Conversion.DefaultListConverterFactory.Instance);
        else
            RegisterListConverterFactory(listType, new CustomListConverterFactory(implementationType));
    }

    /// <inheritdoc/>
    protected override IListConverterFactory? DefaultListConverterFactory
    {
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
            Justification = "The constructor is marked with RequiresDynamicCodeAttribute.")]
        [UnconditionalSuppressMessage("AOT", "IL2026:Calling members annotated with 'RequiresUnreferencedCodeAttribute' may break functionality when trimming application code.",
            Justification = "The constructor is marked with RequiresUnreferencedCodeAttribute.")]
        get => CustomListConverterFactory.DefaultInstance;
    }

    /// <inheritdoc/>
    protected override IListConverterFactory? ArrayListConverterFactory
    {
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
            Justification = "The constructor is marked with RequiresDynamicCode.")]
        get => Conversion.ArrayListConverterFactory.Instance;
    }

    /// <inheritdoc/>
    public override object ToObject(IDictionary<string, object?> source, Type type, IInputObjectGraphType inputGraphType)
    {
        var conversion = GetConversion(typeof(IDictionary<string, object?>), type);
        if (conversion != null)
            return conversion(source);

        return ToObjectImpl(source, type, inputGraphType);
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "The constructor is marked with RequiresDynamicCodeAttribute.")]
    [UnconditionalSuppressMessage("AOT", "IL2026:Calling members annotated with 'RequiresUnreferencedCodeAttribute' may break functionality when trimming application code.",
        Justification = "The constructor is marked with RequiresUnreferencedCodeAttribute.")]
    [UnconditionalSuppressMessage("AOT", "IL2067:Calling members with arguments having 'DynamicallyAccessedMembersAttribute' may break functionality when trimming application code.",
        Justification = "The constructor is marked with RequiresUnreferencedCodeAttribute.")]
    private object ToObjectImpl(IDictionary<string, object?> source, Type type, IInputObjectGraphType inputGraphType)
        => ObjectExtensions.ToObjectReflection(source, type, inputGraphType, this);
}
