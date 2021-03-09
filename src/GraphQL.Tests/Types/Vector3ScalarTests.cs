using System;
using System.Collections.Generic;
using GraphQL.Language.AST;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Types
{
    // these tests relate to the code in custom-scalars.md
    public class Vector3ScalarTests : QueryTestBase<Vector3ScalarTests.Vector3ScalarSchema>
    {
        [Fact]
        public void test_parseliteral()
        {
            AssertQuerySuccess(@"{ input(arg: ""1,2,3"") }", @"{ ""input"": ""=1=2=3="" }");
        }

        [Fact]
        public void test_parsevalue_string()
        {
            AssertQuerySuccess(@"query($arg: Vector3) { input(arg: $arg) }", @"{ ""input"": ""=1=2=3="" }", @"{ ""arg"": ""1,2,3"" }".ToInputs());
        }

        [Fact]
        public void test_parsevalue_structured()
        {
            AssertQuerySuccess(@"query($arg: Vector3) { input(arg: $arg) }", @"{ ""input"": ""=1=2=3="" }", @"{ ""arg"": { ""x"": 1, ""y"": 2, ""z"": 3 } }".ToInputs());
        }

        [Fact]
        public void test_default()
        {
            AssertQuerySuccess(@"{ input }", @"{ ""input"": ""=7=8=9="" }");
        }

        [Fact]
        public void test_output()
        {
            AssertQuerySuccess(@"{ output }", @"{ ""output"": { ""x"": 4, ""y"": 5, ""z"": 6 } }");
        }

        [Fact]
        public void test_loopback_with_value()
        {
            AssertQuerySuccess(@"{ loopback(arg: ""11,12,13"") }", @"{ ""loopback"": { ""x"": 11, ""y"": 12, ""z"": 13 } }");
        }

        [Fact]
        public void test_loopback_with_null()
        {
            AssertQuerySuccess(@"{ loopback }", @"{ ""loopback"": null }");
        }

        public struct Vector3
        {
            public Vector3(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public override string ToString()
            {
                return $"={X}={Y}={Z}=";
            }
        }

        public class Vector3ScalarSchema : Schema
        {
            public Vector3ScalarSchema()
            {
                Query = new Vector3ScalarQuery();
            }
        }

        public class Vector3ScalarQuery : ObjectGraphType
        {
            public Vector3ScalarQuery()
            {
                Field(typeof(StringGraphType), "input",
                    arguments: new QueryArguments
                    {
                        new QueryArgument<Vector3Type> { Name = "arg", DefaultValue = new Vector3(7, 8, 9) }
                    },
                    resolve: context => context.GetArgument<Vector3?>("arg")?.ToString());

                Field(typeof(Vector3Type), "output",
                    resolve: context => new Vector3(4, 5, 6));

                Field(typeof(Vector3Type), "loopback",
                    arguments: new QueryArguments
                    {
                        new QueryArgument<Vector3Type> { Name = "arg" }
                    },
                    resolve: context => context.GetArgument<Vector3?>("arg"));
            }
        }

        public class Vector3Type : ScalarGraphType
        {
            public Vector3Type()
            {
                Name = "Vector3";
            }

            public override object ParseValue(object value)
            {
                if (value == null)
                    return null;

                if (value is string vector3InputString)
                {
                    try
                    {
                        var vector3Parts = vector3InputString.Split(',');
                        var x = float.Parse(vector3Parts[0]);
                        var y = float.Parse(vector3Parts[1]);
                        var z = float.Parse(vector3Parts[2]);
                        return new Vector3(x, y, z);
                    }
                    catch
                    {
                        throw new FormatException($"Failed to parse {nameof(Vector3)} from input '{vector3InputString}'. Input should be a string of three comma-separated floats in X Y Z order, ex. 1.0,2.0,3.0");
                    }
                }

                if (value is IDictionary<string, object> dictionary)
                {
                    try
                    {
                        var x = Convert.ToSingle(dictionary["x"]);
                        var y = Convert.ToSingle(dictionary["y"]);
                        var z = Convert.ToSingle(dictionary["z"]);
                        if (dictionary.Count > 3)
                            return ThrowValueConversionError(value);
                        return new Vector3(x, y, z);
                    }
                    catch
                    {
                        throw new FormatException($"Failed to parse {nameof(Vector3)} from object. Input should be an object of three floats named X Y and Z");
                    }
                }

                return ThrowValueConversionError(value);
            }

            public override object ParseLiteral(IValue value)
            {
                if (value is NullValue)
                    return null;

                if (value is StringValue stringValue)
                    return ParseValue(stringValue.Value);

                return ThrowLiteralConversionError(value);
            }

            public override object Serialize(object value)
            {
                if (value == null)
                    return null;

                if (value is Vector3 vector3)
                    return vector3;

                return ThrowSerializationError(value);
            }

            public override IValue ToAST(object value)
            {
                if (value == null)
                    return new NullValue();

                if (value is Vector3 vector3)
                {
                    return new StringValue($"{vector3.X},{vector3.Y},{vector3.Z}");
                }

                return ThrowASTConversionError(value);
            }
        }
    }
}
