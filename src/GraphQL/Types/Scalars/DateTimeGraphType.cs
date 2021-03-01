using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The DateTime scalar graph type represents a date and time in accordance with the ISO-8601 standard.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="DateTime"/> .NET values to this scalar graph type.
    /// </summary>
    public class DateTimeGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeGraphType"/> class.
        /// </summary>
        public DateTimeGraphType()
        {
            Description =
                "The `DateTime` scalar type represents a date and time. `DateTime` expects timestamps " +
                "to be formatted in accordance with the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            NullValue _ => null,
            StringValue stringValue => ParseValue(stringValue.Value),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value == null ? null : ValueConverter.ConvertTo(value, typeof(DateTime));

        /// <inheritdoc/>
        public override IValue ToAST(object value) => value switch
        {
            null => new NullValue(),
            DateTime d => new StringValue(d.ToString("O")), // "O" is the proper ISO 8601 format required
            _ => null
        };
    }
}
