using GraphQL.MicrosoftDI;
using GraphQL.Caching;
using GraphQL.Types;
using GraphQL.SystemTextJson;
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
        _fixture.Provider.GetRequiredService<IOptions<MemoryQueryCacheOptions>>().Value.SlidingExpiration.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Ordinary_Request_Should_Work()
    {
        var result = await _fixture.ExecuteAsync(opt => opt.Query = "query { ping }").ConfigureAwait(false);

        result.Errors.ShouldBeNull();
        _fixture.Serialize(result).ShouldBe(@"{""data"":{""ping"":""pong""}}");
    }

    [Fact]
    public async Task Without_Query_And_Hash_Should_Return_Error()
    {
        var result = await _fixture.ExecuteAsync().ConfigureAwait(false);

        result.Errors.ShouldNotBeNull();
        result.Errors.Single().Message.ShouldBe("GraphQL query is missing.");
    }

    [Fact]
    public async Task Wrong_Version_Should_Be_Detected()
    {
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = "1",
                ["version"] = "2"
            }
        });

        var result = await _fixture.ExecuteAsync(opt => opt.Extensions = extentions).ConfigureAwait(false);

        result.Errors.ShouldNotBeNull();
        var error = result.Errors.Single();
        error.Message.ShouldBe("Persisted queries with '2' version are not supported.");
        error.Code.ShouldBe("PERSISTED_QUERY_UNSUPPORTED_VERSION");
    }

    [Fact]
    public async Task Not_Saved_Query_Should_Return_Not_Found_Code()
    {
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = "1",
                ["version"] = "1"
            }
        });

        var result = await _fixture.ExecuteAsync(opt => opt.Extensions = extentions).ConfigureAwait(false);

        result.Errors.ShouldNotBeNull();
        var error = result.Errors.Single();
        error.Message.ShouldBe("Persisted query with '1' hash was not found.");
        error.Code.ShouldBe("PERSISTED_QUERY_NOT_FOUND");
    }

    [Fact]
    public async Task Bad_Hash_Should_Be_Detected()
    {
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = "badHash",
                ["version"] = "1"
            }
        });
        var result = await _fixture.ExecuteAsync(opt =>
        {
            opt.Query = "query { ping }";
            opt.Extensions = extentions;
        }).ConfigureAwait(false);

        result.Errors.ShouldNotBeNull();
        var error = result.Errors.Single();
        error.Message.ShouldBe("The 'badHash' hash doesn't correspond to a query.");
        error.Code.ShouldBe("PERSISTED_QUERY_BAD_HASH");
    }

    [Fact]
    public async Task Persisted_Query_Should_Work()
    {
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = "d7b0dfafc61a1f0618f4f346911d5aa87bef97b134f2943383223bdac4410134",
                ["version"] = "1"
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
                Field<StringGraphType>("ping", resolve: _ => "pong");
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
                .AddAutomaticPersistedQueries(options => options.SlidingExpiration = TimeSpan.FromMinutes(1))
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
