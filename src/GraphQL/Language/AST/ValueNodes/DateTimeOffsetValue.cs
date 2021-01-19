using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="DateTimeOffset"/> value within a document.
    /// </summary>
    public class DateTimeOffsetValue : ValueNode<DateTimeOffset>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public DateTimeOffsetValue(DateTimeOffset value)
        {
            Value = value;
        }
    }
}
