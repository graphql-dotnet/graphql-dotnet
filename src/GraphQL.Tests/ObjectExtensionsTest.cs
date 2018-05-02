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
        [Fact]
        public void convert_double_using_cultures() 
        {
            CultureTestHelper.UseCultures(convert_double);
        } 

        [Fact]
        public void convert_double()
        {
            /* Given */
            double value = 123.123d;
            Type floatType = typeof(double);


            /* When */
            var actual = ValueConverter.ConvertTo(value, floatType);

            /* Then */
            actual.ShouldBe(value);
        }

        [Fact]
        public void convert_decimal_using_cultures()
        {
            CultureTestHelper.UseCultures(convert_decimal);
        }

        [Fact]
        public void convert_decimal()
        {
            /* Given */
            decimal value = 123.123m;
            Type floatType = typeof(decimal);


            /* When */
            var actual = ValueConverter.ConvertTo(value, floatType);

            /* Then */
            actual.ShouldBe(value);
        }

        [Fact]
        public void convert_single_using_cultures()
        {
            CultureTestHelper.UseCultures(convert_single);
        }

        [Fact]
        public void convert_single()
        { 

            /* Given */
            float value = 123.123f;
            Type floatType = typeof(float);


            /* When */
            var actual = ValueConverter.ConvertTo(value, floatType);

            /* Then */
            actual.ShouldBe(value);
        }
    }
}
