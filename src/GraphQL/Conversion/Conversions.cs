using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GraphQL.Conversion
{
    // https://github.com/JasperFx/baseline/tree/master/src/Baseline/Conversion
    public class Conversions
    {
        private readonly LightweightCache<Type, Func<string, object>> _convertors;
        private readonly IList<IConversionProvider> _providers = new List<IConversionProvider>();

        public Conversions()
        {
            _convertors =
                new LightweightCache<Type, Func<string, object>>(
                    type =>
                    {
                        return providers().FirstValue(x => x.ConverterFor(type));
                    });

            RegisterConversion(bool.Parse);
            RegisterConversion(byte.Parse);
            RegisterConversion(sbyte.Parse);
            RegisterConversion(x =>
            {
                char c;
                char.TryParse(x, out c);
                return c;
            });
            RegisterConversion(ParseDecimal);
            RegisterConversion(ParseDouble);
            RegisterConversion(ParseFloat);
            RegisterConversion(short.Parse);
            RegisterConversion(int.Parse);
            RegisterConversion(long.Parse);
            RegisterConversion(ushort.Parse);
            RegisterConversion(uint.Parse);
            RegisterConversion(ulong.Parse);
            RegisterConversion(DateTimeConverter.GetDateTime);
            RegisterConversion(DateTimeOffsetConverter.GetDateTimeOffset);
            RegisterConversion(Guid.Parse);

            RegisterConversion(x =>
            {
                if (x == "EMPTY") return string.Empty;

                return x;
            });
        }

        public static float ParseFloat(string value)
        {
            return System.Convert.ToSingle(value, NumberFormatInfo.InvariantInfo);
        }

        public static double ParseDouble(string value)
        {
            return System.Convert.ToDouble(value, NumberFormatInfo.InvariantInfo);
        }

        public static decimal ParseDecimal(string value)
        {
            return System.Convert.ToDecimal(value, NumberFormatInfo.InvariantInfo);
        }

        private IEnumerable<IConversionProvider> providers()
        {
            foreach (var provider in _providers)
            {
                yield return provider;
            }

            yield return new EnumerationConversion();
            yield return new NullableConvertor(this);
            yield return new ArrayConversion(this);
            yield return new StringConverterProvider();
        }

        public void RegisterConversionProvider<T>() where T : IConversionProvider, new()
        {
            _providers.Add(new T());
        }

        public void RegisterConversion<T>(Func<string, T> convertor)
        {
            _convertors[typeof(T)] = x => convertor(x);
        }

        public Func<string, object> FindConverter(Type type)
        {
            return _convertors[type];
        }

        public object Convert(Type type, string raw)
        {
            return _convertors[type](raw);
        }

        public bool Has(Type type)
        {
            return _convertors.Has(type) || providers().Any(x => x.ConverterFor(type) != null);
        }
    }
}
