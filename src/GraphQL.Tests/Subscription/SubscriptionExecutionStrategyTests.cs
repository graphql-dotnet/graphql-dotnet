#nullable enable
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System.Collections.Concurrent;
using GraphQL.Execution;
using GraphQL.MicrosoftDI;
using GraphQL.Subscription;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Subscription;

public class SubscriptionExecutionStrategyTests
{
    private SampleObservable<string> Source { get; } = new();
    private SampleObserver? Observer { get; set; }
    private IDisposable? Disposer { get; set; }

    [Fact]
    public async Task Basic()
    {
        var result = await ExecuteAsync("subscription { test }");
        result.ShouldBeSuccessful();
        Disposer.ShouldNotBeNull();
        result.Perf.ShouldBeNull();
        Source.Next("hello");
        Source.Next("testing");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""test"": ""hello"" } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""test"": ""testing"" } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task NoMetricsForDataEvents()
    {
        var result = await ExecuteAsync("subscription { test }", o => o.EnableMetrics = true);
        result.ShouldBeSuccessful();
        result.Perf.ShouldNotBeNull();
        Source.Next("hello");
        var result2 = Observer.ShouldHaveResult();
        result2.ShouldBeSimilarTo(@"{ ""data"": { ""test"": ""hello"" } }");
        result2.Perf.ShouldBeNull();
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task InitialExtensions()
    {
        var result = await ExecuteAsync("subscription { testWithInitialExtensions }");
        result.ShouldBeSuccessful();
        result.Extensions.ShouldBeSimilarTo(@"{ ""alpha"": ""beta"" }");
        Source.Next("hello");
        Source.Next("testing");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testWithInitialExtensions"": ""hello"" } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testWithInitialExtensions"": ""testing"" } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task InitialError()
    {
        var result = await ExecuteAsync("subscription { testWithInitialError(custom: false) }");
        result.Executed.ShouldBeTrue();
        result.ShouldBeSimilarTo(@"{ ""errors"":[{ ""message"":""Could not subscribe to field \u0027testWithInitialError\u0027."",""locations"":[{ ""line"":1,""column"":16}],""path"":[""testWithInitialError""],""extensions"":{ ""code"":""APPLICATION"",""codes"":[""APPLICATION""]} }],""data"":null}");
        result.Streams.ShouldBeEmpty();
    }

    [Fact]
    public async Task InitialCustomError()
    {
        var result = await ExecuteAsync("subscription { testWithInitialError(custom: true) }");
        result.Executed.ShouldBeTrue();
        result.ShouldBeSimilarTo(@"{ ""errors"":[{ ""message"":""Handled custom exception: InitialException"",""locations"":[{ ""line"":1,""column"":16}],""path"":[""testWithInitialError""],""extensions"":{ ""code"":""INVALID_OPERATION"",""codes"":[""INVALID_OPERATION""]} }],""data"":null}");
        result.Streams.ShouldBeEmpty();
    }

    [Fact]
    public async Task Widget()
    {
        var result = await ExecuteAsync("subscription { testComplex { id name } }");
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Next("testing");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""name"": ""hello"" } } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""name"": ""testing"" } } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task Widget_WithExtension()
    {
        var result = await ExecuteAsync("subscription { testComplex { id nameWithExtension } }");
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Next("testing");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""nameWithExtension"": ""hello"" } }, ""extensions"": { ""alpha"": ""hello"" } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""nameWithExtension"": ""testing"" } }, ""extensions"": { ""alpha"": ""testing"" }  }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task Widget_WithErrors()
    {
        // a field error has occurred on a non-null child field of a non-null subscription field,
        // so the null value gets propogated all the way up to "data"
        var result = await ExecuteAsync("subscription { testComplex { id nameMayThrowError } }");
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Next("custom");
        Source.Next("testing");
        Source.Next("application");
        Source.Next("success");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""nameMayThrowError"": ""hello"" } } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{""errors"":[{""message"":""Handled custom exception: Custom error"",""locations"":[{""line"":1,""column"":33}],""path"":[""testComplex"",""nameMayThrowError""],""extensions"":{""code"":""INVALID_OPERATION"",""codes"":[""INVALID_OPERATION""]}}],""data"":null}");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""nameMayThrowError"": ""testing"" } } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{""errors"":[{""message"":""Error trying to resolve field \u0027nameMayThrowError\u0027."",""locations"":[{""line"":1,""column"":33}],""path"":[""testComplex"",""nameMayThrowError""],""extensions"":{""code"":""APPLICATION"",""codes"":[""APPLICATION""]}}],""data"":null}");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""nameMayThrowError"": ""success"" } } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task Widget_WithErrors_Nullable()
    {
        // a field error has occurred on a nullable field, so just that field is null
        var result = await ExecuteAsync("subscription { testComplex { id nameMayThrowErrorNullable } }");
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Next("custom");
        Source.Next("testing");
        Source.Next("application");
        Source.Next("success");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""nameMayThrowErrorNullable"": ""hello"" } } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{""errors"":[{""message"":""Handled custom exception: Custom error"",""locations"":[{""line"":1,""column"":33}],""path"":[""testComplex"",""nameMayThrowErrorNullable""],""extensions"":{""code"":""INVALID_OPERATION"",""codes"":[""INVALID_OPERATION""]}}],""data"":{""testComplex"":{""id"":""SampleId"",""nameMayThrowErrorNullable"":null}}}");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""nameMayThrowErrorNullable"": ""testing"" } } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{""errors"":[{""message"":""Error trying to resolve field \u0027nameMayThrowErrorNullable\u0027."",""locations"":[{""line"":1,""column"":33}],""path"":[""testComplex"",""nameMayThrowErrorNullable""],""extensions"":{""code"":""APPLICATION"",""codes"":[""APPLICATION""]}}],""data"":{""testComplex"":{""id"":""SampleId"",""nameMayThrowErrorNullable"":null}}}");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""nameMayThrowErrorNullable"": ""success"" } } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task Widget_SourceError()
    {
        // in this case the graphql execution has not started, so executed should be false
        // and "data" should not exist in the map of error events
        var result = await ExecuteAsync("subscription { testComplex { id name } }");
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Error(new ApplicationException("SourceError"));
        Source.Next("testing");
        Source.Error(new InvalidOperationException("SourceError"));
        Source.Next("success");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""name"": ""hello"" } } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{""errors"":[{""message"":""Event stream error for field \u0027testComplex\u0027."",""locations"":[{""line"":1,""column"":16}],""path"":[""testComplex""],""extensions"":{""code"":""APPLICATION"",""codes"":[""APPLICATION""]}}]}");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""name"": ""testing"" } } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{""errors"":[{""message"":""Handled custom exception: SourceError"",""locations"":[{""line"":1,""column"":16}],""path"":[""testComplex""],""extensions"":{""code"":""INVALID_OPERATION"",""codes"":[""INVALID_OPERATION""]}}]}");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""id"": ""SampleId"", ""name"": ""success"" } } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task Widget_Null()
    {
        // verify that if a non-null subscription field returns null during a data event,
        // then "data" is "null", and DOES exist in the resulting map (along with the
        // descriptive error)
        var result = await ExecuteAsync("subscription { testNullHandling { id name } }");
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Next(null!);
        Source.Next("testing");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testNullHandling"": { ""id"": ""SampleId"", ""name"": ""hello"" } } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{""errors"":[{""message"":""Handled custom exception: Cannot return null for a non-null type. Field: testNullHandling, Type: MyWidget!."",""locations"":[{""line"":1,""column"":16}],""path"":[""testNullHandling""],""extensions"":{""code"":""INVALID_OPERATION"",""codes"":[""INVALID_OPERATION""]}}],""data"":null}");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testNullHandling"": { ""id"": ""SampleId"", ""name"": ""testing"" } } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task Widget_Null_Nullable()
    {
        var result = await ExecuteAsync("subscription { testNullHandlingNullable { id name } }");
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Next(null!);
        Source.Next("testing");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testNullHandlingNullable"": { ""id"": ""SampleId"", ""name"": ""hello"" } } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{""data"":{""testNullHandlingNullable"":null}}");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testNullHandlingNullable"": { ""id"": ""SampleId"", ""name"": ""testing"" } } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task NotSubscriptionField()
    {
        var result = await ExecuteAsync("subscription { notSubscriptionField }");
        result.Streams?.ShouldBeEmpty();
        result.ShouldBeSimilarTo(@"{""errors"":[{""message"":""Handled custom exception: Subscriber not set for field \u0027notSubscriptionField\u0027."",""locations"":[{""line"":1,""column"":16}],""path"":[""notSubscriptionField""],""extensions"":{""code"":""INVALID_OPERATION"",""codes"":[""INVALID_OPERATION""]}}],""data"":null}");
    }

    public int Counter = 0;

    [Fact]
    public async Task DocumentListeners_and_UserContext_works()
    {
        var listener = new SampleListener { TestClass = this };
        Counter.ShouldBe(0);
        var result = await ExecuteAsync("subscription { testComplex { name getCounter } }", o =>
        {
            o.Listeners.Add(listener);
            o.UserContext["testClass"] = this;
        });
        Counter.ShouldBe(11);
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Counter.ShouldBe(22);
        Source.Next("testing");
        Counter.ShouldBe(33);
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": {""name"": ""hello"", ""getCounter"": 12 } } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": {""name"": ""testing"", ""getCounter"": 23 } } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task RootCancellationThrowsOnInitialExecution()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(async () => await ExecuteAsync("subscription { test }", o => o.CancellationToken = cts.Token));
    }

    [Fact]
    public async Task RootCancellationDoesNotAffectSubscriptions()
    {
        using var cts = new CancellationTokenSource();
        var result = await ExecuteAsync("subscription { test }", o => o.CancellationToken = cts.Token);
        result.ShouldBeSuccessful();
        result.Perf.ShouldBeNull();
        Source.Next("hello");
        Source.Next("testing");
        cts.Cancel();
        Source.Next("success");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""test"": ""hello"" } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""test"": ""testing"" } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""test"": ""success"" } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task NoEventsAfterSourceDisconnected()
    {
        using var cts = new CancellationTokenSource();
        var result = await ExecuteAsync("subscription { test }", o => o.CancellationToken = cts.Token);
        result.ShouldBeSuccessful();
        result.Perf.ShouldBeNull();
        Source.Next("hello");
        Source.Next("testing");
        Disposer!.Dispose();
        Source.Next("should not happen");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""test"": ""hello"" } }");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""test"": ""testing"" } }");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task CheckServiceProvider()
    {
        var result = await ExecuteAsync("subscription { testComplex { validateServiceProvider } }", o => o.UserContext["provider"] = o.RequestServices);
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Observer.ShouldHaveResult().ShouldBeSimilarTo(@"{ ""data"": { ""testComplex"": { ""validateServiceProvider"": true } } }");
    }

    #region - Schema -
    private class Query
    {
        public static string Hero => "hi";
    }

    private class Subscription
    {
        public static IObservable<string> Test([FromServices] IObservable<string> source) => source;

        public static IObservable<string> TestWithInitialExtensions(IResolveFieldContext context, [FromServices] IObservable<string> source)
        {
            context.SetOutputExtension("alpha", "beta");
            return source;
        }

        public static IObservable<string> TestWithInitialError(bool custom)
            => throw (custom ? new InvalidOperationException("InitialException") : new ApplicationException("InitialException"));

        public static IObservable<MyWidget> TestComplex([FromServices] IObservable<string> source)
            => new ObservableSelect<string, MyWidget>(source, value => new MyWidget { Name = value });

        public static IObservable<MyWidget> TestNullHandling([FromServices] IObservable<string> source)
            => new ObservableSelect<string, MyWidget>(source, value => value == null ? null! : new MyWidget { Name = value });

        public static IObservable<MyWidget?> TestNullHandlingNullable([FromServices] IObservable<string> source)
            => new ObservableSelect<string, MyWidget?>(source, value => value == null ? null : new MyWidget { Name = value });

        public static string NotSubscriptionField() => "testing";
    }

    private class MyWidget
    {
        [Id]
        public string Id => "SampleId";

        public string Name { get; set; } = null!;

        public string NameWithExtension(IResolveFieldContext context)
        {
            context.SetOutputExtension("alpha", Name);
            return Name;
        }

        public string NameMayThrowError()
        {
            if (Name == "custom")
                throw new InvalidOperationException("Custom error");
            if (Name == "application")
                throw new ApplicationException("Custom error");
            return Name;
        }

        public string? NameMayThrowErrorNullable()
        {
            if (Name == "custom")
                throw new InvalidOperationException("Custom error");
            if (Name == "application")
                throw new ApplicationException("Custom error");
            return Name;
        }

        public int GetCounter([FromUserContext] IDictionary<string, object?> userContext)
        {
            var testClass = (SubscriptionExecutionStrategyTests)userContext["testClass"]!;
            return testClass.Counter;
        }

        public bool ValidateServiceProvider(IResolveFieldContext context)
        {
            var provider = (IServiceProvider)context.UserContext["provider"]!;
            return object.ReferenceEquals(context.RequestServices, provider);
        }
    }
    #endregion

    #region - Test helper methods and classes -
    public class SampleListener : DocumentExecutionListenerBase
    {
        public SubscriptionExecutionStrategyTests TestClass = null!;

        public override async Task BeforeExecutionAsync(IExecutionContext context) => TestClass.Counter += 1;

        public override async Task AfterExecutionAsync(IExecutionContext context) => TestClass.Counter += 10;
    }

    public class SampleObserver : IObserver<ExecutionResult>
    {
        public ConcurrentQueue<ExecutionResult> Events { get; } = new();
        public void OnCompleted() => throw new NotImplementedException("OnCompleted should not occur");
        public void OnError(Exception error) => throw new NotImplementedException("OnError should not occur");
        public void OnNext(ExecutionResult value) => Events.Enqueue(value);
    }

    private Task<SubscriptionExecutionResult> ExecuteAsync(string query, Action<ExecutionOptions>? configureOptions = null)
        => ExecuteAsync(o =>
        {
            o.Query = query;
            configureOptions?.Invoke(o);
        });

    private async Task<SubscriptionExecutionResult> ExecuteAsync(Action<ExecutionOptions> configureOptions)
    {
        var services = new ServiceCollection();
        // todo: cleanup after PR #2999
        services.AddGraphQL(b => b
            .AddSchema<MySchema>()
            .ConfigureSchema((schema, provider) =>
            {
                schema.Query = provider.GetRequiredService<AutoRegisteringObjectGraphType<Query>>();
                schema.Subscription = provider.GetRequiredService<AutoRegisteringObjectGraphType<Subscription>>();
            })
            .AddExecutionStrategy<SubscriptionExecutionStrategy>(GraphQLParser.AST.OperationType.Subscription));
        services.AddSingleton<IObservable<string>>(Source);
        var provider = services.BuildServiceProvider();
        var executer = provider.GetService<IDocumentExecuter>();
        var options = new ExecutionOptions
        {
            Schema = provider.GetRequiredService<ISchema>(),
            RequestServices = provider,
            UnhandledExceptionDelegate = async context =>
            {
                if (context.Exception is InvalidOperationException)
                {
                    context.ErrorMessage = "Handled custom exception: " + context.Exception.Message;
                }
            },
        };
        configureOptions(options);
        var result = await executer.ExecuteAsync(options).ConfigureAwait(false);
        var subscriptionResult = result.ShouldBeOfType<SubscriptionExecutionResult>();
        if (subscriptionResult.Streams?.Count == 1)
        {
            Observer = new SampleObserver();
            Disposer = subscriptionResult.Streams.Single().Value.Subscribe(Observer);
        }
        else if (subscriptionResult.Streams?.Count > 1)
        {
            throw new Exception("More than one stream was returned");
        }
        return subscriptionResult;
    }

    private class ObservableSelect<TIn, TOut> : IObservable<TOut>
    {
        private readonly IObservable<TIn> _source;
        private readonly Func<TIn, TOut> _transform;

        public ObservableSelect(IObservable<TIn> source, Func<TIn, TOut> transform)
        {
            _source = source;
            _transform = transform;
        }

        public IDisposable Subscribe(IObserver<TOut> observer)
        {
            return _source.Subscribe(new Observer(observer, _transform));
        }

        private class Observer : IObserver<TIn>
        {
            private readonly Func<TIn, TOut> _transform;
            private readonly IObserver<TOut> _target;

            public Observer(IObserver<TOut> target, Func<TIn, TOut> transform)
            {
                _transform = transform;
                _target = target;
            }

            public void OnCompleted() => _target.OnCompleted();
            public void OnError(Exception error) => _target.OnError(error);
            public void OnNext(TIn value) => _target.OnNext(_transform(value));
        }
    }

    // todo: remove after PR #2999
    private class MySchema : Schema
    {
        public MySchema(IServiceProvider provider) : base(provider)
        {
        }

        protected override SchemaTypes CreateSchemaTypes() => new MySchemaTypes(this, this);
    }

    // todo: remove after PR #2999
    private class MySchemaTypes : SchemaTypes
    {
        public MySchemaTypes(ISchema schema, IServiceProvider provider) : base(schema, provider)
        {
        }

        protected override Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, List<(Type ClrType, Type GraphType)> typeMappings)
        {
            var type = base.GetGraphTypeFromClrType(clrType, isInputType, typeMappings);
            return type != null || isInputType ? type : typeof(AutoRegisteringObjectGraphType<>).MakeGenericType(clrType);
        }
    }
    #endregion
}
