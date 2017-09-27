using System;
using System.Globalization;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    public class FloatGraphType : ScalarGraphType
    {
        public FloatGraphType()
        {
            Name = "Float";
        }

        public override object Serialize(object value)
        {
            return ParseValue(value);
        }

        public override object ParseValue(object value)
        {
            if (value == null)
                return null;

            try
            {
                var result = Convert.ToDouble(value, NumberFormatInfo.InvariantInfo);
                return result;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public override object ParseLiteral(IValue value)
        {
            var floatVal = value as FloatValue;
            if(floatVal != null) return floatVal?.Value;

            var intVal = value as IntValue;
            if (intVal != null) return intVal.Value;

            var longVal = value as LongValue;
            return longVal?.Value;
        }
    }
}
