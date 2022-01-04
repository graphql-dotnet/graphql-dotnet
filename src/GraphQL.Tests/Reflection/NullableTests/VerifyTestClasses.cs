using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Reflection.NullableTests
{
    public class VerifyTestClasses
    {
        // These tests verify that the .NET compiler is building the classes
        // with the expected attributes on them. Failure is not an
        // error, but simply indicates that the Method and Argument
        // and Property tests may not be testing the anticipated scenarios.
        //
        // Note: Method and Argument and Property tests should always pass
        // even if the compiler builds the code differently.

        [Theory]
        [InlineData(typeof(NullableClass1), 1, 0)] //default not nullable
        [InlineData(typeof(NullableClass2), 2, 0)] //default nullable
        [InlineData(typeof(NullableClass5), null, null)] //no default
        [InlineData(typeof(NullableClass6), null, null)]
        [InlineData(typeof(NullableClass7), 1, 0)]
        [InlineData(typeof(NullableClass8), 2, 0)]
        [InlineData(typeof(NullableClass9), null, null)]
        [InlineData(typeof(NullableClass10), null, null)]
        [InlineData(typeof(NullableClass11), 1, 0)]
        [InlineData(typeof(NullableClass12), null, null)]
        [InlineData(typeof(NullableClass13), 1, 0)]
        [InlineData(typeof(NullableClass14), 2, 0)]
        [InlineData(typeof(NullableClass15), null, null)]
        [InlineData(typeof(NullableClass16), 1, 0)]
        [InlineData(typeof(NullableClass16.NestedClass1), null, 0)]
        [InlineData(typeof(NullableClass17), 1, 0)]
        [InlineData(typeof(NullableClass18<>), null, null)]
        [InlineData(typeof(NullableClass19), 1, 0)]
        [InlineData(typeof(NullableClass20), 1, 0)]
        [InlineData(typeof(NullableClass21), null, null)]
        public void VerifyTestClass(Type type, int? nullableContext, int? nullable)
        {
            var actualHasNullableContext = type.CustomAttributes.FirstOrDefault(
                x => x.AttributeType.Name == "NullableContextAttribute");
            if (nullableContext == null)
            {
                actualHasNullableContext.ShouldBeNull();
            }
            else
            {
                actualHasNullableContext.ShouldNotBeNull();
                actualHasNullableContext.ConstructorArguments[0].Value.ShouldBe(nullableContext);
            }

            var actualHasNullable = type.CustomAttributes.FirstOrDefault(
                x => x.AttributeType.Name == "NullableAttribute");
            if (nullable == null)
            {
                actualHasNullable.ShouldBeNull();
            }
            else
            {
                actualHasNullable.ShouldNotBeNull();
                actualHasNullable.ConstructorArguments[0].ArgumentType.ShouldBe(typeof(byte));
                actualHasNullable.ConstructorArguments[0].Value.ShouldBe(nullable);
            }
        }

        [Theory]
        [InlineData(typeof(NullableClass1), "Field1", 2, null)] //method defaults as nullable
        [InlineData(typeof(NullableClass1), "Field2", 2, null)]
        [InlineData(typeof(NullableClass1), "Field3", null, null)] //method inherits NRT annotation from class
        [InlineData(typeof(NullableClass1), "Field4", null, null)]
        [InlineData(typeof(NullableClass1), "Field5", null, null)]
        [InlineData(typeof(NullableClass1), "Field6", null, null)]
        [InlineData(typeof(NullableClass1), "Field7", null, null)]
        [InlineData(typeof(NullableClass1), "Field8", null, null)]
        [InlineData(typeof(NullableClass1), "Field9", null, null)]
        [InlineData(typeof(NullableClass1), "Field10", null, null)]
        [InlineData(typeof(NullableClass2), "Field1", null, null)]
        [InlineData(typeof(NullableClass2), "Field2", null, null)]
        [InlineData(typeof(NullableClass2), "Field3", null, null)]
        [InlineData(typeof(NullableClass2), "Field4", null, null)]
        [InlineData(typeof(NullableClass2), "Field5", null, null)]
        [InlineData(typeof(NullableClass2), "Field6", null, null)]
        [InlineData(typeof(NullableClass2), "Field7", null, null)]
        [InlineData(typeof(NullableClass2), "Field8", null, null)]
        [InlineData(typeof(NullableClass2), "Field9", 1, null)] //method defaults as non-nullable
        [InlineData(typeof(NullableClass2), "Field10", 1, null)]
        [InlineData(typeof(NullableClass2), "Field11", null, null)]
        [InlineData(typeof(NullableClass5), "Test", 1, null)]
        [InlineData(typeof(NullableClass6), "Field1", 1, null)]
        [InlineData(typeof(NullableClass6), "Field2", 2, null)]
        [InlineData(typeof(NullableClass7), "Field1", null, null)]
        [InlineData(typeof(NullableClass7), "Field2", null, null)]
        [InlineData(typeof(NullableClass7), "Field3", 2, null)]
        [InlineData(typeof(NullableClass8), "Field1", null, null)]
        [InlineData(typeof(NullableClass8), "Field2", null, null)]
        [InlineData(typeof(NullableClass8), "Field3", 1, null)]
        [InlineData(typeof(NullableClass8), "Field4", null, null)]
        [InlineData(typeof(NullableClass9), "Field1", 2, "1")] //method including arguments defaults as nullable, but this method's return value is non-nullable
        [InlineData(typeof(NullableClass10), "Field1", 1, "2")]
        [InlineData(typeof(NullableClass11), "Field1", null, null)]
        [InlineData(typeof(NullableClass11), "Field2", null, "2")]
        [InlineData(typeof(NullableClass12), "Field1", 1, null)]
        [InlineData(typeof(NullableClass12), "Field2", null, "12")]
        [InlineData(typeof(NullableClass12), "Field3", null, "21")]
        [InlineData(typeof(NullableClass12), "Field4", 2, null)]
        [InlineData(typeof(NullableClass13), "Field1", null, null)]
        [InlineData(typeof(NullableClass13), "Field2", null, null)]
        [InlineData(typeof(NullableClass14), "Field1", null, null)]
        [InlineData(typeof(NullableClass14), "Field2", null, null)]
        [InlineData(typeof(NullableClass15), "Field1", 1, null)]
        [InlineData(typeof(NullableClass15), "Field2", null, "12")] //this method's return value has specific NRT annotations
        [InlineData(typeof(NullableClass15), "Field3", null, "21")]
        [InlineData(typeof(NullableClass15), "Field4", 2, null)]
        [InlineData(typeof(NullableClass16), "Field1", null, null)]
        [InlineData(typeof(NullableClass16), "Field2", null, null)]
        [InlineData(typeof(NullableClass16), "Field3", 2, null)]
        [InlineData(typeof(NullableClass16.NestedClass1), "Field1", null, null)]
        [InlineData(typeof(NullableClass16.NestedClass1), "Field2", null, null)]
        [InlineData(typeof(NullableClass16.NestedClass1), "Field3", 2, null)]
        [InlineData(typeof(NullableClass17), "Field1", null, null)]
        [InlineData(typeof(NullableClass17), "Field2", null, null)]
        [InlineData(typeof(NullableClass17), "Field3", null, "12")]
        [InlineData(typeof(NullableClass18<>), "Field1", null, "112")]
        [InlineData(typeof(NullableClass18<>), "Field2", null, "11221")]
        [InlineData(typeof(NullableClass18<>), "Field3", null, "12")]
        [InlineData(typeof(NullableClass18<>), "Field4", null, "12")]
        [InlineData(typeof(NullableClass18<>), "Field5", null, "1112")]
        [InlineData(typeof(NullableClass18<>), "Field6", null, "112")]
        [InlineData(typeof(NullableClass18<>), "Field7", null, "1012")]
        [InlineData(typeof(NullableClass18<>), "Field8", null, "1012")]
        [InlineData(typeof(NullableClass18<>), "Field9", null, "102")]
        [InlineData(typeof(NullableClass18<>), "Field10", null, "112")]
        [InlineData(typeof(NullableClass21), "Field1", null, null)]
        [InlineData(typeof(NullableClass21), "Field2", null, null)]
        [InlineData(typeof(NullableClass21), "Field3", null, null)]
        [InlineData(typeof(NullableClass21), "Field4", null, null)]
        [InlineData(typeof(NullableClass21), "Field5", null, null)]
        [InlineData(typeof(NullableClass21), "Field6", null, null)]
        [InlineData(typeof(NullableClass21), "Field7", null, null)]
        [InlineData(typeof(NullableClass21), "Field8", null, null)]
        [InlineData(typeof(NullableClass21), "Field9", null, null)]
        [InlineData(typeof(NullableClass21), "Field10", null, null)]
        [InlineData(typeof(NullableClass21), "Field11", null, null)]
        [InlineData(typeof(NullableClass21), "Field12", null, null)]
        [InlineData(typeof(NullableClass21), "Field13", null, null)]
        [InlineData(typeof(NullableClass21), "Field14", null, null)]
        [InlineData(typeof(NullableClass21), "Field15", null, null)]
        [InlineData(typeof(NullableClass21), "Field16", null, null)]
        [InlineData(typeof(NullableClass21), "Field17", null, null)]
        [InlineData(typeof(NullableClass21), "Field18", null, null)]
        [InlineData(typeof(NullableClass21), "Field19", null, null)]
        [InlineData(typeof(NullableClass21), "Field20", null, null)]
        [InlineData(typeof(NullableClass21), "Field21", null, null)]
        [InlineData(typeof(NullableClass21), "Field22", null, null)]
        [InlineData(typeof(NullableClass21), "Field23", null, null)]
        [InlineData(typeof(NullableClass21), "Field24", null, null)]
        public void VerifyTestMethod(Type type, string methodName, int? nullableContext, string nullableValues)
        {
            var method = type.GetMethod(methodName);
            var methodNullableAttribute = method.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "NullableAttribute");
            methodNullableAttribute.ShouldBeNull(); //should not be possible to apply the attribute here

            var methodNullableContextAttribute = method.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "NullableContextAttribute");
            if (nullableContext.HasValue)
            {
                methodNullableContextAttribute.ShouldNotBeNull();
                methodNullableContextAttribute.ConstructorArguments[0].ArgumentType.ShouldBe(typeof(byte));
                methodNullableContextAttribute.ConstructorArguments[0].Value.ShouldBeOfType<byte>().ShouldBe((byte)nullableContext.Value);
            }
            else
            {
                methodNullableContextAttribute.ShouldBeNull();
            }

            var parameterNullableAttribute = method.ReturnParameter.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "NullableAttribute");
            if (nullableValues != null)
            {
                parameterNullableAttribute.ShouldNotBeNull();
                var expectedValues = nullableValues.Select(x => (byte)int.Parse(x.ToString())).ToArray();
                if (expectedValues.Length == 1)
                {
                    parameterNullableAttribute.ConstructorArguments[0].ArgumentType.ShouldBe(typeof(byte));
                    var actualValue = parameterNullableAttribute.ConstructorArguments[0].Value.ShouldBeOfType<byte>().ToString();
                    actualValue.ShouldBe(nullableValues);
                }
                else
                {
                    parameterNullableAttribute.ConstructorArguments[0].ArgumentType.ShouldBe(typeof(byte[]));
                    var actualValues = string.Join("", parameterNullableAttribute.ConstructorArguments[0].Value.ShouldBeOfType<ReadOnlyCollection<CustomAttributeTypedArgument>>().Select(x => x.Value.ToString()));
                    actualValues.ShouldBe(nullableValues);
                }
            }
            else
            {
                parameterNullableAttribute.ShouldBeNull();
            }
        }

        [Theory]
        [InlineData(typeof(NullableClass20), "Field1", null)]
        [InlineData(typeof(NullableClass20), "Field2", null)]
        [InlineData(typeof(NullableClass20), "Field3", "2")]
        [InlineData(typeof(NullableClass20), "Field4", "12")]
        [InlineData(typeof(NullableClass20), "Field5", null)]
        [InlineData(typeof(NullableClass20), "Field6", null)]
        public void VerifyTestProperty(Type type, string propertyName, string nullableValues)
        {
            var property = type.GetProperty(propertyName);
            var parameterNullableAttribute = property.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "NullableAttribute");
            if (nullableValues != null)
            {
                parameterNullableAttribute.ShouldNotBeNull();
                var expectedValues = nullableValues.Select(x => (byte)int.Parse(x.ToString())).ToArray();
                if (expectedValues.Length == 1)
                {
                    parameterNullableAttribute.ConstructorArguments[0].ArgumentType.ShouldBe(typeof(byte));
                    var actualValue = parameterNullableAttribute.ConstructorArguments[0].Value.ShouldBeOfType<byte>().ToString();
                    actualValue.ShouldBe(nullableValues);
                }
                else
                {
                    parameterNullableAttribute.ConstructorArguments[0].ArgumentType.ShouldBe(typeof(byte[]));
                    var actualValues = string.Join("", parameterNullableAttribute.ConstructorArguments[0].Value.ShouldBeOfType<ReadOnlyCollection<CustomAttributeTypedArgument>>().Select(x => x.Value.ToString()));
                    actualValues.ShouldBe(nullableValues);
                }
            }
            else
            {
                parameterNullableAttribute.ShouldBeNull();
            }
        }

        [Theory]
        [InlineData(typeof(NullableClass9), "Field1", "arg1", null)]
        [InlineData(typeof(NullableClass9), "Field1", "arg2", null)]
        [InlineData(typeof(NullableClass10), "Field1", "arg1", null)]
        [InlineData(typeof(NullableClass10), "Field1", "arg2", null)]
        [InlineData(typeof(NullableClass11), "Field2", "arg1", null)]
        [InlineData(typeof(NullableClass11), "Field2", "arg2", null)]
        [InlineData(typeof(NullableClass13), "Field2", "arg1", null)]
        [InlineData(typeof(NullableClass13), "Field2", "arg2", "2")]
        [InlineData(typeof(NullableClass13), "Field2", "arg3", null)]
        [InlineData(typeof(NullableClass13), "Field2", "arg4", null)]
        [InlineData(typeof(NullableClass13), "Field2", "arg5", null)]
        [InlineData(typeof(NullableClass13), "Field2", "arg6", null)]
        [InlineData(typeof(NullableClass13), "Field2", "arg7", "12")]
        [InlineData(typeof(NullableClass13), "Field2", "arg8", "21")]
        [InlineData(typeof(NullableClass13), "Field2", "arg9", "2")]
        [InlineData(typeof(NullableClass14), "Field2", "arg1", null)]
        [InlineData(typeof(NullableClass14), "Field2", "arg2", "1")]
        [InlineData(typeof(NullableClass14), "Field2", "arg3", null)]
        [InlineData(typeof(NullableClass14), "Field2", "arg4", null)]
        [InlineData(typeof(NullableClass14), "Field2", "arg5", null)]
        [InlineData(typeof(NullableClass14), "Field2", "arg6", "1")]
        [InlineData(typeof(NullableClass14), "Field2", "arg7", "12")]
        [InlineData(typeof(NullableClass14), "Field2", "arg8", "21")]
        [InlineData(typeof(NullableClass14), "Field2", "arg9", null)]
        public void VerifyTestArgument(Type type, string methodName, string argumentName, string nullableValues)
        {
            var method = type.GetMethod(methodName);
            var argument = method.GetParameters().Single(x => x.Name == argumentName);
            var parameterNullableAttribute = argument.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "NullableAttribute");
            if (nullableValues != null)
            {
                parameterNullableAttribute.ShouldNotBeNull();
                var expectedValues = nullableValues.Select(x => (byte)int.Parse(x.ToString())).ToArray();
                if (expectedValues.Length == 1)
                {
                    parameterNullableAttribute.ConstructorArguments[0].ArgumentType.ShouldBe(typeof(byte));
                    var actualValue = parameterNullableAttribute.ConstructorArguments[0].Value.ShouldBeOfType<byte>().ToString();
                    actualValue.ShouldBe(nullableValues);
                }
                else
                {
                    parameterNullableAttribute.ConstructorArguments[0].ArgumentType.ShouldBe(typeof(byte[]));
                    var actualValues = string.Join("", parameterNullableAttribute.ConstructorArguments[0].Value.ShouldBeOfType<ReadOnlyCollection<CustomAttributeTypedArgument>>().Select(x => x.Value.ToString()));
                    actualValues.ShouldBe(nullableValues);
                }
            }
            else
            {
                parameterNullableAttribute.ShouldBeNull();
            }
        }
    }
}
