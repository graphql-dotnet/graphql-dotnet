using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a value node that represents a reference to a variable within a document.
    /// </summary>
    public class VariableReference : GraphQLVariable, IValue
    {
        object IValue.ClrValue => Name;
    }
}
