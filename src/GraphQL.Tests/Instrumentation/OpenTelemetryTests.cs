#if NET5_0_OR_GREATER

using System.Diagnostics;
using System.Reflection;
using GraphQL.DI;
using GraphQL.Telemetry;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace GraphQL.Tests.Instrumentation;

[Collection("StaticTests")]
public sealed class OpenTelemetryTests : IDisposable
{
    private readonly List<Activity> _exportedActivities = [];
    private IHostBuilder __hostBuilder;
    private IHostBuilder _hostBuilder
    {
        get => __hostBuilder;
        set
        {
            __host?.Dispose();
            __host = null;
            __hostBuilder = value;
        }
    }
    private IHost? __host;
    private IHost _host
    {
        get
        {
            if (__host == null)
            {
                __host = _hostBuilder.Build();
                __host.Start();
            }
            return __host;
        }
    }
    private IDocumentExecuter<ISchema> _executer => _host.Services.GetRequiredService<IDocumentExecuter<ISchema>>();
    private IGraphQLTextSerializer _serializer => _host.Services.GetRequiredService<IGraphQLTextSerializer>();
    private GraphQLTelemetryOptions _options => _host.Services.GetRequiredService<GraphQLTelemetryOptions>();

    public OpenTelemetryTests()
    {
        __hostBuilder = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddOpenTelemetry()
                    .WithTracing(b => b
                        .AddSource(GraphQLTelemetryProvider.SourceName) // need to specify the source name to be traced
                        .AddSource(Query.ACTIVITY_SOURCE_NAME)
                        .AddInMemoryExporter(_exportedActivities));

                services.AddGraphQL(b => b
                    .AddSystemTextJson()
                    .AddAutoSchema<Query>()
                    .UseTelemetry());
            });
    }

    public void Dispose() => __host?.Dispose();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanInitializeTelemetryViaReflection(bool thenOverride)
    {
        //note: requires [Collection("StaticTests")] on the test class to ensure that no other tests are run concurrently
        try
        {
            OpenTelemetry.AutoInstrumentation.Initializer.Enabled.ShouldBeFalse();

            // sample of how OpenTelemetry.AutoInstrumentation.Initializer.EnableAutoInstrumentation() is called by the OpenTelemetry framework
            // see https://github.com/graphql-dotnet/graphql-dotnet/pull/3631
            // see https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/2520
            // see https://github.com/RassK/opentelemetry-dotnet-instrumentation/pull/954 for implementation of this code
            var type = typeof(DocumentExecuter).Assembly.GetType("OpenTelemetry.AutoInstrumentation.Initializer").ShouldNotBeNull();
            var method = type.GetMethod("EnableAutoInstrumentation", BindingFlags.Public | BindingFlags.Static).ShouldNotBeNull();
            var optionsType = typeof(DocumentExecuter).Assembly.GetType("GraphQL.Telemetry.GraphQLTelemetryOptions").ShouldNotBeNull();
            var optionsInstance = Activator.CreateInstance(optionsType)!;
            var recordDocumentOption = optionsType.GetProperty("RecordDocument").ShouldNotBeNull();
            recordDocumentOption.SetValue(optionsInstance, true);
            method.Invoke(null, [optionsInstance]);

            // verify that the initializer was called
            OpenTelemetry.AutoInstrumentation.Initializer.Enabled.ShouldBeTrue();

            if (!thenOverride)
            {
                var services = new ServiceCollection();
                services.AddGraphQL(b => { });
                // verify that the telemetry service is added implicitly
                var serviceDescriptor = services.SingleOrDefault(x => x.ImplementationInstance == GraphQLTelemetryProvider.AutoTelemetryProvider).ShouldNotBeNull();
                serviceDescriptor.ServiceType.ShouldBe(typeof(IConfigureExecution));
                serviceDescriptor.Lifetime.ShouldBe(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);
                // verify that no other telemetry service was added
                services.SingleOrDefault(x => x.ImplementationType == typeof(GraphQLTelemetryProvider)).ShouldBeNull();
                // ensure auto-telemetry is still enabled
                OpenTelemetry.AutoInstrumentation.Initializer.Enabled.ShouldBeTrue();
            }
            else
            {
                var services = new ServiceCollection();
                services.AddGraphQL(b => b.UseTelemetry());
                // verify that the telemetry service is not added implicitly
                services.SingleOrDefault(x => x.ImplementationInstance == GraphQLTelemetryProvider.AutoTelemetryProvider).ShouldBeNull();
                // verify that the explicitly added telemetry service was added
                var serviceDescriptor = services.SingleOrDefault(x => x.ImplementationType == typeof(GraphQLTelemetryProvider)).ShouldNotBeNull();
                serviceDescriptor.ServiceType.ShouldBe(typeof(IConfigureExecution));
                serviceDescriptor.Lifetime.ShouldBe(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);
                // ensure auto-telemetry is disabled
                OpenTelemetry.AutoInstrumentation.Initializer.Enabled.ShouldBeFalse();
            }
        }
        finally
        {
            OpenTelemetry.AutoInstrumentation.Initializer.Enabled = false;
            OpenTelemetry.AutoInstrumentation.Initializer.Options = null;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task BasicTest(int registrationMethod)
    {
        _hostBuilder = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddOpenTelemetry()
                    .WithTracing(b => b
                        .AddSource(GraphQLTelemetryProvider.SourceName) // need to specify the source name to be traced
                        .AddInMemoryExporter(_exportedActivities));

                services.AddGraphQL(b =>
                {
                    b.AddSystemTextJson();
                    b.AddAutoSchema<Query>();

                    GraphQLTelemetryOptions Config(GraphQLTelemetryOptions options)
                    {
                        options.EnrichWithExecutionOptions = (a, _) => a.SetTag("mytag", registrationMethod);
                        return options;
                    }

                    switch (registrationMethod)
                    {
                        case 1:
                            b.UseTelemetry(c => Config(c));
                            break;
                        case 2:
                            b.UseTelemetry((c, _) => Config(c));
                            break;
                        case 3:
                            b.UseTelemetry<GraphQLTelemetryProvider>(c => Config(c));
                            break;
                        case 4:
                            b.UseTelemetry<GraphQLTelemetryProvider>((c, _) => Config(c));
                            break;
                        case 5:
                            b.UseTelemetry(_ => new GraphQLTelemetryProvider(Config(new())));
                            break;
                        case 6:
                            b.UseTelemetry(new GraphQLTelemetryProvider(Config(new())));
                            break;
                        case 7:
                            b.UseTelemetry<GraphQLTelemetryProvider, GraphQLTelemetryOptions>(c => Config(c));
                            break;
                        case 8:
                            b.UseTelemetry<GraphQLTelemetryProvider, GraphQLTelemetryOptions>((c, _) => Config(c));
                            break;
                    }
                });
            });

        // execute GraphQL document
        var result = await _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{ hello }",
            RequestServices = _host.Services,
        });

        // verify GraphQL response
        _serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.TagObjects.ShouldBe([
            new("graphql.document", "{ hello }"),
            new("mytag", registrationMethod),
            new("graphql.operation.type", "query")
            // no operation name
        ]);
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
        });

        // verify GraphQL response
        _serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe([
            new("graphql.document", "query helloQuery { hello }"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "helloQuery") // operation name pulled from document
        ]);
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
        });

        // verify GraphQL response
        _serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe([
            new("graphql.document", "{ testing }"),
            new("graphql.operation.type", "query")
            // no operation name
        ]);
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
        var result = await _executer.ExecuteAsync(executionOptions);

        // verify GraphQL response
        _serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe([
            new("graphql.document", "query helloQuery { hello }"),
            new("testoptions", "test1"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "helloQuery"), // operation name pulled from document
            new("testdocument", "test2"),
            new("testresult", "test3")
        ]);
        activity.DisplayName.ShouldBe("query helloQuery");
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Filterable(bool filter)
    {
        var executionOptions = new ExecutionOptions
        {
            Query = "query withActivityQuery { withActivity }",
            RequestServices = _host.Services,
        };
        bool ranFilter = false;
        _options.Filter = options =>
        {
            options.ShouldBe(executionOptions);
            ranFilter = true;
            return filter;
        };

        // execute GraphQL document
        var result = await _executer.ExecuteAsync(executionOptions);

        // verify GraphQL response
        string childActivityCreated = filter.ToString().ToLower();
        _serializer.Serialize(result).ShouldBe($$$"""{"data":{"withActivity":{{{childActivityCreated}}}}}""");

        // verify activity telemetry
        _exportedActivities.Count.ShouldBe(filter ? 2 : 0);
        ranFilter.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FilterThrowsException(bool recordException)
    {
        var executionOptions = new ExecutionOptions
        {
            Query = "query withActivityQuery { withActivity }",
            RequestServices = _host.Services,
            ThrowOnUnhandledException = true,
        };
        bool ranFilter = false;
        _options.Filter = options =>
        {
            options.ShouldBe(executionOptions);
            ranFilter = true;
            throw new FilterException("exception message");
        };

        _options.RecordException = recordException;

        // execute GraphQL document
        await _executer.ExecuteAsync(executionOptions).ShouldThrowAsync<FilterException>();

        // verify activity telemetry
        ranFilter.ShouldBeTrue();
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string?>[]
        {
#if !NET6_0_OR_GREATER
            new("otel.status_code", "ERROR"),
#endif
        });
        activity.Status().ShouldBe(ActivityStatusCode.Error);

        if (recordException)
        {
            var @event = activity.Events.ShouldHaveSingleItem();
            @event.Name.ShouldBe("exception");
            @event.Tags.ShouldNotBeEmpty();
            @event.Tags.FirstOrDefault(t => t.Key == "exception.type").Value.ShouldBe("GraphQL.Tests.Instrumentation.OpenTelemetryTests+FilterException");
            @event.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace").Value.ShouldNotBe(string.Empty);
            @event.Tags.FirstOrDefault(t => t.Key == "exception.message").Value.ShouldBe("exception message");
        }
        else
        {
            activity.Events.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [InlineData(null)] // null for exception
    public async Task ExtensibilityWhenFiltered(bool? filter)
    {
        var executionOptions = new ExecutionOptions
        {
            Query = "query helloQuery { hello }",
            RequestServices = _host.Services,
            ThrowOnUnhandledException = true,
        };

        _options.Filter = _ => filter ?? throw new FilterException();

        bool enrichWithExecutionOptionsCalled = false;
        _options.EnrichWithExecutionOptions = (_, _) =>
            enrichWithExecutionOptionsCalled = true;

        bool enrichWithDocumentCalled = false;
        _options.EnrichWithDocument = (_, _, _, _, _) =>
            enrichWithDocumentCalled = true;

        bool enrichWithExecutionResult = false;
        _options.EnrichWithExecutionResult = (_, _, _) =>
            enrichWithExecutionResult = true;

        bool enrichWithException = false;
        _options.EnrichWithException = (_, _) =>
            enrichWithException = true;

        if (filter.HasValue)
        {
            // execute GraphQL document
            var result = await _executer.ExecuteAsync(executionOptions);
            // verify GraphQL response
            _serializer.Serialize(result).ShouldBe("""{"data":{"hello":"World"}}""");
        }
        else
        {
            // execute GraphQL document
            await _executer.ExecuteAsync(executionOptions).ShouldThrowAsync<FilterException>();
        }

        // verify enrich methods execution
        enrichWithExecutionOptionsCalled.ShouldBe(filter ?? false);
        enrichWithDocumentCalled.ShouldBe(filter ?? false);
        enrichWithExecutionResult.ShouldBe(filter ?? false);
        enrichWithException.ShouldBe(filter == null);
    }

    [Fact]
    public async Task WithValidationError()
    {
        // execute GraphQL document
        var result = await _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "query helloQuery { hello { dummy } }",
            RequestServices = _host.Services,
        });

        // verify GraphQL response
        result.Errors.ShouldNotBeNull().Count.ShouldBeGreaterThan(0);

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe([
            new("graphql.document", "query helloQuery { hello { dummy } }"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "helloQuery")
        ]);
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
        });

        // verify GraphQL response
        result.Errors.ShouldNotBeNull().Count.ShouldBeGreaterThan(0);

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe([
            new("graphql.document", "{")
        ]);
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
        });

        // verify GraphQL response
        result.Errors.ShouldNotBeNull().Count.ShouldBeGreaterThan(0);

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string?>[]
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WithServerErrorWhenThrowOnUnhandledExceptionTrue(bool recordException)
    {
        _options.RecordException = recordException;

        // execute GraphQL document
        await _executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{ serverError }",
            RequestServices = _host.Services,
            ThrowOnUnhandledException = true
        }).ShouldThrowAsync<InvalidOperationException>();

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe(new KeyValuePair<string, string?>[]
        {
            new("graphql.document", "{ serverError }"),
            new("graphql.operation.type", "query"),
#if !NET6_0_OR_GREATER
            new("otel.status_code", "ERROR"),
#endif
        });
        activity.DisplayName.ShouldBe("query");
        activity.Status().ShouldBe(ActivityStatusCode.Error);

        if (recordException)
        {
            var @event = activity.Events.ShouldHaveSingleItem();
            @event.Name.ShouldBe("exception");
            @event.Tags.ShouldNotBeEmpty();
            @event.Tags.FirstOrDefault(t => t.Key == "exception.type").Value.ShouldBe("System.InvalidOperationException");
            @event.Tags.FirstOrDefault(t => t.Key == "exception.stacktrace").Value.ShouldNotBe(string.Empty);
            @event.Tags.FirstOrDefault(t => t.Key == "exception.message").Value.ShouldBe("Could not process data");
        }
        else
        {
            activity.Events.ShouldBeEmpty();
        }
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
        }));

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe([
            new("graphql.document", "query helloQuery { hello }")
            // no operation name within ExecutionOptions, and request was canceled before parsing
        ]);
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
        }));

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe([
            new("graphql.operation.name", "helloQuery"), // operation name pulled from ExecutionOptions
            new("graphql.document", "query helloQuery { hello }")
        ]);
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
        }));

        // verify activity telemetry
        var activity = _exportedActivities.ShouldHaveSingleItem();
        activity.Tags.ShouldBe([
            new("graphql.document", "query cancelQuery { cancel }"),
            new("graphql.operation.type", "query"),
            new("graphql.operation.name", "cancelQuery")
        ]);
        activity.DisplayName.ShouldBe("query cancelQuery");
        activity.Status().ShouldBe(ActivityStatusCode.Unset);
        activity.Events.ShouldBeEmpty();
    }

    private class Query
    {
        public const string ACTIVITY_SOURCE_NAME = "Query";
        private static readonly ActivitySource _activitySource = new(ACTIVITY_SOURCE_NAME);

        public static string Hello => "World";

        public static string Cancel(IResolveFieldContext context)
        {
            var cts = (CancellationTokenSource)context.Source!;
            cts.Cancel();
            cts.Token.ThrowIfCancellationRequested();
            return "Canceled";
        }

        public static bool WithActivity()
        {
            using var activity = _activitySource.StartActivity();
            return activity != null;
        }

        public static string ServerError => throw new InvalidOperationException("Could not process data");
    }

    private class FilterException : Exception
    {
        public FilterException() { }
        public FilterException(string message) : base(message) { }
    }
}

#endif
