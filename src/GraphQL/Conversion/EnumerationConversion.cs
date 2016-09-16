using System;
using System.Reflection;

namespace GraphQL.Conversion
{
    // https://github.com/JasperFx/baseline/tree/master/src/Baseline/Conversion
    public class EnumerationConversion : IConversionProvider
    {
        public Func<string, object> ConverterFor(Type type)
        {
            if (type.GetTypeInfo().IsEnum)
            {
                return x => Enum.Parse(type, x);
            }

            return null;
        }
    }
}
