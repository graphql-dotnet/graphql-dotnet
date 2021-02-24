using GraphQL.Types;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a value within a document with an unknown type.
    /// This class is used when a custom scalar returns a data type within its <see cref="ScalarGraphType.Serialize"/>
    /// method which returns a value of an unrecognized type intended to be serialized by the <see cref="IDocumentWriter"/>,
    /// and then only when setting the default value of field.
    /// </summary>
    public class UnknownValue : ValueNode<object>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public UnknownValue(object value) : base(value)
        {
        }
    }
}
