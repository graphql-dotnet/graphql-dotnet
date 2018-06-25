using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class BooleanGraphType : ScalarGraphType
    {
        public BooleanGraphType()
        {
            Name = "Boolean";
        }

        public override object Serialize(object value) =>
            this.ParseValue(value);

        public override object ParseValue(object value) =>
            ValueConverter.ConvertTo(value, typeof(bool));

        public override object ParseLiteral(IValue value)
        {
            var boolVal = value as BooleanValue;
            return boolVal?.Value;
        }
    }
}
