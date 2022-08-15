using GraphQL.Caching;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GraphQL.Tests.PersistedQueries;

public class AutomaticPersistedQueriesTests : IClassFixture<AutomaticPersistedQueriesFixture>
{
    private readonly AutomaticPersistedQueriesFixture _fixture;

    public AutomaticPersistedQueriesTests(AutomaticPersistedQueriesFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Configure_Action_Was_Applied_To_Options()
    {
        _fixture.Provider.GetRequiredService<IOptions<AutomaticPersistedQueriesCacheOptions>>().Value.SlidingExpiration.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Ordinary_Request_Should_Work()
    {
        var result = await _fixture.ExecuteAsync(opt => opt.Query = "query { ping }").ConfigureAwait(false);

        result.Errors.ShouldBeNull();
        _fixture.Serialize(result).ShouldBe(@"{""data"":{""ping"":""pong""}}");
    }

    private void AssertError(ExecutionResult result, string code, string message)
    {
        result.Errors.ShouldNotBeNull();
        var error = result.Errors.Single();
        error.Code.ShouldBe(code);
        error.Message.ShouldBe(message);
    }

    [Fact]
    public async Task Without_Query_And_Hash_Should_Throw_Error()
    {
        var result = await _fixture.ExecuteAsync().ConfigureAwait(false);
        AssertError(result, "QUERY_MISSING", "GraphQL query is missing.");
    }

    [Theory]
    [InlineData(2)]
    [InlineData("2")]
    public async Task Wrong_Version_Should_Be_Detected(object version)
    {
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = "1",
                ["version"] = version
            }
        });

        var result = await _fixture.ExecuteAsync(opt => opt.Extensions = extentions).ConfigureAwait(false);

        AssertError(result, "PERSISTED_QUERY_UNSUPPORTED_VERSION", "Automatic persisted queries protocol of version '2' is not supported.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData("1")]
    public async Task Not_Saved_Query_Should_Return_Not_Found_Code(object version)
    {
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = "1",
                ["version"] = version
            }
        });

        var result = await _fixture.ExecuteAsync(opt => opt.Extensions = extentions).ConfigureAwait(false);

        AssertError(result, "PERSISTED_QUERY_NOT_FOUND", "Persisted query with '1' hash was not found.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData("1")]
    public async Task Bad_Hash_Should_Be_Detected(object version)
    {
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = "badHash",
                ["version"] = version
            }
        });
        var result = await _fixture.ExecuteAsync(opt =>
        {
            opt.Query = "query { ping }";
            opt.Extensions = extentions;
        }).ConfigureAwait(false);

        AssertError(result, "PERSISTED_QUERY_BAD_HASH", "The 'badHash' hash doesn't correspond to a query.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData("1")]
    public async Task Persisted_Query_Should_Work(object version)
    {
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = "d7b0dfafc61a1f0618f4f346911d5aa87bef97b134f2943383223bdac4410134",
                ["version"] = version
            }
        });

        var result = await _fixture.ExecuteAsync(opt =>
        {
            opt.Query = "query { ping }";
            opt.Extensions = extentions;
        }).ConfigureAwait(false);
        result.Errors.ShouldBeNull();
        _fixture.Serialize(result).ShouldBe(@"{""data"":{""ping"":""pong""}}");

        result = await _fixture.ExecuteAsync(opt => opt.Extensions = extentions).ConfigureAwait(false);
        result.Errors.ShouldBeNull();
        _fixture.Serialize(result).ShouldBe(@"{""data"":{""ping"":""pong""}}");
    }
}

public class AutomaticPersistedQueriesFixture : IDisposable
{
    public class AutomaticPersistedQueriesTestSchema : Schema
    {
        public class AutomaticPersistedQueriesTestQuery : ObjectGraphType
        {
            public AutomaticPersistedQueriesTestQuery()
            {
                Field<StringGraphType>("ping").Resolve(_ => "pong");
            }
        }

        public AutomaticPersistedQueriesTestSchema()
        {
            Query = new AutomaticPersistedQueriesTestQuery();
        }
    }

    private readonly IDocumentExecuter<AutomaticPersistedQueriesTestSchema> _executer;
    private readonly IGraphQLTextSerializer _serializer;
    public readonly ServiceProvider Provider;

    public AutomaticPersistedQueriesFixture()
    {
        Provider = new ServiceCollection()
            .AddGraphQL(builder => builder
                .UseAutomaticPersistedQueries(options => options.SlidingExpiration = TimeSpan.FromMinutes(1))
                .AddSchema<AutomaticPersistedQueriesTestSchema>()
                .AddSystemTextJson()
            ).BuildServiceProvider();

        _executer = Provider.GetRequiredService<IDocumentExecuter<AutomaticPersistedQueriesTestSchema>>();
        _serializer = Provider.GetRequiredService<IGraphQLTextSerializer>();
    }

    public Task<ExecutionResult> ExecuteAsync(Action<ExecutionOptions> configure = null)
    {
        var options = new ExecutionOptions
        {
            RequestServices = Provider
        };
        configure?.Invoke(options);

        return _executer.ExecuteAsync(options);
    }

    public string Serialize(ExecutionResult result) => _serializer.Serialize(result);

    public void Dispose() => Provider.Dispose();
}
