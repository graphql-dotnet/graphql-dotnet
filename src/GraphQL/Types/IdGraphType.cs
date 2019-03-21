using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class IdGraphType : ScalarGraphType
    {
        public IdGraphType()
        {
            Name = "ID";
            //Description =
            //    "The `ID` scalar type represents a unique identifier, often used to re-fetch an object or " +
            //    "as key for a cache. The `ID` type appears in a JSON response as a `String`; however, it " +
            //    "is not intended to be human-readable. When expected as an input type, any string (such " +
            //    "as `\"4\"`) or integer (such as `4`) input value will be accepted as an `ID`.";
        }

        public override object Serialize(object value) => value?.ToString();

        public override object ParseValue(object value) => value?.ToString().Trim(' ', '"');

        public override object ParseLiteral(IValue value)
        {
            if (value is StringValue str)
            {
                return ParseValue(str.Value);
            }

            if (value is IntValue num)
            {
                return num.Value;
            }

            if (value is LongValue longVal)
            {
                return longVal.Value;
            }

            return null;
        }
    }
}
