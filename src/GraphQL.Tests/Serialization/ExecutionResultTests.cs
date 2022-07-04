using GraphQL.Tests.StarWars;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Serialization;

/// <summary>
/// Tests for <see cref="IGraphQLTextSerializer"/> implementations and the custom converters
/// that are used in the process of serializing an <see cref="ExecutionResult"/> to JSON.
/// </summary>
public class ExecutionResultTests
{
    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Can_Write_Execution_Result(IGraphQLTextSerializer serializer)
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

        var actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_Correct_Execution_Result_With_Null_Data_And_Null_Errors(IGraphQLTextSerializer serializer)
    {
        var executionResult = new ExecutionResult { Executed = true };

        var expected = @"{
              ""data"": null
            }";

        var actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_Correct_Execution_Result_With_Null_Data_And_Some_Errors(IGraphQLTextSerializer serializer)
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

        var actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_Correct_Execution_Result_With_Empty_Data_Errors_And_Extensions_When_Executed(IGraphQLTextSerializer serializer)
    {
        var executionResult = new ExecutionResult
        {
            Data = new Dictionary<string, object>().ToExecutionTree(),
            Errors = new ExecutionErrors(),
            Extensions = new Dictionary<string, object>(),
            Executed = true
        };

        var expected = @"{ ""data"": {} }";

        var actual = serializer.Serialize(executionResult);

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
    public void Writes_Correct_Execution_Result_With_Null_Data_Errors_And_Extensions_When_Executed(IGraphQLTextSerializer serializer)
    {
        var executionResult = new ExecutionResult
        {
            Data = null,
            Errors = new ExecutionErrors(),
            Extensions = new Dictionary<string, object>(),
            Executed = true
        };

        var expected = @"{ ""data"": null }";

        var actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public async Task Synchronous_and_Async_Works_Same(IGraphQLTextSerializer serializer)
    {
        //ISSUE: manually created test instance with ServiceProvider
        var builder = new MicrosoftDI.GraphQLBuilder(new ServiceCollection(), b => new StarWarsTestBase().RegisterServices(b.Services));
        var schema = new GraphQL.StarWars.StarWarsSchema(builder.ServiceCollection.BuildServiceProvider());
        var result = await new DocumentExecuter().ExecuteAsync(new ExecutionOptions
        {
            Schema = schema,
            Query = "IntrospectionQuery".ReadGraphQLRequest()
        }).ConfigureAwait(false);
        var syncResult = serializer.Serialize(result);
        var stream = new System.IO.MemoryStream();
        await serializer.WriteAsync(stream, result).ConfigureAwait(false);
        var asyncResult = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        syncResult.ShouldBe(asyncResult);
    }
}
