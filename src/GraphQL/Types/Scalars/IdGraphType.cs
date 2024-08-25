using System.Collections;
using System.Globalization;
using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types;

/// <summary>
/// The ID scalar graph type represents a string identifier, not intended to be human-readable. It is one of the five built-in scalars.
/// When expected as an input type, any string or integer input value will be accepted as an ID.
/// By default <see cref="SchemaTypes"/> maps all <see cref="Guid"/> .NET values to this scalar graph type.
/// </summary>
public class IdGraphType : ScalarGraphType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdGraphType"/> class.
    /// </summary>
    public IdGraphType()
    {
        Name = "ID";
        //Description =
        //    "The `ID` scalar type represents a unique identifier, often used to re-fetch an object or " +
        //    "as key for a cache. The `ID` type appears in a JSON response as a `String`; however, it " +
        //    "is not intended to be human-readable. When expected as an input type, any string (such " +
        //    "as `\"4\"`) or integer (such as `4`) input value will be accepted as an `ID`.";
    }

    /// <inheritdoc/>
    public override object? ParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue str => (string)str.Value, //ISSUE:allocation
        GraphQLIntValue num when Int.TryParse(num.Value, out int i) => i,
        GraphQLIntValue num when Long.TryParse(num.Value, out long l) => l,
        GraphQLIntValue num when BigInt.TryParse(num.Value, out var b) => b,
        GraphQLNullValue _ => null,
        _ => ThrowLiteralConversionError(value),
    };

    /// <inheritdoc/>
    public override bool CanParseLiteral(GraphQLValue value) => value switch
    {
        GraphQLStringValue _ => true,
        GraphQLIntValue _ => true,
        GraphQLNullValue _ => true,
        _ => false
    };

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value switch
    {
        string _ => value,
        int _ => value,
        long _ => value,
        Guid _ => value,
        null => null,
        byte _ => value,
        sbyte _ => value,
        short _ => value,
        ushort _ => value,
        uint _ => value,
        ulong _ => value,
        BigInteger _ => value,
        _ => ThrowValueConversionError(value)
    };

    /// <inheritdoc/>
    public override object? Serialize(object? value) => value switch
    {
        string _ => value,
        int i => i.ToString(CultureInfo.InvariantCulture),
        long l => l.ToString(CultureInfo.InvariantCulture),
        Guid g => g.ToString("D"),
        null => null,
        byte b => b.ToString(CultureInfo.InvariantCulture),
        sbyte sb => sb.ToString(CultureInfo.InvariantCulture),
        short s => s.ToString(CultureInfo.InvariantCulture),
        ushort us => us.ToString(CultureInfo.InvariantCulture),
        uint ui => ui.ToString(CultureInfo.InvariantCulture),
        ulong ul => ul.ToString(CultureInfo.InvariantCulture),
        BigInteger bi => bi.ToString(CultureInfo.InvariantCulture),
        _ => ThrowSerializationError(value)
    };

    /// <inheritdoc/>
    public override bool CanSerializeList(IEnumerable list, bool allowNulls) =>
        CanSerializeList<int>(list, allowNulls) ||
        CanSerializeList<long>(list, allowNulls) ||
        CanSerializeList<Guid>(list, allowNulls) ||
        list is IEnumerable<string?> values && values.FastAll(static s => s != null);

    /// <inheritdoc/>
    public override IEnumerable SerializeList(IEnumerable list) => list switch
    {
        // CanSerializeList has already verified that these are not uint[] or ulong[]
        IEnumerable<int> values => values.Select(value => value.ToString(CultureInfo.InvariantCulture)),
        IEnumerable<int?> values => values.Select(value => value?.ToString(CultureInfo.InvariantCulture)),
        IEnumerable<long> values => values.Select(value => value.ToString(CultureInfo.InvariantCulture)),
        IEnumerable<long?> values => values.Select(value => value?.ToString(CultureInfo.InvariantCulture)),
        IEnumerable<Guid> values => values.Select(value => value.ToString("D")),
        IEnumerable<Guid?> values => values.Select(value => value?.ToString("D")),
        IEnumerable<string?> values => values,
        _ => throw new NotSupportedException(),
    };
}
