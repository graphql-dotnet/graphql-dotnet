using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class StringGraphType : ScalarGraphType
    {
        public StringGraphType() => Name = "String";

        public override object Serialize(object value) => value?.ToString();

        public override object ParseValue(object value) => value?.ToString();

        public override object ParseLiteral(IValue value) => (value as StringValue)?.Value;
    }
}
