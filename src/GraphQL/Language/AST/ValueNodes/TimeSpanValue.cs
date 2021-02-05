using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="TimeSpan"/> value within a document.
    /// </summary>
    public class TimeSpanValue : ValueNode<TimeSpan>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public TimeSpanValue(TimeSpan value)
        {
            Value = value;
        }
    }
}
