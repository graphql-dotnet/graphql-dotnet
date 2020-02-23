using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Int scalar type represents a signed 32‐bit numeric non‐fractional value.
    /// https://graphql.github.io/graphql-spec/June2018/#sec-Int
    /// </summary>
    public class IntGraphType : ScalarGraphType
    {
        public override object ParseLiteral(IValue value)
        {
            if (value is IntValue intValue)
            {
                return intValue.Value;
            }

            return null;
        }

        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(int));

        public override object Serialize(object value) => ParseValue(value);
    }
}
