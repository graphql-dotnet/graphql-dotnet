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
        public override object ParseLiteral(IValue value)
            => value is StringValue stringValue ? ParseValue(stringValue.Value) : null;

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(DateTime));

        /// <inheritdoc/>
        public override IValue ToAst(object value) => new StringValue(((DateTime)value).ToString("O"));
    }
}
