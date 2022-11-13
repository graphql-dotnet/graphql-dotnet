using System.Numerics;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Provides extension methods for schemas.
/// </summary>
public static class SchemaExtensions
{
    /// <summary>
    /// Registers a bunch of GraphQL scalars that represent CLR "primitive" types
    /// like <see cref="DateTime"/>, <see cref="long"/> or <see cref="decimal"/>.
    /// </summary>
    public static TSchema RegisterBCLScalars<TSchema>(this TSchema schema)
        where TSchema : ISchema
    {
        var mappings = new Dictionary<Type, Type>
        {
            [typeof(long)] = typeof(LongGraphType),
            [typeof(BigInteger)] = typeof(BigIntGraphType),
            [typeof(decimal)] = typeof(DecimalGraphType),
            [typeof(DateTime)] = typeof(DateTimeGraphType),
#if NET6_0_OR_GREATER
            [typeof(DateOnly)] = typeof(DateOnlyGraphType),
            [typeof(TimeOnly)] = typeof(TimeOnlyGraphType),
#endif
            [typeof(DateTimeOffset)] = typeof(DateTimeOffsetGraphType),
            [typeof(TimeSpan)] = typeof(TimeSpanSecondsGraphType),
            [typeof(short)] = typeof(ShortGraphType),
            [typeof(ushort)] = typeof(UShortGraphType),
            [typeof(ulong)] = typeof(ULongGraphType),
            [typeof(uint)] = typeof(UIntGraphType),
            [typeof(byte)] = typeof(ByteGraphType),
            [typeof(sbyte)] = typeof(SByteGraphType),
            [typeof(Uri)] = typeof(UriGraphType),
        };

        var scalars = new IGraphType[]
        {
            new DateGraphType(),
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
        };

        foreach (var mapping in mappings)
            schema.RegisterTypeMapping(mapping.Key, mapping.Value);

        foreach (var scalar in scalars)
            schema.RegisterType(scalar);

        return schema;
    }
}
