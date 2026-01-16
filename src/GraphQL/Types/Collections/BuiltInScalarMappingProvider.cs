using System.Collections.ObjectModel;
using System.Numerics;

namespace GraphQL.Types;

/// <summary>
/// Maps CLR types to built-in scalar graph types.
/// </summary>
public sealed class BuiltInScalarMappingProvider : IGraphTypeMappingProvider
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

    /// <inheritdoc/>
    public Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, Type? preferredType)
    {
        if (preferredType != null)
            return preferredType;

        // Check built-in scalar mappings
        if (BuiltInScalarMappings.TryGetValue(clrType, out var builtInType))
            return builtInType;

        return null;
    }
}
