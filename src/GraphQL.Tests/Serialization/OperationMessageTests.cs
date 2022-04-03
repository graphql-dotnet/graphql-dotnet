using GraphQL.Transport;

namespace GraphQL.Tests.Serialization;

public class OperationMessageTests : DeserializationTestBase
{
    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_OperationMessage_Populated(IGraphQLTextSerializer serializer)
    {
        var test = $"{{\"id\":\"hello\",\"type\":\"hello2\",\"payload\":{{\"query\":\"hello3\",\"variables\":{ExampleJson}}}}}";
        var actual = serializer.Deserialize<OperationMessage>(test);
        actual.Id.ShouldBe("hello");
        actual.Type.ShouldBe("hello2");
        actual.Payload.ShouldNotBeNull();
        var request = serializer.ReadNode<GraphQLRequest>(actual.Payload);
        request.Query.ShouldBe("hello3");
        Verify(request.Variables);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_OperationMessage_Nulls(IGraphQLTextSerializer serializer)
    {
        var test = $"{{\"id\":null,\"type\":null,\"payload\":null}}";
        var actual = serializer.Deserialize<OperationMessage>(test);
        actual.Id.ShouldBeNull();
        actual.Type.ShouldBeNull();
        actual.Payload.ShouldBeNull();
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_OperationMessage_Empty(IGraphQLTextSerializer serializer)
    {
        var test = $"{{}}";
        var actual = serializer.Deserialize<OperationMessage>(test);
        actual.Id.ShouldBeNull();
        actual.Type.ShouldBeNull();
        actual.Payload.ShouldBeNull();
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_OperationMessage_Populated(IGraphQLTextSerializer serializer)
    {
        var message = new OperationMessage
        {
            Id = "hello",
            Type = "hello2",
            Payload = new GraphQLRequest
            {
                Query = "hello3",
                Variables = new Inputs(new Dictionary<string, object> {
                    { "arg", ExampleData },
                }),
            }
        };
        var actual = serializer.Serialize(message);
        var expected = $"{{\"type\":\"hello2\",\"id\":\"hello\",\"payload\":{{\"query\":\"hello3\",\"variables\":{{\"arg\":{ExampleJson}}}}}}}";
        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_OperationMessage_Nulls(IGraphQLTextSerializer serializer)
    {
        var message = new OperationMessage();
        var actual = serializer.Serialize(message);
        var expected = @"{}";
        actual.ShouldBeCrossPlatJson(expected);
    }
}
