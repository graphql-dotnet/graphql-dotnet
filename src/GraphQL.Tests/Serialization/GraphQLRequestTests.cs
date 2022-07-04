using GraphQL.Transport;

namespace GraphQL.Tests.Serialization;

/// <summary>
/// Tests for <see cref="IGraphQLTextSerializer"/> implementations and the custom converters
/// that are used in the process of serializing and deserializing an <see cref="GraphQLRequest"/> to JSON.
/// </summary>
public class GraphQLRequestTests : DeserializationTestBase
{
    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_GraphQLRequest_Correctly_Simple(IGraphQLTextSerializer serializer)
    {
        var request = new GraphQLRequest
        {
            Query = "hello",
        };

        var expected = @"{ ""query"": ""hello"" }";

        var actual = serializer.Serialize(request);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_GraphQLRequest_Correctly_Complex(IGraphQLTextSerializer serializer)
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

        var actual = serializer.Serialize(request);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_GraphQLRequest_Correctly_SampleData(IGraphQLTextSerializer serializer)
    {
        var request = new GraphQLRequest
        {
            Query = "hello",
            Variables = new Inputs(new Dictionary<string, object>
            {
                { "arg", ExampleData },
            }),
        };

        var expected = $"{{ \"query\": \"hello\", \"variables\": {{ \"arg\": {ExampleJson} }} }}";

        var actual = serializer.Serialize(request);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_GraphQLRequest_List_Correctly(IGraphQLTextSerializer serializer)
    {
        var request = new GraphQLRequest
        {
            Query = "hello",
        };

        var expected = @"[{ ""query"": ""hello"" }]";

        var actual = serializer.Serialize(new List<GraphQLRequest> { request });

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_GraphQLRequest_Array_Correctly(IGraphQLTextSerializer serializer)
    {
        var request = new GraphQLRequest
        {
            Query = "hello",
        };

        var expected = @"[{ ""query"": ""hello"" }]";

        var actual = serializer.Serialize(new GraphQLRequest[] { request });

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest(IGraphQLTextSerializer serializer)
    {
        var test = $"{{\"query\":\"hello\",\"variables\":{ExampleJson}}}";
        var actual = serializer.Deserialize<GraphQLRequest>(test);
        actual.Query.ShouldBe("hello");
        Verify(actual.Variables);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_IsCaseSensitive(IGraphQLTextSerializer serializer)
    {
        var test = $"{{\"query\":\"hello\",\"Variables\":{ExampleJson}}}";
        var actual = serializer.Deserialize<GraphQLRequest>(test);
        actual.Query.ShouldBe("hello");
        actual.Variables.ShouldBeNull();
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_List(IGraphQLTextSerializer serializer)
        => Reads_GraphQLRequest_Test<List<GraphQLRequest>>(serializer);

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_Enumerable(IGraphQLTextSerializer serializer)
        => Reads_GraphQLRequest_Test<IEnumerable<GraphQLRequest>>(serializer);

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_Array(IGraphQLTextSerializer serializer)
        => Reads_GraphQLRequest_Test<GraphQLRequest[]>(serializer);

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_IList(IGraphQLTextSerializer serializer)
        => Reads_GraphQLRequest_Test<IList<GraphQLRequest>>(serializer);

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_IReadOnlyList(IGraphQLTextSerializer serializer)
        => Reads_GraphQLRequest_Test<IReadOnlyList<GraphQLRequest>>(serializer);

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_ICollection(IGraphQLTextSerializer serializer)
        => Reads_GraphQLRequest_Test<ICollection<GraphQLRequest>>(serializer);

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_IReadOnlyCollection(IGraphQLTextSerializer serializer)
        => Reads_GraphQLRequest_Test<IReadOnlyCollection<GraphQLRequest>>(serializer);

    private void Reads_GraphQLRequest_Test<T>(IGraphQLTextSerializer serializer)
        where T : IEnumerable<GraphQLRequest>
    {
        var test = $"[{{\"query\":\"hello\",\"variables\":{ExampleJson}}}, {{\"query\":\"dummy\"}}]";
        var actual = serializer.Deserialize<T>(test);
        actual.Count().ShouldBe(2);
        var request = actual.First();
        request.Query.ShouldBe("hello");
        Verify(request.Variables);
        var request2 = actual.Last();
        request2.Query.ShouldBe("dummy");
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_Null_GraphQLRequest_In_List(IGraphQLTextSerializer serializer)
    {
        var actual = serializer.Deserialize<GraphQLRequest[]>("[null]");
        actual.Length.ShouldBe(1);
        actual.First().ShouldBeNull();
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void BatchRequestAsIListIsNotArrayForListOfSingle(IGraphQLTextSerializer serializer)
    {
        // verifies that when deserializing to IList<GraphQLRequest>, and when a single item is in the list, the result is not a GraphQLRequest[]
        // note: the server counts on this behavior to determine whether or not a request is a batch request
        var actual = serializer.Deserialize<IList<GraphQLRequest>>("[{}]");
        actual.ShouldNotBeOfType<GraphQLRequest[]>();
        actual.Count.ShouldBe(1);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void BatchRequestAsIListIsArrayForSingle(IGraphQLTextSerializer serializer)
    {
        // verifies that when deserializing to IList<GraphQLRequest>, and when it is not a batch request, the result is a GraphQLRequest[1]
        // note: the server counts on this behavior to determine whether or not a request is a batch request
        var actual = serializer.Deserialize<IList<GraphQLRequest>>("{}");
        actual.ShouldBeOfType<GraphQLRequest[]>();
        actual.Count.ShouldBe(1);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_List_NotCaseSensitive(IGraphQLTextSerializer serializer)
    {
        var test = @"{""VARIABLES"":{""date"":""2015-12-22T10:10:10+03:00""},""query"":""test""}";
        var actual = serializer.Deserialize<List<GraphQLRequest>>(test);
        actual.Count.ShouldBe(1);
        actual[0].Query.ShouldBe("test");
        actual[0].Variables.ShouldBeNull();
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_List_Multiple_Items(IGraphQLTextSerializer serializer)
    {
        var test = $"[{{\"query\":\"hello\",\"variables\":{ExampleJson}}},{{\"query\":\"hello2\"}}]";
        var actual = serializer.Deserialize<List<GraphQLRequest>>(test);
        actual.Count.ShouldBe(2);
        actual[0].Query.ShouldBe("hello");
        Verify(actual[0].Variables);
        actual[1].Query.ShouldBe("hello2");
        actual[1].Variables.ShouldBeNull();
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_List_No_Items(IGraphQLTextSerializer serializer)
    {
        var test = $"[]";
        var actual = serializer.Deserialize<List<GraphQLRequest>>(test);
        actual.Count.ShouldBe(0);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_List_Reads_Single_Item(IGraphQLTextSerializer serializer)
    {
        var test = $"{{\"query\":\"hello\",\"variables\":{ExampleJson}}}";
        var actual = serializer.Deserialize<List<GraphQLRequest>>(test);
        var request = actual.ShouldHaveSingleItem();
        request.Query.ShouldBe("hello");
        Verify(request.Variables);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_Nulls(IGraphQLTextSerializer serializer)
    {
        var test = $"{{\"query\":null,\"operationName\":null,\"variables\":null,\"extensions\":null}}";
        var actual = serializer.Deserialize<GraphQLRequest>(test);
        actual.Query.ShouldBeNull();
        actual.OperationName.ShouldBeNull();
        actual.Variables.ShouldBeNull();
        actual.Extensions.ShouldBeNull();
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_Empty(IGraphQLTextSerializer serializer)
    {
        var test = $"{{}}";
        var actual = serializer.Deserialize<GraphQLRequest>(test);
        actual.Query.ShouldBeNull();
        actual.OperationName.ShouldBeNull();
        actual.Variables.ShouldBeNull();
        actual.Extensions.ShouldBeNull();
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Reads_GraphQLRequest_Other_Properties(IGraphQLTextSerializer serializer)
    {
        var test = $"{{\"query\":\"hello\",\"operationName\":\"hello2\",\"extensions\":{ExampleJson}}}";
        var actual = serializer.Deserialize<GraphQLRequest>(test);
        actual.Query.ShouldBe("hello");
        actual.OperationName.ShouldBe("hello2");
        Verify(actual.Extensions);
    }
}
