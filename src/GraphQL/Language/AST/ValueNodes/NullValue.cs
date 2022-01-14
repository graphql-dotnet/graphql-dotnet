using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents the 'null' value within a document.
    /// </summary>
    public class NullValue : GraphQLNullValue, IValue
    {
        object? IValue.ClrValue => null;
    }
}
