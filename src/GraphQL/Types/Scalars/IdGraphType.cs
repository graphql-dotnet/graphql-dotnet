using System;
using System.Numerics;
using GraphQL.Language.AST;

#nullable enable

namespace GraphQL.Types
{
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
        public override object? ParseLiteral(IValue value) => value switch
        {
            StringValue str => str.Value,
            IntValue num => num.Value,
            LongValue longVal => longVal.Value,
            BigIntValue bigIntValue => bigIntValue.Value,
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value),
        };

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            StringValue _ => true,
            IntValue _ => true,
            LongValue _ => true,
            BigIntValue _ => true,
            NullValue _ => true,
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
        public override object? Serialize(object? value) => ParseValue(value)?.ToString();
    }
}
