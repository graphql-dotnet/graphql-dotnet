using GraphQL.Language.AST;

namespace GraphQL.Types
{

    /// <summary>
    /// The String scalar type represents textual data, represented as UTF‐8 character sequences.
    /// </summary>
    public class StringGraphType : ScalarGraphType<string>
    {
        public StringGraphType()
        {
            Name = "String";
        }

        public override object Serialize(object value)
        {
            return value;
        }

        public override string ParseValue(object value)
        {
            return value?.ToString();
        }

        public override object ParseLiteral(IValue value)
        {
            var stringValue = value as StringValue;
            return stringValue?.Value;
        }
    }
}
