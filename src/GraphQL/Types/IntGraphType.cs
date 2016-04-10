namespace GraphQL.Types
{
    public class IntGraphType : ScalarGraphType
    {
        public IntGraphType()
        {
            Name = "Int";
        }

        public override object Coerce(object value)
        {
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
    }
}
