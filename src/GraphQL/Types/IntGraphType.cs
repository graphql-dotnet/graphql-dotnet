using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class IntGraphType : ScalarGraphType
    {
        public IntGraphType()
        {
            Name = "Int";
        }

        public override object Serialize(object value)
        {
            return ParseValue(value);
        }

        public override object ParseValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            /* The Int scalar type represents non-fractional signed whole numeric values.
               Int can represent values between -(2^53 - 1) and 2^53 - 1 since represented
               in JSON as double-precision floating point numbers specified by IEEE 754 */
            int intResult;
            if (int.TryParse(value.ToString(), out intResult))
            {
                return intResult;
            }
            // If the value doesn't fit in an integer, revert to using long...
            long longResult;
            if (long.TryParse(value.ToString(), out longResult))
            {
                return longResult;
            }
            return null;
        }

        public override object ParseLiteral(IValue value)
        {
            var intValue = value as IntValue;
            if (intValue != null)
            {
                return intValue.Value;
            }

            var longValue = value as LongValue;
            return longValue?.Value;
        }
    }
}
