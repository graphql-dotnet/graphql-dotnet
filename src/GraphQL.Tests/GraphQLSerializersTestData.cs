using System.Collections;
using Xunit.Abstractions;

namespace GraphQL.Tests;

public class GraphQLSerializersTestData : IEnumerable<object[]>
{
    // See https://github.com/xunit/xunit/issues/1473
    // Without this wrapper test output is cluttered with tons of xUnit warnings like that:
    // GraphQL.Tests: Non-serializable data ('System.Object[]') found for 'GraphQL.Tests.Extensions.GraphQLExtensionsTests.ToAST_Test'; falling back to single test case.
    // Also see TheoryExAttribute and TheoryExDiscoverer
    private class Wrapper : IGraphQLTextSerializer, IXunitSerializable
    {
        private IGraphQLTextSerializer _serializer = null!;

        public Wrapper()
        {
        }

        public Wrapper(IGraphQLTextSerializer serializer)
        {
            _serializer = serializer;
        }

        public string _Type => _serializer!.GetType().Namespace!.Split('.').Last();
        public bool IsNativelyAsync => _serializer.IsNativelyAsync;
        public T? Deserialize<T>(string? value) => _serializer.Deserialize<T>(value);
        public ValueTask<T?> ReadAsync<T>(Stream stream, CancellationToken cancellationToken = default) => _serializer.ReadAsync<T>(stream, cancellationToken);
        public T? ReadNode<T>(object? value) => _serializer.ReadNode<T>(value);
        public string Serialize<T>(T? value) => _serializer.Serialize<T>(value);
        public Task WriteAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default) => _serializer.WriteAsync<T>(stream, value, cancellationToken);

        public void Deserialize(IXunitSerializationInfo info)
        {
            string type = info.GetValue<string>("type");

            if (type == "GraphQL.NewtonsoftJson.GraphQLSerializer")
                _serializer = CreateNSJ();
            else if (type == "GraphQL.SystemTextJson.GraphQLSerializer")
                _serializer = CreateSTJ();
#if NET8_0_OR_GREATER
            else if (type == "GraphQL.Tests.MyStjAotSerializer")
                _serializer = CreateSTJAOT();
#endif
            else
                throw new NotSupportedException("Unknown serializer: " + type);
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("type", _serializer.GetType().FullName);
        }
    }

    public static readonly List<IGraphQLTextSerializer> AllWriters =
    [
        new Wrapper(CreateNSJ()),
        new Wrapper(CreateSTJ()),
#if NET8_0_OR_GREATER
        new Wrapper(CreateSTJAOT()),
#endif
    ];

    public static readonly List<IGraphQLTextSerializer> AllNonAotWriters =
    [
        new Wrapper(CreateNSJ()),
        new Wrapper(CreateSTJ()),
    ];

    private static IGraphQLTextSerializer CreateNSJ() => new NewtonsoftJson.GraphQLSerializer(settings =>
    {
        settings.Formatting = Newtonsoft.Json.Formatting.Indented;
        settings.Converters.Add(new NewtonsoftJson.FixPrecisionConverter(true, true, true));
    });

    private static IGraphQLTextSerializer CreateSTJ() => new SystemTextJson.GraphQLSerializer(new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // less strict about what is encoded into \uXXXX
    });

#if NET8_0_OR_GREATER
    private static IGraphQLTextSerializer CreateSTJAOT()
    {
        return new MyStjAotSerializer();
    }
#endif

    public IEnumerator<object[]> GetEnumerator()
    {
        foreach (var writer in AllWriters)
        {
            yield return new object[] { writer };
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class GraphQLSerializersNoAotTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        foreach (var writer in GraphQLSerializersTestData.AllNonAotWriters)
        {
            yield return new object[] { writer };
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

#if NET8_0_OR_GREATER
internal class MyStjAotSerializer : GraphQL.SystemTextJson.GraphQLAotSerializer
{
    public MyStjAotSerializer()
    {
        SerializerOptions.WriteIndented = true;
        SerializerOptions.PropertyNameCaseInsensitive = true;
        SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping; // less strict about what is encoded into \uXXXX
    }
}
#endif
