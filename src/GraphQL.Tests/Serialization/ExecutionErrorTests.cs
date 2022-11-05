namespace GraphQL.Tests.Serialization;

/// <summary>
/// Tests for <see cref="IGraphQLTextSerializer"/> implementations and the custom converters
/// that are used in the process of serializing an <see cref="ExecutionError"/> to JSON.
/// </summary>
public class ExecutionErrorTests
{
    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Simple(IGraphQLTextSerializer serializer)
    {
        var error = new ExecutionError("some error 1");

        var expected = @"{""message"": ""some error 1""}";

        var actual = serializer.Serialize(error);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Null(IGraphQLTextSerializer serializer)
    {
        var expected = "null";

        var actual = serializer.Serialize<ExecutionError>(null);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Array(IGraphQLTextSerializer serializer)
    {
        var errors = new ExecutionError[] { new ExecutionError("some error 1"), new ExecutionError("some error 2") };

        var expected = @"[{""message"": ""some error 1""}, {""message"": ""some error 2""}]";

        var actual = serializer.Serialize(errors);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_Path_Property_Correctly(IGraphQLTextSerializer serializer)
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

        var actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }
}
