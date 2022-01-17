using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    /// <summary>
    /// Represents a type unknown within this portion of the federated schema.
    /// </summary>
    public class AnyScalarGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public AnyScalarGraphType()
        {
            Name = "_Any";
        }

        /// <inheritdoc/>
        public override object? ParseLiteral(GraphQLValue value) => value switch
        {
            GraphQLObjectValue v => ParseObject(v),
            GraphQLListValue v => ParseList(v),
            GraphQLIntValue v => ParseInt(v),
            GraphQLFloatValue v => ParseFloat(v),
            GraphQLStringValue v => ParseString(v),
            GraphQLBooleanValue v => (v.Value == "true").Boxed(),
            GraphQLEnumValue e => e.Name.StringValue, //TODO:???
            GraphQLNullValue _ => null,
            _ => throw new NotSupportedException() // GraphQLVariable
        };

        /// <inheritdoc/>
        public override object? ParseValue(object? value) => value;

        /// <inheritdoc/>
        public override bool CanParseLiteral(GraphQLValue value) => true;

        /// <inheritdoc/>
        public override bool CanParseValue(object? value) => true;

        /// <inheritdoc/>
        public override bool IsValidDefault(object value) => true;

        /// <inheritdoc/>
        public override GraphQLValue? ToAST(object? value) => new AnyValue(value);

        private static readonly ReadOnlyDictionary<string, object?> _empty = new(new Dictionary<string, object?>());

        private IDictionary<string, object?> ParseObject(GraphQLObjectValue v)
        {
            if (v.Fields == null || v.Fields.Count == 0)
            {
                return _empty;
            }
            else
            {
                var @object = new Dictionary<string, object?>(v.Fields.Count);
                foreach (var field in v.Fields)
                    @object.Add(field.Name.StringValue, ParseLiteral(field.Value));
                return @object;
            }
        }

        private IList<object?> ParseList(GraphQLListValue v)
        {
            if (v.Values == null || v.Values.Count == 0)
            {
                return Array.Empty<object?>();
            }
            else
            {
                var list = new object?[v.Values.Count];
                for (int i = 0; i < v.Values.Count; ++i)
                    list[i] = ParseLiteral(v.Values[i]);
                return list;
            }
        }

        private object ParseInt(GraphQLIntValue v)
        {
            if (v.Value.Length == 0)
                throw new InvalidOperationException("Invalid number (empty string)");

            if (Int.TryParse(v.Value, out int intResult))
            {
                return intResult;
            }

            // If the value doesn't fit in an integer, revert to using long...
            if (Long.TryParse(v.Value, out long longResult))
            {
                return longResult;
            }

            // If the value doesn't fit in an long, revert to using decimal...
            if (Decimal.TryParse(v.Value, out decimal decimalResult))
            {
                return decimalResult;
            }

            // If the value doesn't fit in an decimal, revert to using BigInteger...
            if (BigInt.TryParse(v.Value, out var bigIntegerResult))
            {
                return bigIntegerResult;
            }

            // Since BigInteger can contain any valid integer (arbitrarily large), this is impossible to trigger via an invalid query
            throw new InvalidOperationException($"Invalid number {v.Value}");
        }

        private object ParseFloat(GraphQLFloatValue v)
        {
            if (v.Value.Length == 0)
                throw new InvalidOperationException("Invalid number (empty string)");

            // the idea is to see if there is a loss of accuracy of value
            // for example, 12.1 or 12.11 is double but 12.10 is decimal
            if (!Double.TryParse(v.Value, out double dbl))
            {
                dbl = v.Value.Span[0] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
            }

            //it is possible for a FloatValue to overflow a decimal; however, with a double, it just returns Infinity or -Infinity
            if (Decimal.TryParse(v.Value, out decimal dec))
            {
                // Cast the decimal to our struct to avoid the decimal.GetBits allocations.
                var decBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref dec);
                decimal temp = new decimal(dbl);
                var dblAsDecBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref temp);
                if (!decBits.Equals(dblAsDecBits))
                    return dec;
            }

            return dbl;
        }

        private object ParseString(GraphQLStringValue v)
        {
            return v.Value.Length == 0
                ? string.Empty
                : (string)v.Value;
        }
    }
}
