using System.Collections;
using System.Collections.ObjectModel;
using System.Numerics;
using GraphQL.Introspection;
using GraphQLParser;

namespace GraphQL.Types;

/// <summary>
/// An abstract base class that represents a list of all the graph types utilized by a schema.
/// Also provides lookup for all schema types.
/// </summary>
public abstract class SchemaTypesBase : IEnumerable<IGraphType>
{
    /// <summary>
    /// Returns a dictionary of default CLR type to graph type mappings for a set of built-in (primitive) types.
    /// </summary>
    public static ReadOnlyDictionary<Type, Type> BuiltInScalarMappings { get; } = new(new Dictionary<Type, Type>
    {
        [typeof(int)] = typeof(IntGraphType),
        [typeof(long)] = typeof(LongGraphType),
        [typeof(BigInteger)] = typeof(BigIntGraphType),
        [typeof(double)] = typeof(FloatGraphType),
        [typeof(float)] = typeof(FloatGraphType),
        [typeof(decimal)] = typeof(DecimalGraphType),
        [typeof(string)] = typeof(StringGraphType),
        [typeof(bool)] = typeof(BooleanGraphType),
        [typeof(DateTime)] = typeof(DateTimeGraphType),
#if NET5_0_OR_GREATER
        [typeof(Half)] = typeof(HalfGraphType),
#endif
#if NET6_0_OR_GREATER
        [typeof(DateOnly)] = typeof(DateOnlyGraphType),
        [typeof(TimeOnly)] = typeof(TimeOnlyGraphType),
#endif
        [typeof(DateTimeOffset)] = typeof(DateTimeOffsetGraphType),
        [typeof(TimeSpan)] = typeof(TimeSpanSecondsGraphType),
        [typeof(Guid)] = typeof(IdGraphType),
        [typeof(short)] = typeof(ShortGraphType),
        [typeof(ushort)] = typeof(UShortGraphType),
        [typeof(ulong)] = typeof(ULongGraphType),
        [typeof(uint)] = typeof(UIntGraphType),
        [typeof(byte)] = typeof(ByteGraphType),
        [typeof(sbyte)] = typeof(SByteGraphType),
        [typeof(Uri)] = typeof(UriGraphType),
    });

    /// <summary>
    /// Built-in scalar instances (preinitialized and shared across all schema instances).
    /// </summary>
    protected static readonly ReadOnlyDictionary<Type, ScalarGraphType> BuiltInScalars = new(new ScalarGraphType[]
    {
        new StringGraphType(),
        new BooleanGraphType(),
        new FloatGraphType(),
        new IntGraphType(),
        new IdGraphType(),
        new DateGraphType(),
#if NET5_0_OR_GREATER
        new HalfGraphType(),
#endif
#if NET6_0_OR_GREATER
        new DateOnlyGraphType(),
        new TimeOnlyGraphType(),
#endif
        new DateTimeGraphType(),
        new DateTimeOffsetGraphType(),
        new TimeSpanSecondsGraphType(),
        new TimeSpanMillisecondsGraphType(),
        new DecimalGraphType(),
        new UriGraphType(),
        new GuidGraphType(),
        new ShortGraphType(),
        new UShortGraphType(),
        new UIntGraphType(),
        new LongGraphType(),
        new BigIntGraphType(),
        new ULongGraphType(),
        new ByteGraphType(),
        new SByteGraphType(),
    }
    .ToDictionary(t => t.GetType()));

    /// <summary>
    /// Built-in scalar instances by name (preinitialized and shared across all schema instances).
    /// </summary>
    protected static readonly ReadOnlyDictionary<string, ScalarGraphType> BuiltInScalarsByName = new(BuiltInScalars.Values.ToDictionary(t => t.Name));

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GraphQLClrInputTypeReference<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GraphQLClrOutputTypeReference<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ListGraphType<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NonNullGraphType<>))]
    static SchemaTypesBase()
    {
        // The above attributes preserve those classes when T is a reference type, but not
        // when T is a value type, which is necessary for GraphQL CLR type references.
        // Also, specifying the closed generic type does not help, so the only way to force
        // the trimmer to preserve types such as GraphQLClrInputTypeReference<int> is to
        // directly reference the type within the compiled MSIL, even if the code does
        // not actually run. While this does not help with user defined structs, the combination
        // of the above attributes and below code will allow all built-in types to be handled
        // by the auto-registering graph types such as AutoRegisteringObjectGraphType and
        // similar code.

        if (BuiltInScalarMappings != null) // always true
            return; // no need to actually execute the below code, but it must be present in the compiled IL

        // prevent trimming of these input and output type reference types
        Preserve<int>();
        Preserve<long>();
        Preserve<BigInteger>();
        Preserve<double>();
        Preserve<float>();
        Preserve<decimal>();
        Preserve<string>();
        Preserve<bool>();
        Preserve<DateTime>();
#if NET5_0_OR_GREATER
        Preserve<Half>();
#endif
#if NET6_0_OR_GREATER
        Preserve<DateOnly>();
        Preserve<TimeOnly>();
#endif
        Preserve<DateTimeOffset>();
        Preserve<TimeSpan>();
        Preserve<Guid>();
        Preserve<short>();
        Preserve<ushort>();
        Preserve<ulong>();
        Preserve<uint>();
        Preserve<byte>();
        Preserve<sbyte>();
        Preserve<Uri>();

        static void Preserve<T>()
        {
            // force the MSIL to contain strong references to the specified type
            GC.KeepAlive(typeof(GraphQLClrInputTypeReference<T>));
            GC.KeepAlive(typeof(GraphQLClrOutputTypeReference<T>));
        }
    }

    /// <summary>
    /// Initializes a new instance with an empty dictionary.
    /// </summary>
    protected SchemaTypesBase() : this([])
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary that relates type names to graph types.</param>
    protected SchemaTypesBase(Dictionary<ROM, IGraphType> dictionary)
    {
        Dictionary = dictionary;
    }

    /// <summary>
    /// Returns a dictionary that relates type names to graph types.
    /// </summary>
    protected internal Dictionary<ROM, IGraphType> Dictionary { get; protected set; }

    /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
    public IEnumerator<IGraphType> GetEnumerator() => Dictionary.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets the count of all the graph types utilized by the schema.
    /// </summary>
    public int Count => Dictionary.Count;

    /// <summary>
    /// Returns a graph type instance from the lookup table by its GraphQL type name.
    /// </summary>
    public IGraphType? this[ROM typeName]
    {
        get
        {
            return typeName.IsEmpty
                ? throw new ArgumentOutOfRangeException(nameof(typeName), "A type name is required to lookup.")
                : Dictionary.TryGetValue(typeName, out var type) ? type : null;
        }
    }

    /// <summary>
    /// Returns the <see cref="FieldType"/> instance for the <c>__schema</c> meta-field.
    /// </summary>
    public FieldType SchemaMetaFieldType { get; protected set; } = new SchemaMetaFieldType();

    /// <summary>
    /// Returns the <see cref="FieldType"/> instance for the <c>__type</c> meta-field.
    /// </summary>
    public FieldType TypeMetaFieldType { get; protected set; } = new TypeMetaFieldType();

    /// <summary>
    /// Returns the <see cref="FieldType"/> instance for the <c>__typename</c> meta-field.
    /// </summary>
    public FieldType TypeNameMetaFieldType { get; protected set; } = new TypeNameMetaFieldType();
}
