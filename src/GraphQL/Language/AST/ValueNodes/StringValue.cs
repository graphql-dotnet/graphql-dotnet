#nullable enable

using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="string"/> value within a document.
    /// </summary>
    public class StringValue : ValueNode<string>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public StringValue(string value) : base(value ?? throw new ArgumentNullException(nameof(value)))
        {
        }
    }
}
