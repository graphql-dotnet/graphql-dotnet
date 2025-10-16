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

        const string expected = """{"message": "some error 1"}""";

        string actual = serializer.Serialize(error);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Extensions(IGraphQLTextSerializer serializer)
    {
        var error = new ExecutionError("some error 1")
            .AddExtension("severity", "warn")
            .AddExtension("rank", 42);

        const string expected = """
{
  "message": "some error 1",
  "extensions": {
    "severity": "warn",
    "rank": 42
  }
}
""";

        string actual = serializer.Serialize(error);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Null(IGraphQLTextSerializer serializer)
    {
        const string expected = "null";

        string actual = serializer.Serialize<ExecutionError>(null);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Array(IGraphQLTextSerializer serializer)
    {
        var errors = new ExecutionError[] { new ExecutionError("some error 1"), new ExecutionError("some error 2") };

        const string expected = """[{"message": "some error 1"}, {"message": "some error 2"}]""";

        string actual = serializer.Serialize(errors);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_Path_Property_Correctly(IGraphQLTextSerializer serializer)
    {
        var executionResult = new ExecutionResult
        {
            Data = null,
            Errors = [],
            Extensions = null,
        };
        var executionError = new ExecutionError("Error testing index")
        {
            Path = ["parent", 23, "child"]
        };
        executionResult.Errors.Add(executionError);

        const string expected = """{ "errors": [{ "message": "Error testing index", "path": [ "parent", 23, "child" ] }] }""";

        string actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }
}
