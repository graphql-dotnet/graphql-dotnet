#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable IDE0053 // Use expression body for lambda expressions

using GraphQL.Execution;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Subscription;

public class SubscriptionExecutionStrategyTests : IDisposable
{
    private SampleObservable<string> Source { get; } = new();
    private SampleObserver? Observer { get; set; }
    private IDisposable? SubscriptionObj { get; set; }
    private IDisposable? ServiceProviderDisposable { get; set; }

    public void Dispose() => ServiceProviderDisposable?.Dispose();

    [Fact]
    public async Task Basic()
    {
        var result = await ExecuteAsync("subscription { test }");
        result.ShouldBeSuccessful();
        SubscriptionObj.ShouldNotBeNull();
        result.Perf.ShouldBeNull();
        Source.Next("hello");
        Source.Next("testing");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "test": "hello" } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "test": "testing" } }""");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task BasicInt()
    {
        var result = await ExecuteAsync("subscription { testInt }");
        result.ShouldBeSuccessful();
        SubscriptionObj.ShouldNotBeNull();
        result.Perf.ShouldBeNull();
        Source.Next("1");
        Source.Next("2");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testInt": 1 } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testInt": 2 } }""");
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
        result2.ShouldBeSimilarTo("""{ "data": { "test": "hello" } }""");
        result2.Perf.ShouldBeNull();
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task InitialExtensions()
    {
        var result = await ExecuteAsync("subscription { testWithInitialExtensions }");
        result.ShouldBeSuccessful();
        result.Extensions.ShouldBeSimilarTo("""{ "alpha": "beta" }""");
        Source.Next("hello");
        Source.Next("testing");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testWithInitialExtensions": "hello" } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testWithInitialExtensions": "testing" } }""");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task InitialError()
    {
        var result = await ExecuteAsync("subscription { testWithInitialError(custom: false) }");
        result.ShouldNotBeSuccessful();
        result.Executed.ShouldBeTrue();
        result.ShouldBeSimilarTo("""{ "errors":[{ "message":"Could not resolve source stream for field \u0027testWithInitialError\u0027.","locations":[{ "line":1,"column":16}],"path":["testWithInitialError"],"extensions":{ "code":"APPLICATION","codes":["APPLICATION"]} }],"data":null}""");
    }

    [Fact]
    public async Task InitialCustomError()
    {
        var result = await ExecuteAsync("subscription { testWithInitialError(custom: true) }");
        result.ShouldNotBeSuccessful();
        result.Executed.ShouldBeTrue();
        result.ShouldBeSimilarTo("""{ "errors":[{ "message":"Handled custom exception: InitialException","locations":[{ "line":1,"column":16}],"path":["testWithInitialError"],"extensions":{ "code":"INVALID_OPERATION","codes":["INVALID_OPERATION"]} }],"data":null}""");
    }

    [Fact]
    public async Task InitialExecutionError()
    {
        var result = await ExecuteAsync("subscription { testWithExecutionError }");
        result.ShouldNotBeSuccessful();
        result.Executed.ShouldBeTrue();
        result.ShouldBeSimilarTo("""{"errors":[{"message":"Test error","locations":[{"line":1,"column":16}],"path":["testWithExecutionError"]}],"data":null}""");
    }

    [Fact]
    public async Task Widget()
    {
        var result = await ExecuteAsync("subscription { testComplex { id name } }");
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Next("testing");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "name": "hello" } } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "name": "testing" } } }""");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task Widget_WithExtension()
    {
        var result = await ExecuteAsync("subscription { testComplex { id nameWithExtension } }");
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Next("testing");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "nameWithExtension": "hello" } }, "extensions": { "alpha": "hello" } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "nameWithExtension": "testing" } }, "extensions": { "alpha": "testing" }  }""");
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
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "nameMayThrowError": "hello" } } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Handled custom exception: Custom error","locations":[{"line":1,"column":33}],"path":["testComplex","nameMayThrowError"],"extensions":{"code":"INVALID_OPERATION","codes":["INVALID_OPERATION"]}}],"data":null}""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "nameMayThrowError": "testing" } } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Error trying to resolve field \u0027nameMayThrowError\u0027.","locations":[{"line":1,"column":33}],"path":["testComplex","nameMayThrowError"],"extensions":{"code":"APPLICATION","codes":["APPLICATION"]}}],"data":null}""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "nameMayThrowError": "success" } } }""");
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
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "nameMayThrowErrorNullable": "hello" } } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Handled custom exception: Custom error","locations":[{"line":1,"column":33}],"path":["testComplex","nameMayThrowErrorNullable"],"extensions":{"code":"INVALID_OPERATION","codes":["INVALID_OPERATION"]}}],"data":{"testComplex":{"id":"SampleId","nameMayThrowErrorNullable":null}}}""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "nameMayThrowErrorNullable": "testing" } } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Error trying to resolve field \u0027nameMayThrowErrorNullable\u0027.","locations":[{"line":1,"column":33}],"path":["testComplex","nameMayThrowErrorNullable"],"extensions":{"code":"APPLICATION","codes":["APPLICATION"]}}],"data":{"testComplex":{"id":"SampleId","nameMayThrowErrorNullable":null}}}""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "nameMayThrowErrorNullable": "success" } } }""");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task Widget_SourceError()
    {
        // during handling of the `Source.Error` events below,
        // in this case the graphql execution has not started, so executed should be false
        // and "data" should not exist in the map of error events
        var result = await ExecuteAsync("subscription { testComplex { id name } }");
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Error(new ApplicationException("SourceError"));
        Source.Next("testing");
        Source.Error(new InvalidOperationException("SourceError"));
        Source.Next("success");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "name": "hello" } } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Response stream error for field \u0027testComplex\u0027.","locations":[{"line":1,"column":16}],"path":["testComplex"],"extensions":{"code":"APPLICATION","codes":["APPLICATION"]}}]}""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "name": "testing" } } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Handled custom exception: SourceError","locations":[{"line":1,"column":16}],"path":["testComplex"],"extensions":{"code":"INVALID_OPERATION","codes":["INVALID_OPERATION"]}}]}""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "id": "SampleId", "name": "success" } } }""");
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
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testNullHandling": { "id": "SampleId", "name": "hello" } } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Handled custom exception: Cannot return null for a non-null type. Field: testNullHandling, Type: MyWidget!.","locations":[{"line":1,"column":16}],"path":["testNullHandling"],"extensions":{"code":"INVALID_OPERATION","codes":["INVALID_OPERATION"]}}],"data":null}""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testNullHandling": { "id": "SampleId", "name": "testing" } } }""");
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
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testNullHandlingNullable": { "id": "SampleId", "name": "hello" } } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"data":{"testNullHandlingNullable":null}}""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testNullHandlingNullable": { "id": "SampleId", "name": "testing" } } }""");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task NotSubscriptionField()
    {
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<StringGraphType>("dummy");
        var subscriptionType = new ObjectGraphType() { Name = "Subscription" };
        subscriptionType.Field<StringGraphType>("notSubscriptionField");
        var schema = new Schema()
        {
            Query = queryType,
            Subscription = subscriptionType
        };
        Should.Throw<InvalidOperationException>(() => schema.Initialize())
            .Message.ShouldBe("The field 'notSubscriptionField' of the subscription root type 'Subscription' must have StreamResolver set.");
    }

    public int Counter = 0;

    [Fact]
    public async Task DocumentListeners_and_UserContext_works()
    {
        var listener = new SampleListener(_ => Counter += 1, _ => Counter += 10);
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
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": {"name": "hello", "getCounter": 12 } } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": {"name": "testing", "getCounter": 23 } } }""");
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
        Source.Next("hello");
        Source.Next("testing");
        cts.Cancel();
        Source.Next("success");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "test": "hello" } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "test": "testing" } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "test": "success" } }""");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task NoEventsAfterSourceDisconnected()
    {
        using var cts = new CancellationTokenSource();
        var result = await ExecuteAsync("subscription { test }", o => o.CancellationToken = cts.Token);
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Source.Next("testing");
        SubscriptionObj!.Dispose();
        Source.Next("should not happen");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "test": "hello" } }""");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "test": "testing" } }""");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task CheckServiceProvider()
    {
        var result = await ExecuteAsync("subscription { testComplex { validateServiceProvider } }", o => o.UserContext["provider"] = o.RequestServices);
        result.ShouldBeSuccessful();
        Source.Next("hello");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{ "data": { "testComplex": { "validateServiceProvider": true } } }""");
    }

    [Fact]
    public void SubscriptionExecutionStrategy_NullConstructorThrow()
    {
        Should.Throw<ArgumentNullException>(() => new SubscriptionExecutionStrategy(null!));
    }

    [Fact]
    public async Task CancelWithinErrorHandler_Rethrows()
    {
        var result = await ExecuteAsync("subscription { testComplex { nameMayThrowError } }", o =>
        {
            o.UnhandledExceptionDelegate = async _ => throw new InvalidOperationException();
        });
        result.ShouldBeSuccessful();
        Source.Next("custom");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Unhandled error of type InvalidOperationException"}]}""");
    }

    [Fact]
    public async Task CancelWithinDocumentExecuter_Returns_DoesNotHandle()
    {
        bool throwNow = false;
        var result = await ExecuteAsync("subscription { test }", o =>
        {
            o.Listeners.Add(new SampleListener(
                context =>
                {
                    if (throwNow)
                    {
                        SubscriptionObj!.Dispose();
                        context.CancellationToken.ThrowIfCancellationRequested();
                        throw new NotSupportedException("Should not happen");
                    }
                },
                _ => { }));
            o.UnhandledExceptionDelegate = context => throw new NotSupportedException("Should not happen");
        });
        result.ShouldBeSuccessful();
        throwNow = true;
        Source.Next("test");
        Observer.ShouldHaveNoMoreResults();
    }

    [Fact]
    public async Task ThrowExceptionWithinListener_Before_Returns()
    {
        bool throwNow = false;
        var result = await ExecuteAsync("subscription { test }", o =>
        {
            o.Listeners.Add(new SampleListener(
                context =>
                {
                    if (throwNow)
                    {
                        throw new InvalidOperationException("Test exception");
                    }
                },
                _ => { }));
        });
        result.ShouldBeSuccessful();
        throwNow = true;
        Source.Next("test");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Handled custom exception: Test exception","locations":[{"line":1,"column":16}],"path":["test"],"extensions":{"code":"INVALID_OPERATION","codes":["INVALID_OPERATION"]}}]}""");
    }

    [Fact]
    public async Task ThrowErrorWithinListener_Before_Returns()
    {
        bool throwNow = false;
        var result = await ExecuteAsync("subscription { test }", o =>
        {
            o.Listeners.Add(new SampleListener(
                context =>
                {
                    if (throwNow)
                    {
                        throw new ExecutionError("Test error");
                    }
                },
                _ => { }));
            o.UnhandledExceptionDelegate = async context => context.Exception = new ExecutionError("Should not happen");
        });
        result.ShouldBeSuccessful();
        throwNow = true;
        Source.Next("test");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Test error","locations":[{"line":1,"column":16}],"path":["test"]}]}""");
    }

    [Fact]
    public async Task ThrowExceptionWithinListener_After_ReturnsWithData()
    {
        bool throwNow = false;
        var result = await ExecuteAsync("subscription { test }", o =>
        {
            o.Listeners.Add(new SampleListener(
                _ => { },
                context =>
                {
                    if (throwNow)
                    {
                        throw new InvalidOperationException("Test exception");
                    }
                }));
        });
        result.ShouldBeSuccessful();
        throwNow = true;
        Source.Next("test");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Handled custom exception: Test exception","locations":[{"line":1,"column":16}],"path":["test"],"extensions":{"code":"INVALID_OPERATION","codes":["INVALID_OPERATION"]}}],"data":{"test":"test"}}""");
    }

    [Fact]
    public async Task ThrowErrorWithinListener_After_ReturnsWithData()
    {
        bool throwNow = false;
        var result = await ExecuteAsync("subscription { test }", o =>
        {
            o.Listeners.Add(new SampleListener(
                _ => { },
                context =>
                {
                    if (throwNow)
                    {
                        throw new ExecutionError("Test error");
                    }
                }));
            o.UnhandledExceptionDelegate = async context => context.Exception = new ExecutionError("Should not happen");
        });
        result.ShouldBeSuccessful();
        throwNow = true;
        Source.Next("test");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Test error","locations":[{"line":1,"column":16}],"path":["test"]}],"data":{"test":"test"}}""");
    }

    [Fact]
    public async Task ErrorWithinListener_Before_ReturnsBeforeExecuting()
    {
        bool errorNow = false;
        var result = await ExecuteAsync("subscription { test }", o =>
        {
            o.Listeners.Add(new SampleListener(
                context =>
                {
                    if (errorNow)
                    {
                        context.Errors.Add(new ExecutionError("Test error"));
                    }
                },
                _ => { }));
        });
        result.ShouldBeSuccessful();
        errorNow = true;
        Source.Next("test");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Test error"}]}""");
    }

    [Fact]
    public async Task ErrorWithinListener_After_ReturnsAsExecuted()
    {
        bool errorNow = false;
        var result = await ExecuteAsync("subscription { test }", o =>
        {
            o.Listeners.Add(new SampleListener(
                _ => { },
                context =>
                {
                    if (errorNow)
                    {
                        context.Errors.Add(new ExecutionError("Test error"));
                    }
                }));
        });
        result.ShouldBeSuccessful();
        errorNow = true;
        Source.Next("test");
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"errors":[{"message":"Test error"}],"data":{"test":"test"}}""");
    }

    [Fact]
    public async Task CancellationDuringSubscriptionShouldThrow()
    {
        var cts = new CancellationTokenSource();
        await Should.ThrowAsync<OperationCanceledException>(async () => await ExecuteAsync("subscription { testWithCancellation }", o =>
        {
            o.CancellationToken = cts.Token;
            o.UserContext["cts"] = cts;
        }));
    }

    [Fact]
    public async Task SubscriptionMethodNullShouldThrow()
    {
        var result = await ExecuteAsync("subscription { testObservableNull }");
        result.ShouldNotBeSuccessful();
        result.ShouldBeSimilarTo("""{"errors":[{"message":"Handled custom exception: No event stream returned for field \u0027testObservableNull\u0027.","locations":[{"line":1,"column":16}],"path":["testObservableNull"],"extensions":{"code":"INVALID_OPERATION","codes":["INVALID_OPERATION"]}}],"data":null}""");
    }

    [Fact]
    public async Task MultipleSubscriptionsShouldThrow()
    {
        var result = await ExecuteAsync("subscription { test testWithInitialExtensions }", null, false);
        result.ShouldNotBeSuccessful();
        result.ShouldBeSimilarTo("""{"errors":[{"message":"Anonymous Subscription must have exactly one root field and that field must not be an introspection field.","locations":[{"line":1,"column":21}],"extensions":{"code":"SINGLE_ROOT_FIELD_SUBSCRIPTIONS","codes":["SINGLE_ROOT_FIELD_SUBSCRIPTIONS"],"number":"5.2.3.1"}}]}""");
    }

    [Fact]
    public async Task MultipleSubscriptions_NoValidation()
    {
        var result = await ExecuteAsync("subscription { test testWithInitialExtensions }", o => o.ValidationRules = new GraphQL.Validation.IValidationRule[] { }, false);
        result.Streams.ShouldNotBeNull().Count.ShouldBe(2);
        result.Data.ShouldBeNull();
        result.Errors.ShouldBeNull();
        result.Executed.ShouldBeTrue();
    }

    private static readonly AsyncLocal<string?> _asyncLocal = new();

    [Fact]
    public async Task NetExecutionContextIsCarriedOver()
    {
        // This verifies that the System.Threading.ExecutionContext is preserved during data events after the initial subscription.
        // This means that AsyncLocal instances will contain the values they had during the initial subscription during the
        // execution of field resolvers. As HttpContextAccessor is based on AsyncLocal, this allows subscription field resolvers
        // that reference IHttpContextAccessor.HttpContext to access the correct HttpContext of the connected client, rather
        // than the HttpContext of the event source, which would be unexpected. Of course, any processing before it reaches
        // GraphQL would be run in the execution context of the event source.

        // simulate the ExecutionContext of a connecting client and execute a subscription
        _asyncLocal.Value.ShouldBeNull();
        await ConnectClientAsync();

        // simulate the ExecutionContext of an event source and raise an event
        _asyncLocal.Value.ShouldBeNull();
        await RaiseEventAsync();

        // verify that the connected client has recieved an event with its own context applied during the execution of the resolvers
        _asyncLocal.Value.ShouldBeNull();
        Observer.ShouldHaveResult().ShouldBeSimilarTo("""{"data":{"testComplex":{"asyncLocalValue":"client"}}}""");

        async Task ConnectClientAsync()
        {
            _asyncLocal.Value = "client";
            await ExecuteAsync("subscription { testComplex { asyncLocalValue } }");
        }

        async Task RaiseEventAsync()
        {
            _asyncLocal.Value = "source";
            Source.Next("hello");
        }
    }

    #region - Schema -
    private class Query
    {
        public static string Hero => "hi";
    }

    private class Subscription
    {
        public static IObservable<string> Test([FromServices] IObservable<string> source) => source;

        public static IObservable<int> TestInt([FromServices] IObservable<string> source)
            => new ObservableSelect<string, int>(source, value => int.Parse(value));

        public static IObservable<string> TestWithInitialExtensions(IResolveFieldContext context, [FromServices] IObservable<string> source)
        {
            context.SetOutputExtension("alpha", "beta");
            return source;
        }

        public static IObservable<string> TestWithCancellation(IResolveFieldContext context)
        {
            ((CancellationTokenSource)context.UserContext["cts"]!).Cancel();
            context.CancellationToken.ThrowIfCancellationRequested();
            throw new Exception("Should not occur");
        }

        public static IObservable<string> TestWithInitialError(bool custom)
            => throw (custom ? new InvalidOperationException("InitialException") : new ApplicationException("InitialException"));

        public static IObservable<string> TestWithExecutionError() => throw new ExecutionError("Test error");

        public static IObservable<MyWidget> TestComplex([FromServices] IObservable<string> source)
            => new ObservableSelect<string, MyWidget>(source, value => new MyWidget { Name = value });

        public static IObservable<MyWidget> TestNullHandling([FromServices] IObservable<string> source)
            => new ObservableSelect<string, MyWidget>(source, value => value == null ? null! : new MyWidget { Name = value });

        public static IObservable<MyWidget?> TestNullHandlingNullable([FromServices] IObservable<string> source)
            => new ObservableSelect<string, MyWidget?>(source, value => value == null ? null : new MyWidget { Name = value });

        public static IObservable<string> TestObservableNull() => null!;

        //public static string NotSubscriptionField() => "testing";
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

        public string? AsyncLocalValue()
        {
            return _asyncLocal.Value;
        }
    }
    #endregion

    #region - Test helper methods and classes -
    public class SampleListener : DocumentExecutionListenerBase
    {
        private readonly Action<IExecutionContext> _beforeFunc;
        private readonly Action<IExecutionContext> _afterFunc;

        public SampleListener(Action<IExecutionContext> beforeFunc, Action<IExecutionContext> afterFunc)
        {
            _beforeFunc = beforeFunc;
            _afterFunc = afterFunc;
        }

        public override async Task BeforeExecutionAsync(IExecutionContext context) => _beforeFunc(context);

        public override async Task AfterExecutionAsync(IExecutionContext context) => _afterFunc(context);
    }

    private Task<ExecutionResult> ExecuteAsync(string query, Action<ExecutionOptions>? configureOptions = null, bool validateResponse = true)
        => ExecuteAsync(
            o =>
            {
                o.Query = query;
                configureOptions?.Invoke(o);
            },
            validateResponse);

    private async Task<ExecutionResult> ExecuteAsync(Action<ExecutionOptions> configureOptions, bool validateResponse = true)
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<Query>(s => s.WithSubscription<Subscription>()));
        services.AddSingleton<IObservable<string>>(Source);
        var provider = services.BuildServiceProvider();
        if (ServiceProviderDisposable != null)
            throw new InvalidOperationException("Cannot run ExecuteAsync twice within one test");
        ServiceProviderDisposable = provider; // only disposed after execution is complete
        var executer = provider.GetRequiredService<IDocumentExecuter>();
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
        if (validateResponse)
        {
            if (result.Streams?.Count == 1)
            {
                Observer = new SampleObserver();
                SubscriptionObj = result.Streams.Single().Value.Subscribe(Observer);
            }
            else if (result.Streams?.Count > 1)
            {
                throw new Exception("More than one stream was returned");
            }
        }
        return result;
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
    #endregion
}
