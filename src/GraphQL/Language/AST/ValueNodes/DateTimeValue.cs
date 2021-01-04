using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="DateTime"/> value within a document.
    /// </summary>
    public class DateTimeValue : ValueNode<DateTime>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public DateTimeValue(DateTime value)
        {
            Value = value;
        }
    }
}
