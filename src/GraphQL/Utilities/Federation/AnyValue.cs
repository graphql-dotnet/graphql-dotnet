using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    public class AnyValue : GraphQLValue
    {
        public AnyValue(object? value)
        {
            Value = value!;
        }

        public override ASTNodeKind Kind => (ASTNodeKind)(-1); //TODO:!!!!!

        public object? Value { get; }
    }
}
