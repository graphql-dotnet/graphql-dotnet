using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    using Xunit.Sdk;

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
        
        [Fact]
        public void convert_double_array_to_array()
        {
            // Arrange
            var doubles = new double[] { 1.00, 2.01, 3.14 };

            // Act 
            var actual = doubles.GetPropertyValue(typeof(double[]));

            // Assert
            actual.ShouldBe(doubles);
        }

        [Fact]
        public void convert_double_list_to_array()
        {
            // Arrange
            var doubles = new List<double>() { 1.00, 2.01, 3.14 };

            // Act 
            var actual = doubles.GetPropertyValue(typeof(double[]));

            // Assert
            actual.ShouldBe(doubles);
        }

        [Fact]
        public void convert_ndouble_list_to_array()
        {
            // Arrange
            var doubles = new List<double?>() { 1.00, 2.01, 3.14, null };

            // Act 
            var actual = doubles.GetPropertyValue(typeof(double?[]));

            // Assert
            actual.ShouldBe(doubles);
        }

        [Fact]
        public void convert_ndouble_array_to_array()
        {
            // Arrange
            var doubles = new double?[] { 1.00, 2.01, 3.14 };

            // Act 
            var actual = doubles.GetPropertyValue(typeof(double?[]));

            // Assert
            actual.ShouldBe(doubles);
        }

        [Fact]
        public void convert_double_list_to_list()
        {
            // Arrange
            var doubles = new List<double>() { 1.00, 2.01, 3.14 };

            // Act 
            var actual = doubles.GetPropertyValue(typeof(List<double>));

            // Assert
            actual.ShouldBe(doubles);
        }

        [Fact]
        public void convert_double_list_of_arrays_to_list_of_arrays()
        {
            // Arrange
            var doubles = new List<double[]>() { new double[] { 1.00, 2.01, 3.14 }, new double[] { 3.25, 2.21, 1.10 } };

            // Act 
            var actual = doubles.GetPropertyValue(typeof(List<double[]>));

            // Assert
            actual.ShouldBe(doubles);
        }


        [Fact]
        public void convert_double_array_of_arrays_to_array_of_arrays()
        {
            // Arrange
            var doubles = new double[][] { new double[] { 1.00, 2.01, 3.14 }, new double[] { 3.25, 2.21, 1.10 } };

            // Act 
            var actual = doubles.GetPropertyValue(typeof(double[][]));

            // Assert
            actual.ShouldBe(doubles);
        }

        [Fact]
        public void convert_double_list_of_arrays_to_array_of_arrays()
        {
            // Arrange
            var doubles = new List<double[]>() { new double[] { 1.00, 2.01, 3.14 }, new double[] { 3.25, 2.21, 1.10 } };

            // Act 
            var actual = doubles.GetPropertyValue(typeof(double[][]));

            // Assert
            actual.ShouldBe(doubles);
        }


        [Fact]
        public void convert_strings_array_to_array()
        {
            // Arrange
            var strings = new string[] { "foo", "bar", "new" };

            // Act 
            var actual = strings.GetPropertyValue(typeof(string[]));

            // Assert
            actual.ShouldBe(strings);
        }

        [Fact]
        public void convert_strings_list_to_array()
        {
            // Arrange
            var strings = new List<string>() { "foo", "bar", "new" };

            // Act 
            var actual = strings.GetPropertyValue(typeof(string[]));

            // Assert
            actual.ShouldBe(strings);
        }

        [Fact]
        public void convert_strings_list_to_list()
        {
            // Arrange
            var strings = new List<string>() { "foo", "bar", "new" };

            // Act 
            var actual = strings.GetPropertyValue(typeof(List<string>));

            // Assert
            actual.ShouldBe(strings);
        }

        [Fact]
        public void convert_string_list_of_arrays_to_list_of_arrays()
        {
            // Arrange
            var strings = new List<string[]>() { new string[] { "foo", "bar", "boo" }, new string[] { "new", "year", "eve" } };

            // Act 
            var actual = strings.GetPropertyValue(typeof(List<string[]>));

            // Assert
            actual.ShouldBe(strings);
        }


        [Fact]
        public void convert_string_array_of_arrays_to_array_of_arrays()
        {
            // Arrange
            var strings = new string[][] { new string[] { "foo", "bar", "boo" }, new string[] { "new", "year", "eve" } };

            // Act 
            var actual = strings.GetPropertyValue(typeof(string[][]));

            // Assert
            actual.ShouldBe(strings);
        }

        [Fact]
        public void convert_string_list_of_arrays_to_array_of_arrays()
        {
            // Arrange
            var strings = new List<string[]>() { new string[] { "foo", "bar", "boo" }, new string[] { "new", "year", "eve" } };

            // Act 
            var actual = strings.GetPropertyValue(typeof(string[][]));

            // Assert
            actual.ShouldBe(strings);
        }

        [Fact]
        public void convert_string_list_of_lists_to_array_of_arrays()
        {
            // Arrange
            var strings = new List<List<string>>() { new List<string>() { "foo", "bar", "boo" }, new List<string>() { "new", "year", "eve" } };

            // Act 
            var actual = strings.GetPropertyValue(typeof(string[][]));

            // Assert
            actual.ShouldBe(strings);
        }
    }
}
