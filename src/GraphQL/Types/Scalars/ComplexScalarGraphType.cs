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
    public override object? ParseLiteral(GraphQLValue value) => value.ParseAnyLiteral();

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value;

    /// <inheritdoc/>
    public override bool CanParseLiteral(GraphQLValue value) => value != null;

    /// <inheritdoc/>
    public override bool CanParseValue(object? value) => true;

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

            _ => throw new NotSupportedException($"Converting the complex type '{value.GetType().GetFriendlyName()}' to its AST representation is not supported.")
        };

        GraphQLObjectValue ConvertDictionary(IDictionary dictionary)
        {
            if (dictionary.Count == 0)
                return new();

            if (dictionary is IDictionary<string, object?> objectDictionary)
            {
                return new()
                {
                    Fields = objectDictionary
                        .Select(row => new GraphQLObjectField(
                            new GraphQLName(row.Key),
                            ToAST(row.Value)))
                        .ToList(),
                };
            }

            var fields = new List<GraphQLObjectField>(dictionary.Count);
            var enumerator = dictionary.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Key is not string keyString)
                    throw new InvalidOperationException("Object keys must be string values");
                fields.Add(new GraphQLObjectField(
                    new GraphQLName(keyString),
                    ToAST(enumerator.Value)));
            }
            return new() { Fields = fields };
        }

        GraphQLListValue ConvertList(IEnumerable list)
        {
            List<GraphQLValue>? values = null;
            foreach (var item in list)
            {
                (values ??= new()).Add(ToAST(item));
            }
            return new GraphQLListValue { Values = values };
        }
    }
}
