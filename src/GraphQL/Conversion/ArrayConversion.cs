using System;

namespace GraphQL.Conversion
{
    // https://github.com/JasperFx/baseline/tree/master/src/Baseline/Conversion
    public class ArrayConversion : IConversionProvider
    {
        private readonly Conversions _conversions;

        public ArrayConversion(Conversions conversions)
        {
            _conversions = conversions;
        }

        public Func<string, object> ConverterFor(Type type)
        {
            if (!type.IsArray) return null;

            var innerType = type.GetElementType();
            var inner = _conversions.FindConverter(innerType);

            return stringValue =>
            {
                if (stringValue.ToUpper() == "EMPTY" || stringValue.Trim().IsEmpty())
                {
                    return Array.CreateInstance(innerType, 0);
                }

                var strings = stringValue.ToDelimitedArray();
                var array = Array.CreateInstance(innerType, strings.Length);

                for (var i = 0; i < strings.Length; i++)
                {
                    var value = inner(strings[i]);
                    array.SetValue(value, i);
                }

                return array;
            };
        }
    }
}
