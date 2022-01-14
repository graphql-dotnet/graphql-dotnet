using System;
using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="string"/> value within a document.
    /// </summary>
    public class StringValue : GraphQLStringValue, IValue<string>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public StringValue(string value)
        {
            ClrValue = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string ClrValue { get; }

        object? IValue.ClrValue => ClrValue;
    }
}
