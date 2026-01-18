using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
using GraphQL.Conversion;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Base class for value conversion operations used by ToObject methods.
/// </summary>
public abstract class ValueConverterBase : IValueConverter
{
    /// <summary>
    /// Dictionary of registered value conversions.
    /// </summary>
    protected Dictionary<(Type, Type), Func<object, object>> ValueConversions { get; } = new();

    /// <summary>
    /// Dictionary of registered list converter factories.
    /// </summary>
    protected Dictionary<Type, IListConverterFactory> ListConverterFactories { get; } = new();

    /// <summary>
    /// Cache of list converters.
    /// </summary>
    private readonly ConcurrentDictionary<Type, IListConverter> _listConverterCache = new();

    /// <summary>
    /// Registers common built-in conversions.
    /// </summary>
    protected void RegisterScalarConversions()
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
    }

    /// <inheritdoc/>
    public virtual Func<object, object>? GetConversion(Type valueType, Type targetType)
    {
        return ValueConversions.TryGetValue((valueType, targetType), out var conversion)
            ? conversion
            : null;
    }

    /// <inheritdoc/>
    public IListConverter GetListConverter(Type listType)
    {
        return _listConverterCache.GetOrAdd(listType, type => GetListConverterFactory(type).Create(type));
    }

    /// <summary>
    /// Gets the list converter factory for the specified list type, if any.
    /// Array types are supported.
    /// </summary>
    public virtual IListConverterFactory GetListConverterFactory(Type listType)
    {
        if (listType.IsArray)
            return ArrayListConverterFactory.Instance;

        // if the list type is not explicitly registered
        if (!ListConverterFactories.TryGetValue(listType, out var converter)
            // and if the generic type definition is not explicitly registered
            && (!listType.IsConstructedGenericType
            || !ListConverterFactories.TryGetValue(listType.GetGenericTypeDefinition(), out converter)))
        {
            // then use the default list converter factory
            converter = DefaultListConverterFactory;
        }

        // but if the default list converter factory is not set, throw an exception
        return converter
            ?? throw new InvalidOperationException($"No list converter is registered for type '{listType.GetFriendlyName()}' and no default list converter is specified.");
    }

    /// <summary>
    /// Gets the default list converter factory for types that are not explicitly registered.
    /// Returns <see langword="null"/> by default, which will cause an exception to be thrown
    /// when attempting to convert a list type that is not explicitly registered.
    /// </summary>
    protected virtual IListConverterFactory? DefaultListConverterFactory => null;

    /// <summary>
    /// Allows you to register your own conversion delegate from one type to another.
    /// <br/><br/>
    /// If the conversion from valueType to targetType is already registered, then it will be overwritten.
    /// </summary>
    /// <param name="valueType">Type of original value.</param>
    /// <param name="targetType">Converted value type.</param>
    /// <param name="conversion">Conversion delegate; <see langword="null"/> for unregister already registered conversion.</param>
    public virtual void Register(Type valueType, Type targetType, Func<object, object>? conversion)
    {
        if (conversion == null)
            ValueConversions.Remove((valueType, targetType));
        else
            ValueConversions[(valueType, targetType)] = conversion;
    }

    /// <summary>
    /// Allows you to register your own conversion delegate from one type to another.
    /// <br/><br/>
    /// If the conversion from TSource to TTarget is already registered, then it will be overwritten.
    /// </summary>
    /// <typeparam name="TSource">Type of original value.</typeparam>
    /// <typeparam name="TTarget">Converted value type.</typeparam>
    /// <param name="conversion">Conversion delegate; <see langword="null"/> for unregister already registered conversion.</param>
    public virtual void Register<TSource, TTarget>(Func<TSource, TTarget>? conversion)
        => Register(typeof(TSource), typeof(TTarget), conversion == null ? null : v => conversion((TSource)v)!);

    /// <summary>
    /// Registers a list converter for a specified list type. Especially useful for AOT scenarios where
    /// dynamic compilation is not available. Each element type must be individually registered.
    /// To register an open generic list type, use <see cref="RegisterListConverterFactory(Type, Type)"/>.
    /// <para>
    /// Sample usage:
    /// <code>
    /// RegisterListConverter&lt;List&lt;int&gt;, int&gt;(list => list.ToList());
    /// </code>
    /// </para>
    /// </summary>
    public virtual void RegisterListConverter<TListType, TElementType>(Func<IEnumerable<TElementType>, TListType>? conversion) where TListType : IEnumerable<TElementType>
        => RegisterListConverterFactory(typeof(TListType), conversion != null ? new DelegateListConverter<TListType, TElementType>(conversion) : null);

    /// <summary>
    /// Registers or removes a list converter factory for a specified list type.
    /// The list type may be a generic type definition, such as <see cref="List{T}"/>
    /// or a non-generic collection type such as <see cref="IList"/>. Closed
    /// generic types are also supported, such as <c>List&lt;int&gt;</c>.
    /// Array types cannot be registered. If the converter is <see langword="null"/>,
    /// the factory is removed.
    /// </summary>
    public virtual void RegisterListConverterFactory(Type listType, IListConverterFactory? converter)
    {
        if (listType.IsArray)
            throw new ArgumentException("Array types cannot be registered.", nameof(listType));
        if (converter == null)
            ListConverterFactories.Remove(listType);
        else
            ListConverterFactories[listType] = converter;
        _listConverterCache.Clear();
    }

    /// <summary>
    /// Registers a generic list converter factory for a specified list type.
    /// For example, it can be used to register a custom list converter for <see cref="IList{T}"/>.
    /// If the implementation type is an open generic type, the generic type argument from the list
    /// type will be used to create the implementation type. The implementation type must have a
    /// public constructor and Add method that accepts a single argument of the generic type argument,
    /// or a public constructor that accepts a single argument of type <see cref="IEnumerable{T}"/>.
    /// </summary>
    [RequiresUnreferencedCode(
        "For generic list types, the constructed implementation type (e.g. List<T>) must be rooted for trimming. " +
        "If the closed generic type is only referenced via reflection, the trimmer may remove its required constructors " +
        "or other members, which can cause runtime failures.")]
    public abstract void RegisterListConverterFactory(Type listType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type implementationType);

    /// <inheritdoc/>
    public abstract object ToObject(IDictionary<string, object?> source, Type type, IInputObjectGraphType inputGraphType);
}
