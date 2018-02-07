using System;
using System.Linq;
using System.Reflection;

namespace GraphQL.Conversion
{
    // https://github.com/JasperFx/baseline/tree/master/src/Baseline/Conversion
    public class NullableConverter : IConversionProvider
    {
        private readonly Conversions _conversions;

        public NullableConverter(Conversions conversions)
        {
            _conversions = conversions;
        }

        public Func<string, object> ConverterFor(Type type)
        {
            if (!type.IsNullable()) return null;

            var innerType = type.GetGenericArguments().First();
            var inner = _conversions.FindConverter(innerType);

            return str =>
            {
                if (str == "NULL") return null;

                return inner(str);
            };
        }
    }
}
