using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    public class ObjectExtensionsTests
    {
        [Theory]
        [ClassData(typeof(CultureList))]
        public void convert_double(CultureInfo cultureInfo)
        {
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            /* Given */
            double value = 123.123d;
            Type floatType = typeof(double);


            /* When */
            var actual = ObjectExtensions.ConvertValue(value, floatType);

            /* Then */
            actual.ShouldBe(value);
        }

        [Theory]
        [ClassData(typeof(CultureList))]
        public void convert_decimal(CultureInfo cultureInfo)
        {
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            /* Given */
            decimal value = 123.123m;
            Type floatType = typeof(decimal);


            /* When */
            var actual = ObjectExtensions.ConvertValue(value, floatType);

            /* Then */
            actual.ShouldBe(value);
        }

        [Theory]
        [ClassData(typeof(CultureList))]
        public void convert_single(CultureInfo cultureInfo)
        {
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            /* Given */
            float value = 123.123f;
            Type floatType = typeof(float);


            /* When */
            var actual = ObjectExtensions.ConvertValue(value, floatType);

            /* Then */
            actual.ShouldBe(value);
        }
    }
}
