using System.Collections.Generic;
using System.Linq;
using GraphQL.Transports.Json;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    /// <summary>
    /// Tests for <see cref="IGraphQLTextSerializer"/> implementations and the custom converters
    /// that are used in the process of serializing an <see cref="ExecutionResult"/> to JSON.
    /// </summary>
    public class GraphQLSerializerTests
    {
        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Can_Write_Execution_Result(IGraphQLTextSerializer writer)
        {
            var executionResult = new ExecutionResult
            {
                Executed = true,
                Data = @"{ ""someType"": { ""someProperty"": ""someValue"" } }".ToDictionary().ToExecutionTree(),
                Errors = new ExecutionErrors
                {
                    new ExecutionError("some error 1"),
                    new ExecutionError("some error 2"),
                },
                Extensions = new Dictionary<string, object>
                {
                    { "someExtension", new { someProperty = "someValue", someOtherPropery = 1 } }
                }
            };

            var expected = @"{
              ""errors"": [
                {
                  ""message"": ""some error 1""
                },
                {
                  ""message"": ""some error 2""
                }
              ],
              ""data"": {
                ""someType"": {
                    ""someProperty"": ""someValue""
                }
              },
              ""extensions"": {
                ""someExtension"": {
                  ""someProperty"": ""someValue"",
                  ""someOtherPropery"": 1
                }
              }
            }";

            var actual = writer.Serialize(executionResult);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_Correct_Execution_Result_With_Null_Data_And_Null_Errors(IGraphQLTextSerializer writer)
        {
            var executionResult = new ExecutionResult { Executed = true };

            var expected = @"{
              ""data"": null
            }";

            var actual = writer.Serialize(executionResult);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_Correct_Execution_Result_With_Null_Data_And_Some_Errors(IGraphQLTextSerializer writer)
        {
            // "If an error was encountered before execution begins, the data entry should not be present in the result."
            // Source: https://github.com/graphql/graphql-spec/blob/master/spec/Section%207%20--%20Response.md#data

            var executionResult = new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError("some error 1"),
                    new ExecutionError("some error 2"),
                }
            };

            var expected = @"{
              ""errors"": [{""message"":""some error 1""},{""message"":""some error 2""}]
            }";

            var actual = writer.Serialize(executionResult);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_Correct_Execution_Result_With_Empty_Data_Errors_And_Extensions_When_Executed(IGraphQLTextSerializer writer)
        {
            var executionResult = new ExecutionResult
            {
                Data = new Dictionary<string, object>().ToExecutionTree(),
                Errors = new ExecutionErrors(),
                Extensions = new Dictionary<string, object>(),
                Executed = true
            };

            var expected = @"{ ""data"": {} }";

            var actual = writer.Serialize(executionResult);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_Correct_Execution_Result_With_Empty_Data_Errors_And_Extensions_When_Not_Executed(IGraphQLTextSerializer writer)
        {
            var executionResult = new ExecutionResult
            {
                Data = new Dictionary<string, object>().ToExecutionTree(),
                Errors = new ExecutionErrors(),
                Extensions = new Dictionary<string, object>(),
                Executed = false
            };

            var expected = @"{ }";

            var actual = writer.Serialize(executionResult);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_Path_Property_Correctly(IGraphQLTextSerializer writer)
        {
            var executionResult = new ExecutionResult
            {
                Data = null,
                Errors = new ExecutionErrors(),
                Extensions = null,
            };
            var executionError = new ExecutionError("Error testing index")
            {
                Path = new object[] { "parent", 23, "child" }
            };
            executionResult.Errors.Add(executionError);

            var expected = @"{ ""errors"": [{ ""message"": ""Error testing index"", ""path"": [ ""parent"", 23, ""child"" ] }] }";

            var actual = writer.Serialize(executionResult);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_GraphQLRequest_Correctly_Simple(IGraphQLTextSerializer writer)
        {
            var request = new GraphQLRequest
            {
                Query = "hello",
            };

            var expected = @"{ ""query"": ""hello"" }";

            var actual = writer.Serialize(request);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_GraphQLRequest_Correctly_Complex(IGraphQLTextSerializer writer)
        {
            var request = new GraphQLRequest
            {
                Query = "hello",
                OperationName = "opname",
                Variables = new Inputs(new Dictionary<string, object>
                {
                    { "arg1", 1 },
                    { "arg2", "test" },
                }),
                Extensions = new Inputs(new Dictionary<string, object>
                {
                    { "arg1", 2 },
                    { "arg2", "test2" },
                }),
            };

            var expected = @"{ ""query"": ""hello"", ""operationName"": ""opname"", ""variables"": { ""arg1"": 1, ""arg2"": ""test"" }, ""extensions"": { ""arg1"": 2, ""arg2"": ""test2"" } }";

            var actual = writer.Serialize(request);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_GraphQLRequest_List_Correctly(IGraphQLTextSerializer writer)
        {
            var request = new GraphQLRequest
            {
                Query = "hello",
            };

            var expected = @"[{ ""query"": ""hello"" }]";

            var actual = writer.Serialize(new List<GraphQLRequest> { request });

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_GraphQLRequest_Array_Correctly(IGraphQLTextSerializer writer)
        {
            var request = new GraphQLRequest
            {
                Query = "hello",
            };

            var expected = @"[{ ""query"": ""hello"" }]";

            var actual = writer.Serialize(new GraphQLRequest[] { request });

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Can_Read_GraphQLRequest(IGraphQLTextSerializer reader)
        {
            var sample = @"
{
  ""query"": ""test"",
  ""operationName"": ""hello"",
  ""variables"": { ""int"": 1, ""str"": ""value"" },
  ""extensions"": { ""int"": 2, ""str"": ""VALUE"" }
}";

            var result = reader.Deserialize<GraphQLRequest>(sample);
            result.Query.ShouldBe("test");
            result.OperationName.ShouldBe("hello");
            result.Variables.ShouldBe<IDictionary<string, object>>(new Dictionary<string, object>()
            {
                { "int", 1 },
                { "str", "value" }
            });
            result.Extensions.ShouldBe<IDictionary<string, object>>(new Dictionary<string, object>()
            {
                { "int", 2 },
                { "str", "VALUE" }
            });
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Can_Read_GraphQLRequest_Simple(IGraphQLTextSerializer reader)
        {
            var sample = @"
{
  ""query"": ""test""
}";

            var result = reader.Deserialize<GraphQLRequest>(sample);
            result.Query.ShouldBe("test");
            result.OperationName.ShouldBeNull();
            result.Variables.ShouldBeNull();
            result.Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Can_Read_GraphQLRequest_List_Single(IGraphQLTextSerializer reader)
        {
            var sample = @"
{
  ""query"": ""test""
}";

            var result = reader.Deserialize<List<GraphQLRequest>>(sample).ShouldHaveSingleItem();
            result.Query.ShouldBe("test");
            result.OperationName.ShouldBeNull();
            result.Variables.ShouldBeNull();
            result.Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Can_Read_GraphQLRequest_List_Multiple(IGraphQLTextSerializer reader)
        {
            var sample = @"
[
  {
    ""query"": ""test""
  },
  {
    ""query"": ""test2""
  }
]";

            var result = reader.Deserialize<List<GraphQLRequest>>(sample);
            result.Count.ShouldBe(2);
            result[0].Query.ShouldBe("test");
            result[0].OperationName.ShouldBeNull();
            result[0].Variables.ShouldBeNull();
            result[0].Extensions.ShouldBeNull();
            result[1].Query.ShouldBe("test2");
            result[1].OperationName.ShouldBeNull();
            result[1].Variables.ShouldBeNull();
            result[1].Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Can_Read_GraphQLRequest_Array_Single(IGraphQLTextSerializer reader)
        {
            var sample = @"
{
  ""query"": ""test""
}";

            var result = reader.Deserialize<GraphQLRequest[]>(sample).ShouldHaveSingleItem();
            result.Query.ShouldBe("test");
            result.OperationName.ShouldBeNull();
            result.Variables.ShouldBeNull();
            result.Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Can_Read_GraphQLRequest_Array_Multiple(IGraphQLTextSerializer reader)
        {
            var sample = @"
[
  {
    ""query"": ""test""
  },
  {
    ""query"": ""test2""
  }
]";

            var result = reader.Deserialize<GraphQLRequest[]>(sample);
            result.Length.ShouldBe(2);
            result[0].Query.ShouldBe("test");
            result[0].OperationName.ShouldBeNull();
            result[0].Variables.ShouldBeNull();
            result[0].Extensions.ShouldBeNull();
            result[1].Query.ShouldBe("test2");
            result[1].OperationName.ShouldBeNull();
            result[1].Variables.ShouldBeNull();
            result[1].Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Can_Read_GraphQLRequest_Enumerable_Single(IGraphQLTextSerializer reader)
        {
            var sample = @"
{
  ""query"": ""test""
}";

            var result = reader.Deserialize<IEnumerable<GraphQLRequest>>(sample).ShouldHaveSingleItem();
            result.Query.ShouldBe("test");
            result.OperationName.ShouldBeNull();
            result.Variables.ShouldBeNull();
            result.Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Can_Read_GraphQLRequest_Enumerable_Multiple(IGraphQLTextSerializer reader)
        {
            var sample = @"
[
  {
    ""query"": ""test""
  },
  {
    ""query"": ""test2""
  }
]";

            var result = reader.Deserialize<IEnumerable<GraphQLRequest>>(sample).ToList();
            result.Count.ShouldBe(2);
            result[0].Query.ShouldBe("test");
            result[0].OperationName.ShouldBeNull();
            result[0].Variables.ShouldBeNull();
            result[0].Extensions.ShouldBeNull();
            result[1].Query.ShouldBe("test2");
            result[1].OperationName.ShouldBeNull();
            result[1].Variables.ShouldBeNull();
            result[1].Extensions.ShouldBeNull();
        }
    }
}
