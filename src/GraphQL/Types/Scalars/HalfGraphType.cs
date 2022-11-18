#if NET5_0_OR_GREATER

using System.Numerics;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Types;

/// <summary>
/// The Half scalar graph type represents an IEEE 754 half-precision floating point value.
/// </summary>
public class HalfGraphType : ScalarGraphType
{
    /// <inheritdoc/>
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLIntValue x => Parse(value, x.Value),
        GraphQLFloatValue x => Parse(value, x.Value),
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value)
    };

    /// <inheritdoc/>
    public override bool CanParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLIntValue x => TryParse(x.Value),
        GraphQLFloatValue x => TryParse(x.Value),
        GraphQLNullValue _ => true,
        _ => false
    };

    private object Parse(GraphQLValue value, ROM rom)
    {
        var parsed = Half.Parse(rom);
        return Half.IsInfinity(parsed) ? ThrowLiteralConversionError(value) : parsed;
    }

    private bool TryParse(ROM rom)
    {
        return Half.TryParse(rom, out var parsed) && !Half.IsInfinity(parsed);
    }

    private object NotInfinity(Half value)
    {
        return Half.IsInfinity(value) ? ThrowValueConversionError(value) : value;
    }

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value switch
    {
        double db => NotInfinity(checked((Half)db)),
        int i => checked((Half)i),
        null => null,
        float f => NotInfinity(checked((Half)f)),
        decimal d => NotInfinity(checked((Half)(double)d)),
        Half h when !Half.IsInfinity(h) => value,
        sbyte sb => checked((Half)sb),
        byte b => checked((Half)b),
        short s => checked((Half)s),
        ushort us => checked((Half)us),
        uint ui => checked((Half)ui),
        long l => checked((Half)l),
        ulong ul => checked((Half)ul),
        BigInteger bi => checked((Half)(double)bi),
        _ => ThrowValueConversionError(value)
    };
}

#endif
