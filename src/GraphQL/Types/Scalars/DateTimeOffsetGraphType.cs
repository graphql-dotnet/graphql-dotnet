using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The DateTimeOffset scalar graph type represents a date, time and offset from UTC.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="DateTimeOffset"/> .NET values to this scalar graph type.
    /// </summary>
    public class DateTimeOffsetGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffsetGraphType"/> class.
        /// </summary>
        public DateTimeOffsetGraphType()
        {
            Description =
                "The `DateTimeOffset` scalar type represents a date, time and offset from UTC. `DateTimeOffset` expects timestamps " +
                "to be formatted in accordance with the [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.";
        }

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            NullValue _ => null,
            StringValue s => ParseValue(s.Value),
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value == null ? null : ValueConverter.ConvertTo(value, typeof(DateTimeOffset));

        /// <inheritdoc/>
        public override IValue ToAST(object value) => value switch
        {
            null => new NullValue(),
            DateTimeOffset d => new StringValue(d.ToString("O")), // "O" is the proper ISO 8601 format required
            _ => null
        };
    }
}
