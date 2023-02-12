#if NET5_0_OR_GREATER

using System.Diagnostics;
using GraphQL.Instrumentation;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace GraphQL.Tests.Instrumentation;

public class OpenTelemetryTests : IDisposable
{
    private readonly List<Activity> _exportedActivities = new();
    private readonly IHost _host;
    private readonly IDocumentExecuter<ISchema> _executer;
    private readonly IGraphQLTextSerializer _serializer;

    public OpenTelemetryTests()
    {
        // configure services
        _host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddOpenTelemetry()
                    .WithTracing(b => b
                        .AddSource(GraphQLTelemetryProvider.SourceName) // need to specify the source name to be traced
                        .AddInMemoryExporter(_exportedActivities));

                services.AddGraphQL(b => b
                    .AddSystemTextJson()
                    .AddAutoSchema<Query>()
                    .UseTelemetry());
            })
            .Build();

        // starts telemetry services
        _host.Start();

        _executer = _host.Services.GetRequiredService<IDocumentExecuter<ISchema>>();
        _serializer = _host.Services.GetRequiredService<IGraphQLTextSerializer>();
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task BasicTest()
    {
        // execute GraphQL document
        var result = await _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query helloQuery { hello }",
            RequestServices = _host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        _serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "query helloQuery { hello }"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "helloQuery"), // operation name pulled from document
        });
        activity.DisplayName.ShouldBe("query helloQuery");
#if NET6_0_OR_GREATER
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
#endif
    }

    [Fact]
    public async Task BasicTest2()
    {
        // execute GraphQL document
        var result = await _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{ hello }",
            RequestServices = _host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        _serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "{ hello }"),
            new("graphql.operation.type", "query"),
            // no operation name
        });
        activity.DisplayName.ShouldBe("query");
#if NET6_0_OR_GREATER
        activity.Status.ShouldBe(ActivityStatusCode.Ok);
#endif
    }

    [Fact]
    public async Task WithValidationError()
    {
        // execute GraphQL document
        var result = await _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query helloQuery { hello { dummy } }",
            RequestServices = _host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        result.Errors.ShouldNotBeNull().Count.ShouldBeGreaterThan(0);

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "query helloQuery { hello { dummy } }"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "helloQuery"),
        });
        activity.DisplayName.ShouldBe("query helloQuery");
#if NET6_0_OR_GREATER
        activity.Status.ShouldBe(ActivityStatusCode.Error);
#endif
    }

    [Fact]
    public async Task WithParseError()
    {
        // execute GraphQL document
        var result = await _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{",
            RequestServices = _host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        result.Errors.ShouldNotBeNull().Count.ShouldBeGreaterThan(0);

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "{"),
        });
        activity.DisplayName.ShouldBe("graphql");
#if NET6_0_OR_GREATER
        activity.Status.ShouldBe(ActivityStatusCode.Error);
#endif
    }

    [Fact]
    public async Task WithCancellation1()
    {
        // execute GraphQL document
        await Should.ThrowAsync<OperationCanceledException>(() => _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query helloQuery { hello }",
            RequestServices = _host.Services,
            CancellationToken = new CancellationToken(true),
        })).ConfigureAwait(false);

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "query helloQuery { hello }"),
            // no operation name within ExecutionOptions, and request was canceled before parsing
        });
        activity.DisplayName.ShouldBe("graphql"); // unknown operation type since request was canceled before parsing
#if NET6_0_OR_GREATER
        activity.Status.ShouldBe(ActivityStatusCode.Unset);
#endif
    }

    [Fact]
    public async Task WithCancellation1b()
    {
        // execute GraphQL document
        await Should.ThrowAsync<OperationCanceledException>(() => _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query helloQuery { hello }",
            OperationName = "helloQuery",
            RequestServices = _host.Services,
            CancellationToken = new CancellationToken(true),
        })).ConfigureAwait(false);

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.operation.name", "helloQuery"), // operation name pulled from ExecutionOptions
            new("graphql.document", "query helloQuery { hello }"),
        });
        activity.DisplayName.ShouldBe("graphql"); // unknown operation type since request was canceled before parsing
#if NET6_0_OR_GREATER
        activity.Status.ShouldBe(ActivityStatusCode.Unset);
#endif
    }

    [Fact]
    public async Task WithCancellation2()
    {
        // execute GraphQL document
        var cts = new CancellationTokenSource();
        await Should.ThrowAsync<OperationCanceledException>(() => _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query cancelQuery { cancel }",
            RequestServices = _host.Services,
            Root = cts,
            CancellationToken = cts.Token,
        })).ConfigureAwait(false);

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string>[]
        {
            new("graphql.document", "query cancelQuery { cancel }"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "cancelQuery"),
        });
        activity.DisplayName.ShouldBe("query cancelQuery");
#if NET6_0_OR_GREATER
        activity.Status.ShouldBe(ActivityStatusCode.Unset);
#endif
    }

    private class Query
    {
        public static string Hello => "World";

        public static string Cancel(IResolveFieldContext context)
        {
            var cts = (CancellationTokenSource)context.Source;
            cts.Cancel();
            cts.Token.ThrowIfCancellationRequested();
            return "Canceled";
        }
    }
}

#endif
