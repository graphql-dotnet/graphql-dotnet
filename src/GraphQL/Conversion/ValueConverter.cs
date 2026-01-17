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
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
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

        // types that return an array (fully supported by AOT, if the array type is not trimmed)
        RegisterListConverterFactory(typeof(ICollection), ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IEnumerable), ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IList), ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IList<>), ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IEnumerable<>), ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(ICollection<>), ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IReadOnlyList<>), ArrayListConverterFactory.Instance);
        RegisterListConverterFactory(typeof(IReadOnlyCollection<>), ArrayListConverterFactory.Instance);

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
    protected override IListConverterFactory? GetDefaultListConverterFactory() => CustomListConverterFactory.DefaultInstance;

    /// <inheritdoc/>
    public override object ToObject(IDictionary<string, object?> source, Type type, IInputObjectGraphType inputGraphType)
    {
        var conversion = GetConversion(typeof(IDictionary<string, object?>), type);
        if (conversion != null)
            return conversion(source);

        return ObjectExtensions.ToObjectReflection(source, type, inputGraphType, this);
    }
}
