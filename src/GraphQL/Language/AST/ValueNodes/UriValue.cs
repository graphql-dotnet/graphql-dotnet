using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="Uri"/> value within a document.
    /// </summary>
    public class UriValue : ValueNode<Uri>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public UriValue(Uri value)
        {
            Value = value;
        }
    }
}
