using GraphQL.Language.AST;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    public class AnyValue : GraphQLValue, IValue<object>
    {
        public AnyValue(object? value)
        {
            ClrValue = value!;
        }

        public override ASTNodeKind Kind => (ASTNodeKind)(-1); //TODO:!!!!!

        public object ClrValue { get; }

        object? IValue.ClrValue => ClrValue;
    }
}
