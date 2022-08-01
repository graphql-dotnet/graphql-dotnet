using System.Collections;
using System.Numerics;
using GraphQLParser.AST;

namespace GraphQL.Types;

/// <summary>
/// A scalar that can represent a simple or complex object, including lists.
/// <br/><br/>
/// When used with the provided serializers, input objects within literals or variables are parsed as
/// <see cref="IDictionary{TKey, TValue}">IDictionary</see>&lt;<see cref="string"/>,<see cref="object"/>?&gt; objects,
/// and lists are parsed as <see cref="IEnumerable{T}">IEnumerable</see>&lt;<see cref="object"/>?&gt; objects.
/// Integers are parsed as <see cref="int"/>, <see cref="long"/> or <see cref="BigInteger"/> objects, while
/// floating point values are parsed as <see cref="double"/> or <see cref="decimal"/> objects.
/// <br/><br/>
/// Serialized (output) values are passed directly to the underlying JSON serializer, and as such will support any
/// object type they support, such as anonymous types.
/// <br/><br/>
/// If a default value is set on a <see cref="ComplexScalarGraphType"/> field, complex types must be limited to
/// numeric data types, booleans, strings, lists, or <see cref="IDictionary"/> objects containing <see cref="string"/>s as keys;
/// other complex types as well as <see cref="Guid"/>, <see cref="DateTime"/> and other similar types are not supported.
/// </summary>
public class ComplexScalarGraphType : ScalarGraphType
{
    /// <inheritdoc cref="ComplexScalarGraphType"/>
    public ComplexScalarGraphType()
    {
        Name = "Complex";
    }

    /// <inheritdoc/>
    public override object? ParseLiteral(GraphQLValue value)
    {
        return value switch
        {
            GraphQLNullValue => null,
            GraphQLIntValue intValue => ParseInteger(intValue),
            GraphQLFloatValue floatValue => ParseFloat(floatValue),
            GraphQLStringValue stringValue => (string)stringValue.Value,
            GraphQLBooleanValue booleanValue => booleanValue.BoolValue,
            GraphQLObjectValue objectValue => ParseObject(objectValue),
            GraphQLListValue listValue => ParseList(listValue),
            GraphQLVariable variableValue => ParseVariable(variableValue),
            _ => ThrowLiteralConversionError(value),
        };

        // todo: need access to request variables to be able to properly support this
        object? ParseVariable(GraphQLVariable node)
            => throw new NotSupportedException($"Cannot read referenced variable '{node.Name.Value}' within a complex literal object.");

        IDictionary<string, object?> ParseObject(GraphQLObjectValue node)
            => node.Fields?.ToDictionary(n => (string)n.Name.Value, n => ParseLiteral(n.Value), StringComparer.Ordinal) ?? new Dictionary<string, object?>();

        IEnumerable<object?> ParseList(GraphQLListValue node)
            => node.Values?.Select(ParseLiteral) ?? Array.Empty<object?>();

        object ParseInteger(GraphQLIntValue node)
        {
#if NETSTANDARD2_0
            if (int.TryParse((string)node.Value, out int intValue))
                return intValue;

            if (long.TryParse((string)node.Value, out long longValue))
                return longValue;
#else
            if (int.TryParse(node.Value, out int intValue))
                return intValue;

            if (long.TryParse(node.Value, out long longValue))
                return longValue;
#endif

            return BigInt.Parse(node.Value);
        }

        object ParseFloat(GraphQLFloatValue node)
        {
#if NETSTANDARD2_0
            bool isDouble = double.TryParse((string)node.Value, out double dbl);
            bool isDecimal = decimal.TryParse((string)node.Value, out decimal dec);
#else
            bool isDouble = double.TryParse(node.Value, out double dbl);
            bool isDecimal = decimal.TryParse(node.Value, out decimal dec);
#endif

            if (isDouble && !isDecimal)
                return dbl;

            if (!isDouble && isDecimal)
                return dec;

            if (isDouble && isDecimal)
            {
                // Cast the decimal to our struct to avoid the decimal.GetBits allocations.
                var decBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref dec);
                decimal temp = new(dbl);
                var dblAsDecBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref temp);
                return decBits.Equals(dblAsDecBits)
                    ? dbl
                    : dec;
            }

            return ThrowLiteralConversionError(node);
        }
    }

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value;

    /// <inheritdoc/>
    public override object? Serialize(object? value) => value;

    /// <inheritdoc/>
    public override GraphQLValue ToAST(object? value)
    {
        return value switch
        {
            null => new GraphQLNullValue(),

            true => new GraphQLTrueBooleanValue(),
            false => new GraphQLFalseBooleanValue(),

            byte n => new GraphQLIntValue(n),
            sbyte n => new GraphQLIntValue(n),
            short n => new GraphQLIntValue(n),
            ushort n => new GraphQLIntValue(n),
            int n => new GraphQLIntValue(n),
            uint n => new GraphQLIntValue(n),
            long n => new GraphQLIntValue(n),
            ulong n => new GraphQLIntValue(n),
            BigInteger n => new GraphQLIntValue(n),

            float n => new GraphQLFloatValue(n),
            double n => new GraphQLFloatValue(n),
            decimal n => new GraphQLFloatValue(n),

            string s => new GraphQLStringValue(s),

            IDictionary d => ConvertDictionary(d),
            IEnumerable l => ConvertList(l),

            _ => throw new NotImplementedException("Converting complex types to their AST representations is not supported.")
        };

        GraphQLObjectValue ConvertDictionary(IDictionary dictionary)
        {
            if (dictionary is IDictionary<string, object?> objectDictionary)
                return new()
                {
                    Fields = objectDictionary.Select(row => new GraphQLObjectField
                    {
                        Name = new GraphQLName(row.Key),
                        Value = ToAST(row.Value),
                    }).ToList(),
                };

            var fields = new List<GraphQLObjectField>(dictionary.Count);
            foreach (var key in dictionary.Keys)
            {
                if (key is not string keyString)
                    throw new InvalidOperationException("Object keys must be string values");
                var value = dictionary[key];
                fields.Add(new GraphQLObjectField
                {
                    Name = new GraphQLName(keyString),
                    Value = ToAST(value),
                });
            }
            return new() { Fields = fields };
        }

        GraphQLListValue ConvertList(IEnumerable list)
            => new GraphQLListValue { Values = list.Cast<object?>().Select(v => ToAST(v)).ToList() };
    }
}
