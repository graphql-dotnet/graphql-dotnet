using System;

namespace GraphQL.Conversion
{
    // https://github.com/JasperFx/baseline/tree/master/src/Baseline/Conversion
    public interface IConversionProvider
    {
        /// <summary>
        /// Given the type argument, either return a
        /// Func that can parse a string into that Type
        /// or return null to let another IConversionProvider
        /// handle this type
        /// </summary>
        Func<string, object> ConverterFor(Type type);
    }
}
