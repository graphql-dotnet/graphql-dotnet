#if NET5_0_OR_GREATER

using System.Diagnostics;
using GraphQL.Telemetry;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace GraphQL.Tests.Instrumentation;

public class OpenTelemetryTests : IClassFixture<OpenTelemetryFixture>
{
    private readonly OpenTelemetryFixture _fixture;

    public OpenTelemetryTests(OpenTelemetryFixture fixture)
    {
        fixture.Reset();
        _fixture = fixture;
    }

    [Fact]
    public async Task BasicTest()
    {
        // execute GraphQL document
        var result = await _fixture.Executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{ hello }",
            RequestServices = _fixture.Host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        _fixture.Serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _fixture.ExportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "{ hello }"),
            new("graphql.operation.type", "query"),
            // no operation name
        });
        activity.DisplayName.ShouldBe("query");
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task WithOperationNameTest()
    {
        // execute GraphQL document
        var result = await _fixture.Executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query helloQuery { hello }",
            RequestServices = _fixture.Host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        _fixture.Serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _fixture.ExportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "query helloQuery { hello }"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "helloQuery"), // operation name pulled from document
        });
        activity.DisplayName.ShouldBe("query helloQuery");
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task DocumentFilterTest()
    {
        _fixture.Options.SanitizeDocument = options => options.Query?.Replace("hello", "testing");

        // execute GraphQL document
        var result = await _fixture.Executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{ hello }",
            RequestServices = _fixture.Host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        _fixture.Serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _fixture.ExportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "{ testing }"),
            new("graphql.operation.type", "query"),
            // no operation name
        });
        activity.DisplayName.ShouldBe("query");
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task Extensibility()
    {
        var executionOptions = new ExecutionOptions
        {
            Query = "query helloQuery { hello }",
            RequestServices = _fixture.Host.Services,
        };

        _fixture.Options.EnrichWithExecutionOptions = (activity, options) =>
        {
            options.ShouldBe(executionOptions);
            activity.SetTag("testoptions", "test1");
        };
        _fixture.Options.EnrichWithDocument = (activity, options, schema, document, operation) =>
        {
            options.ShouldBe(executionOptions);
            schema.AllTypes["Query"].ShouldBeOfType<AutoRegisteringObjectGraphType<OpenTelemetryFixture.Query>>();
            document.Source.ToString().ShouldBe(executionOptions.Query);
            operation.Operation.ShouldBe(GraphQLParser.AST.OperationType.Query);
            activity.SetTag("testdocument", "test2");
        };
        _fixture.Options.EnrichWithExecutionResult = (activity, options, result) =>
        {
            options.ShouldBe(executionOptions);
            result.Query.ToString().ShouldBe(executionOptions.Query);
            activity.SetTag("testresult", "test3");
        };

        // execute GraphQL document
        var result = await _fixture.Executer.ExecuteAsync(executionOptions).ConfigureAwait(false);

        // verify GraphQL response
        _fixture.Serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _fixture.ExportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "query helloQuery { hello }"),
            new("testoptions", "test1"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "helloQuery"), // operation name pulled from document
            new("testdocument", "test2"),
            new("testresult", "test3"),
        });
        activity.DisplayName.ShouldBe("query helloQuery");
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task Filterable()
    {
        var executionOptions = new ExecutionOptions
        {
            Query = "query helloQuery { hello }",
            RequestServices = _fixture.Host.Services,
        };
        var ranFilter = false;
        _fixture.Options.Filter = options =>
        {
            options.ShouldBe(executionOptions);
            ranFilter = true;
            return false;
        };

        // execute GraphQL document
        var result = await _fixture.Executer.ExecuteAsync(executionOptions).ConfigureAwait(false);

        // verify GraphQL response
        _fixture.Serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        _fixture.ExportedActivities.ShouldBeEmpty();
        ranFilter.ShouldBeTrue();
    }

    [Fact]
    public async Task WithValidationError()
    {
        // execute GraphQL document
        var result = await _fixture.Executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query helloQuery { hello { dummy } }",
            RequestServices = _fixture.Host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        result.Errors.ShouldNotBeNull().Count.ShouldBeGreaterThan(0);

        // verify activity telemetry
        var activity = _fixture.ExportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "query helloQuery { hello { dummy } }"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "helloQuery"),
        });
        activity.DisplayName.ShouldBe("query helloQuery");
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task WithParseError()
    {
        // execute GraphQL document
        var result = await _fixture.Executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{",
            RequestServices = _fixture.Host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        result.Errors.ShouldNotBeNull().Count.ShouldBeGreaterThan(0);

        // verify activity telemetry
        var activity = _fixture.ExportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "{"),
        });
        activity.DisplayName.ShouldBe("graphql");
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task WithServerError()
    {
        // execute GraphQL document
        var result = await _fixture.Executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{ serverError }",
            RequestServices = _fixture.Host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        result.Errors.ShouldNotBeNull().Count.ShouldBeGreaterThan(0);

        // verify activity telemetry
        var activity = _fixture.ExportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "{ serverError }"),
            new("graphql.operation.type", "query"),
#if !NET6_0_OR_GREATER
            new("otel.status_code", "ERROR"),
#endif
        });
        activity.DisplayName.ShouldBe("query");
        activity.Status().ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public async Task WithCancellation1()
    {
        // execute GraphQL document
        await Should.ThrowAsync<OperationCanceledException>(() => _fixture.Executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query helloQuery { hello }",
            RequestServices = _fixture.Host.Services,
            CancellationToken = new CancellationToken(true),
        })).ConfigureAwait(false);

        // verify activity telemetry
        var activity = _fixture.ExportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "query helloQuery { hello }"),
            // no operation name within ExecutionOptions, and request was canceled before parsing
        });
        activity.DisplayName.ShouldBe("graphql"); // unknown operation type since request was canceled before parsing
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task WithCancellation1b()
    {
        // execute GraphQL document
        await Should.ThrowAsync<OperationCanceledException>(() => _fixture.Executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query helloQuery { hello }",
            OperationName = "helloQuery",
            RequestServices = _fixture.Host.Services,
            CancellationToken = new CancellationToken(true),
        })).ConfigureAwait(false);

        // verify activity telemetry
        var activity = _fixture.ExportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.operation.name", "helloQuery"), // operation name pulled from ExecutionOptions
            new("graphql.document", "query helloQuery { hello }"),
        });
        activity.DisplayName.ShouldBe("graphql"); // unknown operation type since request was canceled before parsing
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task WithCancellation2()
    {
        // execute GraphQL document
        var cts = new CancellationTokenSource();
        await Should.ThrowAsync<OperationCanceledException>(() => _fixture.Executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query cancelQuery { cancel }",
            RequestServices = _fixture.Host.Services,
            Root = cts,
            CancellationToken = cts.Token,
        })).ConfigureAwait(false);

        // verify activity telemetry
        var activity = _fixture.ExportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "query cancelQuery { cancel }"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "cancelQuery"),
        });
        activity.DisplayName.ShouldBe("query cancelQuery");
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }
}

public class OpenTelemetryFixture : IDisposable
{
    public OpenTelemetryFixture()
    {
        // configure services
        Host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddOpenTelemetry()
                    .WithTracing(b => b
                        .AddSource(GraphQLTelemetryProvider.SourceName) // need to specify the source name to be traced
                        .AddInMemoryExporter(ExportedActivities));

                services.AddGraphQL(b => b
                    .AddSystemTextJson()
                    .AddAutoSchema<Query>()
                    .UseTelemetry());
            })
            .Build();

        // starts telemetry services
        Host.Start();
    }

    public void Reset()
    {
        ExportedActivities.Clear();

        var defaultOptions = new GraphQLTelemetryOptions();
        var options = Options;

        options.RecordDocument = defaultOptions.RecordDocument;
        options.SanitizeDocument = defaultOptions.SanitizeDocument;
        options.Filter = defaultOptions.Filter;
        options.EnrichWithExecutionOptions = defaultOptions.EnrichWithExecutionOptions;
        options.EnrichWithDocument = defaultOptions.EnrichWithDocument;
        options.EnrichWithExecutionResult = defaultOptions.EnrichWithExecutionResult;
        options.EnrichWithException = defaultOptions.EnrichWithException;
    }

    public IHost Host { get; }

    public List<Activity> ExportedActivities { get; } = new();

    public GraphQLTelemetryOptions Options => Host.Services.GetRequiredService<GraphQLTelemetryOptions>();

    public IDocumentExecuter<ISchema> Executer => Host.Services.GetRequiredService<IDocumentExecuter<ISchema>>();

    public IGraphQLTextSerializer Serializer => Host.Services.GetRequiredService<IGraphQLTextSerializer>();

    public void Dispose() => Host.Dispose();

    internal class Query
    {
        public static string Hello => "World";

        public static string Cancel(IResolveFieldContext context)
        {
            var cts = (CancellationTokenSource)context.Source;
            cts.Cancel();
            cts.Token.ThrowIfCancellationRequested();
            return "Canceled";
        }

        public static string ServerError => throw new InvalidOperationException("Could not process data");
    }
}

#endif
