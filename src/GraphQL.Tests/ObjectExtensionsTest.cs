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
            var actual = ObjectExtensions.ConvertValue(value, floatType);

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
            var actual = ObjectExtensions.ConvertValue(value, floatType);

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
            var actual = ObjectExtensions.ConvertValue(value, floatType);

            /* Then */
            actual.ShouldBe(value);
        }
        
                [Fact]
        public void convert_double_array_to_array()
        {
            // Arrange
            var doubles = new double[] { 1.00, 2.01, 3.14 };

            // Act 
            var result = doubles.GetPropertyValue(typeof(double[]));

            // Assert
            Assert.Equal(result, doubles);
        }

        [Fact]
        public void convert_double_list_to_array()
        {
            // Arrange
            var doubles = new List<double>() { 1.00, 2.01, 3.14 };

            // Act 
            var result = doubles.GetPropertyValue(typeof(double[]));

            // Assert
            Assert.Equal(result, doubles);
        }

        [Fact]
        public void convert_ndouble_list_to_array()
        {
            // Arrange
            var doubles = new List<double?>() { 1.00, 2.01, 3.14, null };

            // Act 
            var result = doubles.GetPropertyValue(typeof(double?[]));

            // Assert
            Assert.Equal(result, doubles);
        }

        [Fact]
        public void convert_ndouble_array_to_array()
        {
            // Arrange
            var doubles = new double?[] { 1.00, 2.01, 3.14 };

            // Act 
            var result = doubles.GetPropertyValue(typeof(double?[]));

            // Assert
            Assert.Equal(result, doubles);
        }

        [Fact]
        public void convert_double_list_to_list()
        {
            // Arrange
            var doubles = new List<double>() { 1.00, 2.01, 3.14 };

            // Act 
            var result = doubles.GetPropertyValue(typeof(List<double>));

            // Assert
            Assert.Equal(result, doubles);
        }

        [Fact]
        public void convert_strings_array_to_array()
        {
            // Arrange
            var strings = new string[] { "foo", "bar", "new" };

            // Act 
            var result = strings.GetPropertyValue(typeof(string[]));

            // Assert
            Assert.Equal(result, strings);
        }

        [Fact]
        public void convert_strings_list_to_array()
        {
            // Arrange
            var strings = new List<string>() { "foo", "bar", "new" };

            // Act 
            var result = strings.GetPropertyValue(typeof(string[]));

            // Assert
            Assert.Equal(result, strings);
        }

        [Fact]
        public void convert_strings_list_to_list()
        {
            // Arrange
            var strings = new List<string>() { "foo", "bar", "new" };

            // Act 
            var result = strings.GetPropertyValue(typeof(List<string>));

            // Assert
            Assert.Equal(result, strings);
        }
    }
}
