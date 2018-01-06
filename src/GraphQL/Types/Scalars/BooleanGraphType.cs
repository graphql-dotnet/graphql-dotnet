using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class BooleanGraphType : ScalarGraphType<bool?>
    {
        public BooleanGraphType()
        {
            Name = "Boolean";
        }

        public override object Serialize(object value)
        {
            if (value is bool)
            {
                return (bool) value;
            }

            return false;
        }

        public override bool? ParseValue(object value)
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

        public override object ParseLiteral(IValue value)
        {
            var boolVal = value as BooleanValue;
            return boolVal?.Value;
        }
    }
}
