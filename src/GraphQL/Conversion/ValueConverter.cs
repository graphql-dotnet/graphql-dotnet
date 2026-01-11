using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
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
    private readonly Dictionary<(Type, Type), Func<object, object>> _valueConversions = new();
    private readonly Dictionary<Type, IListConverterFactory> _listConverterFactories = new();
    private readonly ConcurrentDictionary<Type, IListConverter> _listConverterCache = new();

    /// <summary>
    /// Register built-in conversions. This list is expected to grow over time.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    public ValueConverter()
    {
        Register<string, sbyte>(value => sbyte.Parse(value, NumberFormatInfo.InvariantInfo));
        Register<string, byte>(value => byte.Parse(value, NumberFormatInfo.InvariantInfo));
        Register<string, short>(value => short.Parse(value, NumberFormatInfo.InvariantInfo));
        Register<string, ushort>(value => ushort.Parse(value, NumberFormatInfo.InvariantInfo));
        Register<string, int>(value => int.Parse(value, NumberFormatInfo.InvariantInfo));
        Register<string, uint>(value => uint.Parse(value, NumberFormatInfo.InvariantInfo));
        Register<string, long>(value => long.Parse(value, NumberFormatInfo.InvariantInfo));
        Register<string, ulong>(value => ulong.Parse(value, NumberFormatInfo.InvariantInfo));
        Register<string, BigInteger>(value => BigInteger.Parse(value, NumberFormatInfo.InvariantInfo));
        Register<string, float>(value => float.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo));
        Register<string, double>(value => double.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo));
        Register<string, decimal>(value => decimal.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo));
#if NET6_0_OR_GREATER
        Register<string, DateOnly>(value => DateOnly.Parse(value, DateTimeFormatInfo.InvariantInfo));
        Register<string, TimeOnly>(value => TimeOnly.Parse(value, DateTimeFormatInfo.InvariantInfo));
#endif
        Register<string, DateTime>(value => DateTimeOffset.Parse(value, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal).UtcDateTime);
        Register<string, DateTimeOffset>(value => DateTimeOffset.Parse(value, DateTimeFormatInfo.InvariantInfo));
        Register(typeof(string), typeof(bool), value =>
        {
            string stringValue = (string)value;
            if (string.CompareOrdinal(stringValue, "1") == 0)
                return BoolBox.True;
            else if (string.CompareOrdinal(stringValue, "0") == 0)
                return BoolBox.False;

            return Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo).Boxed();
        });
        Register<string, Guid>(value => Guid.Parse(value));
        Register<string, Uri>(value => new Uri(value));
        Register<string, byte[]>(value => Convert.FromBase64String(value)); // such a built-in conversion for string->byte[] seems useful

        Register<DateTime, DateTimeOffset>(value => value);
        Register<DateTimeOffset, DateTime>(value => value.UtcDateTime);
        Register<TimeSpan, long>(value => (long)value.TotalSeconds);

        Register<int, sbyte>(value => checked((sbyte)value));
        Register<int, byte>(value => checked((byte)value));
        Register<int, short>(value => checked((short)value));
        Register<int, ushort>(value => checked((ushort)value));
        Register(typeof(int), typeof(bool), value => Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo).Boxed());
        Register<int, uint>(value => checked((uint)value));
        Register<int, long>(value => value);
        Register<int, ulong>(value => checked((ulong)value));
        Register<int, BigInteger>(value => new BigInteger(value));
        Register<int, double>(value => value);
        Register<int, float>(value => value);
        Register<int, decimal>(value => value);
        Register<int, TimeSpan>(value => TimeSpan.FromSeconds(value));
        Register<int, string>(value => value.ToString(CultureInfo.InvariantCulture));

        Register<long, sbyte>(value => checked((sbyte)value));
        Register<long, byte>(value => checked((byte)value));
        Register<long, short>(value => checked((short)value));
        Register<long, ushort>(value => checked((ushort)value));
        Register<long, int>(value => checked((int)value));
        Register<long, uint>(value => checked((uint)value));
        Register<long, ulong>(value => checked((ulong)value));
        Register<long, BigInteger>(value => new BigInteger(value));
        Register<long, double>(value => value);
        Register<long, float>(value => value);
        Register<long, decimal>(value => value);
        Register<long, TimeSpan>(value => TimeSpan.FromSeconds(value));
        Register<long, string>(value => value.ToString(CultureInfo.InvariantCulture));

        Register<BigInteger, sbyte>(value => checked((sbyte)value));
        Register<BigInteger, byte>(value => checked((byte)value));
        Register<BigInteger, decimal>(value => checked((decimal)value));
        Register<BigInteger, double>(value => checked((double)value));
        Register<BigInteger, short>(value => checked((short)value));
        Register<BigInteger, long>(value => checked((long)value));
        Register<BigInteger, sbyte>(value => checked((sbyte)value));
        Register<BigInteger, ushort>(value => checked((ushort)value));
        Register<BigInteger, uint>(value => checked((uint)value));
        Register<BigInteger, ulong>(value => checked((ulong)value));
        Register<BigInteger, int>(value => checked((int)value));
        Register<BigInteger, float>(value => checked((float)value));
        Register<BigInteger, string>(value => value.ToString(CultureInfo.InvariantCulture));

        Register<uint, sbyte>(value => checked((sbyte)value));
        Register<uint, byte>(value => checked((byte)value));
        Register<uint, int>(value => checked((int)value));
        Register<uint, long>(value => value);
        Register<uint, ulong>(value => value);
        Register<uint, short>(value => checked((short)value));
        Register<uint, ushort>(value => checked((ushort)value));
        Register<uint, BigInteger>(value => new BigInteger(value));

        Register<ulong, BigInteger>(value => new BigInteger(value));

        Register<byte, sbyte>(value => checked((sbyte)value));
        Register<byte, int>(value => value);
        Register<byte, long>(value => value);
        Register<byte, ulong>(value => value);
        Register<byte, short>(value => value);
        Register<byte, ushort>(value => value);
        Register<byte, BigInteger>(value => new BigInteger(value));

        Register<sbyte, byte>(value => checked((byte)value));
        Register<sbyte, int>(value => value);
        Register<sbyte, long>(value => value);
        Register<sbyte, ulong>(value => checked((ulong)value));
        Register<sbyte, short>(value => value);
        Register<sbyte, ushort>(value => checked((ushort)value));
        Register<sbyte, BigInteger>(value => new BigInteger(value));

        Register<float, double>(value => value);
        Register<float, decimal>(value => checked((decimal)value));
        Register<float, BigInteger>(value => new BigInteger(value));

        Register<double, float>(value => checked((float)value));
        Register<double, decimal>(value => checked((decimal)value));

        Register<char, byte>(value => checked((byte)value));
        Register<char, int>(value => value);

        Register<decimal, double>(value => checked((double)value));

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
    public override Func<object, object>? GetConversion(Type valueType, Type targetType)
    {
        return _valueConversions.TryGetValue((valueType, targetType), out var conversion)
            ? conversion
            : null;
    }

    /// <inheritdoc/>
    public override void Register(Type valueType, Type targetType, Func<object, object>? conversion)
    {
        if (conversion == null)
            _valueConversions.Remove((valueType, targetType));
        else
            _valueConversions[(valueType, targetType)] = conversion;
    }

    /// <inheritdoc/>
    public override void Register<TSource, TTarget>(Func<TSource, TTarget>? conversion)
        => Register(typeof(TSource), typeof(TTarget), conversion == null ? null : v => conversion((TSource)v)!);

    /// <inheritdoc/>
    public override void RegisterListConverterFactory(Type listType, IListConverterFactory? converter)
    {
        if (listType.IsArray)
            throw new ArgumentException("Array types cannot be registered.", nameof(listType));
        if (converter == null)
            _listConverterFactories.Remove(listType);
        else
            _listConverterFactories[listType] = converter;
        _listConverterCache.Clear();
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
    public override void RegisterListConverter<TListType, TElementType>(Func<IEnumerable<TElementType>, TListType>? conversion)
        => RegisterListConverterFactory(typeof(TListType), conversion != null ? new DelegateListConverter<TListType, TElementType>(conversion) : null);

    /// <summary>
    /// Specifies the default list converter factory for types that are not explicitly registered.
    /// When set to <see langword="null"/>, attempting to convert a list type that is not explicitly
    /// registerd will lead to an exception being thrown.
    /// </summary>
    public IListConverterFactory? DefaultListConverterFactory { get; set; } = CustomListConverterFactory.DefaultInstance;

    /// <inheritdoc/>
    public override IListConverterFactory GetListConverterFactory(Type listType)
    {
        if (listType.IsArray)
            return ArrayListConverterFactory.Instance;

        // if the list type is not explicitly registered
        if (!_listConverterFactories.TryGetValue(listType, out var converter)
            // and if the generic type definition is not explicitly registered
            && (!listType.IsConstructedGenericType
            || !_listConverterFactories.TryGetValue(listType.GetGenericTypeDefinition(), out converter)))
        {
            // then use the default list converter factory
            converter = DefaultListConverterFactory;
        }

        // but if the default list converter factory is not set, throw an exception
        return converter
            ?? throw new InvalidOperationException($"No list converter is registered for type '{listType.GetFriendlyName()}' and no default list converter is specified.");
    }

    /// <inheritdoc/>
    public override IListConverter GetListConverter(Type listType)
    {
        return _listConverterCache.GetOrAdd(listType, type => GetListConverterFactory(type).Create(type));
    }

    /// <inheritdoc/>
    public override object ToObject(IDictionary<string, object?> source, Type type, IInputObjectGraphType inputGraphType)
    {
        var conversion = GetConversion(typeof(IDictionary<string, object?>), type);
        if (conversion != null)
            return conversion(source);

        return ObjectExtensions.ToObjectReflection(source, type, inputGraphType, this);
    }
}
