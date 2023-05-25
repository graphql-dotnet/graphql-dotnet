#if NET5_0_OR_GREATER

using System.Diagnostics;
using System.Reflection;
using GraphQL.DI;
using GraphQL.Telemetry;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace GraphQL.Tests.Instrumentation;

[Collection("StaticTests")]
public sealed class OpenTelemetryTests : IDisposable
{
    private readonly List<Activity> _exportedActivities = new();
    private readonly IHost _host;
    private readonly IDocumentExecuter<ISchema> _executer;
    private readonly IGraphQLTextSerializer _serializer;
    private readonly GraphQLTelemetryOptions _options;

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
        _options = _host.Services.GetRequiredService<GraphQLTelemetryOptions>();
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void CanInitializeAutoTelemetryViaReflection()
    {
        //note: requires [Collection("StaticTests")] on the test class to ensure that no other tests are run concurrently
        try
        {
            OpenTelemetry.AutoInstrumentation.Initializer.Enabled.ShouldBeFalse();

            // sample of how OpenTelemetry.AutoInstrumentation.Initializer.EnableAutoInstrumentation() is called by the OpenTelemetry framework
            // see https://github.com/graphql-dotnet/graphql-dotnet/pull/3631 and https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/2520
            var type = typeof(DocumentExecuter).Assembly.GetType("OpenTelemetry.AutoInstrumentation.Initializer").ShouldNotBeNull();
            var method = type.GetMethod("EnableAutoInstrumentation", BindingFlags.Static | BindingFlags.Public).ShouldNotBeNull();
            method.Invoke(null, new object[] { null });

            // verify that the initializer was called
            OpenTelemetry.AutoInstrumentation.Initializer.Enabled.ShouldBeTrue();

            // verify that the telemetry service is added implicitly
            var services = new ServiceCollection();
            services.AddGraphQL(_ => { });
            var serviceDescriptor = services.SingleOrDefault(x => x.ImplementationType == typeof(GraphQLTelemetryProvider)).ShouldNotBeNull();
            serviceDescriptor.ServiceType.ShouldBe(typeof(IConfigureExecution));
            serviceDescriptor.Lifetime.ShouldBe(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);
        }
        finally
        {
            OpenTelemetry.AutoInstrumentation.Initializer.Enabled = false;
            OpenTelemetry.AutoInstrumentation.Initializer.Options = null;
        }
    }

    [Fact]
    public async Task BasicTest()
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
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task WithOperationNameTest()
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
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task DocumentFilterTest()
    {
        _options.SanitizeDocument = options => options.Query?.Replace("hello", "testing");

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
            RequestServices = _host.Services,
        };

        _options.EnrichWithExecutionOptions = (activity, options) =>
        {
            options.ShouldBe(executionOptions);
            activity.SetTag("testoptions", "test1");
        };
        _options.EnrichWithDocument = (activity, options, schema, document, operation) =>
        {
            options.ShouldBe(executionOptions);
            schema.AllTypes["Query"].ShouldBeOfType<AutoRegisteringObjectGraphType<Query>>();
            document.Source.ToString().ShouldBe(executionOptions.Query);
            operation.Operation.ShouldBe(GraphQLParser.AST.OperationType.Query);
            activity.SetTag("testdocument", "test2");
        };
        _options.EnrichWithExecutionResult = (activity, options, result) =>
        {
            options.ShouldBe(executionOptions);
            result.Query.ToString().ShouldBe(executionOptions.Query);
            activity.SetTag("testresult", "test3");
        };

        // execute GraphQL document
        var result = await _executer.ExecuteAsync(executionOptions).ConfigureAwait(false);

        // verify GraphQL response
        _serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
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
            RequestServices = _host.Services,
        };
        bool ranFilter = false;
        _options.Filter = options =>
        {
            options.ShouldBe(executionOptions);
            ranFilter = true;
            return false;
        };

        // execute GraphQL document
        var result = await _executer.ExecuteAsync(executionOptions).ConfigureAwait(false);

        // verify GraphQL response
        _serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        _exportedActivities.ShouldBeEmpty();
        ranFilter.ShouldBeTrue();
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
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
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
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task WithServerError()
    {
        // execute GraphQL document
        var result = await _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{ serverError }",
            RequestServices = _host.Services,
        }).ConfigureAwait(false);

        // verify GraphQL response
        result.Errors.ShouldNotBeNull().Count.ShouldBeGreaterThan(0);

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
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
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
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
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
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
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
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

        public static string ServerError => throw new InvalidOperationException("Could not process data");
    }
}

#endif
