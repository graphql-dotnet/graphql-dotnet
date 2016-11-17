using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class IdGraphType : ScalarGraphType
    {
        public IdGraphType()
        {
            Name = "ID";
            //Description =
            //    "The `ID` scalar type represents a unique identifier, often used to refetch an object or " +
            //    "as key for a cache. The `ID` type appears in a JSON response as a `String`; however, it " +
            //    "is not intended to be human-readable. When expected as an input type, any string (such " +
            //    "as `\"4\"`) or integer (such as `4`) input value will be accepted as an `ID`.";
        }

        public override object Serialize(object value)
        {
            return value?.ToString();
        }

        public override object ParseValue(object value)
        {
            return value?.ToString().Trim(' ', '"');
        }

        public override object ParseLiteral(IValue value)
        {
            var str = value as StringValue;
            if (str != null)
            {
                return ParseValue(str.Value);
            }

            var num = value as IntValue;
            if (num != null)
            {
                return num.Value;
            }

            var longVal = value as LongValue;
            if (longVal != null)
            {
                return longVal.Value;
            }

            return null;
        }
    }
}
