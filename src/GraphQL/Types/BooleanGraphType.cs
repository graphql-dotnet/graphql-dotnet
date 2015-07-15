namespace GraphQL.Types
{
    public class BooleanGraphType : ScalarGraphType
    {
        public BooleanGraphType()
        {
            Name = "Boolean";
        }

        public override object Coerce(object value)
        {
            if (value != null)
            {
                var stringValue = value.ToString().ToLower();
                switch (stringValue)
                {
                    case "false":
                    case "0":
                        return false;
                    case "true":
                    case "1":
                        return true;
                }
            }

            return null;
        }
    }
}
