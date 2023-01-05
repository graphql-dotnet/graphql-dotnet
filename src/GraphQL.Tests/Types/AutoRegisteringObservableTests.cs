#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS8425 // Async-iterator member has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed

using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GraphQL.Tests.Types;

public class AutoRegisteringObservableTests
{
    private ISourceStreamResolver GetResolver(string fieldName)
    {
        var graph = new AutoRegisteringObjectGraphType<TestClass>();
        var field = graph.Fields.Find(fieldName).ShouldNotBeNull();
        return field.StreamResolver.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(nameof(TestClass.ListOfStrings1))]
    [InlineData(nameof(TestClass.ListOfStrings2))]
    [InlineData(nameof(TestClass.ListOfStrings3))]
    [InlineData(nameof(TestClass.ListOfStrings4))]
    [InlineData(nameof(TestClass.ListOfStrings5))]
    public async Task Observable_Basic(string fieldName)
    {
        var streamResolver = GetResolver(fieldName);
        var observable = await streamResolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false);
        observable.ToEnumerable().ShouldBe(new string[] { "a", "b", "c" });
    }

    [Theory]
    [InlineData(nameof(TestClass.ReturnError1))]
    [InlineData(nameof(TestClass.ReturnError2))]
    [InlineData(nameof(TestClass.ReturnError3))]
    public async Task Observable_WithError(string fieldName)
    {
        var streamResolver = GetResolver(fieldName);
        var observable = await streamResolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false);
        Should.Throw<Exception>(() => observable.ToEnumerable().ToList()).Message.ShouldBe("sample error");
    }

    [Theory]
    [InlineData(nameof(TestClass.InitialError1))]
    [InlineData(nameof(TestClass.InitialError2))]
    [InlineData(nameof(TestClass.InitialError3))]
    public async Task Observable_InitialError(string fieldName)
    {
        var streamResolver = GetResolver(fieldName);
        var e = await Should.ThrowAsync<Exception>(async () => await streamResolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false)).ConfigureAwait(false);
        e.Message.ShouldBe("initial error");
    }

    [Theory]
    [InlineData(nameof(TestClass.AsyncStrings1))]
    [InlineData(nameof(TestClass.AsyncStrings2))]
    [InlineData(nameof(TestClass.AsyncStrings3))]
    public async Task AsyncEnumerable_Basic(string fieldName)
    {
        var streamResolver = GetResolver(fieldName);
        var observable = await streamResolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false);
        observable.ToEnumerable().ShouldBe(new string[] { "d", "e", "f" });
    }

    [Theory]
    [InlineData(nameof(TestClass.AsyncWithError1))]
    [InlineData(nameof(TestClass.AsyncWithError2))]
    [InlineData(nameof(TestClass.AsyncWithError3))]
    public async Task AsyncEnumerable_WithError(string fieldName)
    {
        var streamResolver = GetResolver(fieldName);
        var observable = await streamResolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false);
        Should.Throw<Exception>(() => observable.ToEnumerable().ToList()).Message.ShouldBe("sample error 2");
    }

    [Theory]
    [InlineData(nameof(TestClass.AsyncInitialError1))]
    [InlineData(nameof(TestClass.AsyncInitialError2))]
    [InlineData(nameof(TestClass.AsyncInitialError3))]
    public async Task AsyncEnumerable_InitialError(string fieldName)
    {
        var streamResolver = GetResolver(fieldName);
        var e = await Should.ThrowAsync<Exception>(async () => await streamResolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false)).ConfigureAwait(false);
        e.Message.ShouldBe("initial error 2");
    }

    [Fact]
    public async Task AsyncEnumerable_Token1()
    {
        var streamResolver = GetResolver(nameof(TestClass.AsyncWithToken1));
        var context = new ResolveFieldContext();
        context.CancellationToken.CanBeCanceled.ShouldBeFalse();
        var observable = await streamResolver.ResolveAsync(context).ConfigureAwait(false);
        observable.ToEnumerable().ShouldBe(new string[] { "ok1" });
    }

    [Fact]
    public async Task AsyncEnumerable_Token2()
    {
        var streamResolver = GetResolver(nameof(TestClass.AsyncWithToken2));
        var context = new ResolveFieldContext();
        context.CancellationToken.CanBeCanceled.ShouldBeFalse();
        var observable = await streamResolver.ResolveAsync(context).ConfigureAwait(false);
        observable.ToEnumerable().ShouldBe(new string[] { "ok2" });
    }

    [Theory]
    [InlineData(nameof(TestClass.AsyncCancelToken1), true)]
    [InlineData(nameof(TestClass.AsyncCancelToken1), false)]
    [InlineData(nameof(TestClass.AsyncCancelToken2), true)]
    [InlineData(nameof(TestClass.AsyncCancelToken2), false)]
    public async Task AsyncEnumerable_Cancel(string fieldName, bool cancelViaDispose)
    {
        var streamResolver = GetResolver(fieldName);
        var cts = new CancellationTokenSource();
        var context = new ResolveFieldContext() { CancellationToken = cts.Token };
        var observable = await streamResolver.ResolveAsync(context).ConfigureAwait(false);
        var mockObserver = new Mock<IObserver<object>>(MockBehavior.Strict);
        var tcs = new TaskCompletionSource<bool>();
        mockObserver.Setup(x => x.OnNext("canceled")).Callback(() => tcs.SetResult(true)).Verifiable();
        var disposer = observable.Subscribe(mockObserver.Object);
        await Task.Delay(500).ConfigureAwait(false);
        if (cancelViaDispose)
            disposer.Dispose();
        else
            cts.Cancel();
        await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
        mockObserver.Verify();
        mockObserver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Scoped_Basic()
    {
        var streamResolver = GetResolver(nameof(TestClass.Scoped));
        // verify that the stream resolver can be run multiple times
        for (int i = 0; i < 2; i++)
        {
            var services = new ServiceCollection();
            services.AddScoped<ServiceTestClass>();
            var context = new ResolveFieldContext()
            {
                RequestServices = services.BuildServiceProvider(),
                OutputExtensions = new Dictionary<string, object>(),
            };
            // the ServiceTestClass will be created during the call to ResolveAsync
            var observable = await streamResolver.ResolveAsync(context).ConfigureAwait(false);
            // verify that the ServiceTestClass is not disposed while it is being iterated
            observable.ToEnumerable().ShouldBe(new string[] { "1", "2" });
            // verify that the ServiceTestClass is disposed after the iteration is complete
            var service = context.OutputExtensions["service"].ShouldBeOfType<ServiceTestClass>();
            service.Disposed.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Scoped_DisposeAfterException()
    {
        var streamResolver = GetResolver(nameof(TestClass.ScopedError));
        // verify that the stream resolver can be run multiple times
        for (int i = 0; i < 2; i++)
        {
            var services = new ServiceCollection();
            services.AddScoped<ServiceTestClass>();
            var inService = new ServiceTestClass();
            var context = new ResolveFieldContext()
            {
                RequestServices = services.BuildServiceProvider(),
                InputExtensions = new Dictionary<string, object>() { { "in", inService } },
                OutputExtensions = new Dictionary<string, object>(),
            };
            // the ServiceTestClass will be created during the call to ResolveAsync
            var observable = await streamResolver.ResolveAsync(context).ConfigureAwait(false);
            // verify that the ServiceTestClass is not disposed while it is being iterated
            Should.Throw<Exception>(() => observable.ToEnumerable().ToList()).Message.ShouldBe("something");
            // verify that the finalizer has run within the async iterator
            inService.Disposed.ShouldBeTrue();
            // verify that the ServiceTestClass is disposed after the iteration is complete
            var service = context.OutputExtensions["service"].ShouldBeOfType<ServiceTestClass>();
            service.Disposed.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task ResolveFieldContext_Passthrough()
    {
        var options = new ExecutionOptions
        {
            Query = "subscription($n:Int!) { resolveFieldContextPassThrough(num:$n) @skip(if:false) }",
            RequestServices = new ServiceCollection().BuildServiceProvider(),
            Schema = new Schema
            {
                Subscription = new AutoRegisteringObjectGraphType<TestClass>(),
            },
            Variables = new Dictionary<string, object>() { { "n", 2 } }.ToInputs(),
            Extensions = new Dictionary<string, object>() { { "ext", 20 } }.ToInputs(),
            EnableMetrics = true,
            Root = "root",
            User = new ClaimsPrincipal(new ClaimsIdentity("test")),
            ThrowOnUnhandledException = true,
        };
        options.UserContext["key1"] = "value1";
        var ret = await new DocumentExecuter().ExecuteAsync(options).ConfigureAwait(false);
        ret.Executed.ShouldBeTrue();
        var stream = ret.Streams.ShouldHaveSingleItem();
        stream.Key.ShouldBe("resolveFieldContextPassThrough");
        var returnedData = stream.Value.ToEnumerable().Select(result => new SystemTextJson.GraphQLSerializer().Serialize(result)).ToList();
        returnedData.ShouldHaveSingleItem().ShouldBeCrossPlatJson("""{"data":{"resolveFieldContextPassThrough":"1"}}""");
    }

    public class TestClass
    {
        public static IObservable<string> ListOfStrings1() => new string[] { "a", "b", "c" }.ToObservable();
        public static async Task<IObservable<string>> ListOfStrings2() => new string[] { "a", "b", "c" }.ToObservable();
        public static async ValueTask<IObservable<string>> ListOfStrings3() => new string[] { "a", "b", "c" }.ToObservable();
        [OutputType(typeof(StringGraphType))]
        public static IObservable<object> ListOfStrings4() => new string[] { "a", "b", "c" }.ToObservable();
        [OutputType(typeof(StringGraphType))]
        public static async ValueTask<IObservable<string>> ListOfStrings5() => new string[] { "a", "b", "c" }.ToObservable();

        public static IObservable<string> ReturnError1() => Observable.Throw<string>(new Exception("sample error"));
        public static async Task<IObservable<string>> ReturnError2() => Observable.Throw<string>(new Exception("sample error"));
        public static async ValueTask<IObservable<string>> ReturnError3() => Observable.Throw<string>(new Exception("sample error"));

        public static IObservable<string> InitialError1() => throw new Exception("initial error");
        public static async Task<IObservable<string>> InitialError2() => throw new Exception("initial error");
        public static async ValueTask<IObservable<string>> InitialError3() => throw new Exception("initial error");

        public static async IAsyncEnumerable<string> AsyncStrings1()
        {
            yield return "d";
            yield return "e";
            yield return "f";
        }
        public static async Task<IAsyncEnumerable<string>> AsyncStrings2() => AsyncStrings1();
        public static async ValueTask<IAsyncEnumerable<string>> AsyncStrings3() => AsyncStrings1();

        public static async IAsyncEnumerable<string> AsyncWithError1()
        {
            yield return "g";
            throw new Exception("sample error 2");
        }
        public static async Task<IAsyncEnumerable<string>> AsyncWithError2() => AsyncWithError1();
        public static async ValueTask<IAsyncEnumerable<string>> AsyncWithError3() => AsyncWithError1();

        public static IAsyncEnumerable<string> AsyncInitialError1() => throw new Exception("initial error 2");
        public static async Task<IAsyncEnumerable<string>> AsyncInitialError2() => throw new Exception("initial error 2");
        public static async ValueTask<IAsyncEnumerable<string>> AsyncInitialError3() => throw new Exception("initial error 2");

        public static async IAsyncEnumerable<string> AsyncWithToken1(IResolveFieldContext context, CancellationToken token)
        {
            token.CanBeCanceled.ShouldBeTrue();
            token.ShouldBe(context.CancellationToken);
            yield return "ok1";
        }

        public static async IAsyncEnumerable<string> AsyncWithToken2(IResolveFieldContext context, [EnumeratorCancellation] CancellationToken token)
        {
            token.CanBeCanceled.ShouldBeTrue();
            token.ShouldBe(context.CancellationToken);
            yield return "ok2";
        }

        public static async IAsyncEnumerable<string> AsyncCancelToken1(IResolveFieldContext context, CancellationToken token)
        {
            context.CancellationToken.ShouldBe(token);
            string response = "ok";
            try
            {
                await Task.Delay(30000, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                response = "canceled";
            }
            yield return response;
        }

        public static async IAsyncEnumerable<string> AsyncCancelToken2(IResolveFieldContext context, [EnumeratorCancellation] CancellationToken token)
        {
            context.CancellationToken.ShouldBe(token);
            string response = "ok";
            try
            {
                await Task.Delay(30000, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                response = "canceled";
            }
            yield return response;
        }

        [Scoped]
        public static async IAsyncEnumerable<string> Scoped(IResolveFieldContext context, [FromServices] ServiceTestClass serviceTestClass)
        {
            serviceTestClass.Disposed.ShouldBeFalse();
            yield return "1";
            await Task.Delay(500).ConfigureAwait(false);
            serviceTestClass.Disposed.ShouldBeFalse();
            yield return "2";
            context.OutputExtensions["service"] = serviceTestClass;
        }

        [Scoped]
        public static async IAsyncEnumerable<string> ScopedError(IResolveFieldContext context, [FromServices] ServiceTestClass serviceTestClass)
        {
            var inService = context.InputExtensions["in"].ShouldBeOfType<ServiceTestClass>();
            context.OutputExtensions["service"] = serviceTestClass;
            inService.Disposed.ShouldBeFalse();
            serviceTestClass.Disposed.ShouldBeFalse();
            yield return "3";
            try
            {
                throw new Exception("something");
            }
            finally
            {
                inService.Dispose();
            }
        }

        [Scoped]
        public static async IAsyncEnumerable<string> ResolveFieldContextPassThrough(int num, IResolveFieldContext context)
        {
            num.ShouldBe(2);
            context.Document.Source.ShouldBe("subscription($n:Int!) { resolveFieldContextPassThrough(num:$n) @skip(if:false) }");
            context.GetArgument<int>("num").ShouldBe(2);
            context.Arguments["num"].Source.ShouldBe(ArgumentSource.Variable);
            context.Variables.ValueFor("n", out var argValue).ShouldBeTrue();
            argValue.Value.ShouldBe(2);
            context.InputExtensions["ext"].ShouldBe(20);
            context.ArrayPool.ShouldNotBeNull();
            context.Directives["skip"].ShouldNotBeNull().Arguments["if"].Value.ShouldBe(false);
            context.Errors.ShouldNotBeNull();
            context.FieldAst.Name.Value.ShouldBe("resolveFieldContextPassThrough");
            context.FieldDefinition.Name.ShouldBe("resolveFieldContextPassThrough");
            context.Metrics.Enabled.ShouldBeTrue();
            context.Operation.Operation.ShouldBe(GraphQLParser.AST.OperationType.Subscription);
            context.Parent.ShouldBeNull();
            context.ParentType.Name.ShouldBe("TestClass");
            context.Path.ShouldBe(new object[] { "resolveFieldContextPassThrough" });
            context.ResponsePath.ShouldBe(new object[] { "resolveFieldContextPassThrough" });
            context.RootValue.ShouldBe("root");
            context.Schema.Subscription.Name.ShouldBe("TestClass");
            context.Source.ShouldBe("root");
            context.SubFields.ShouldBeNull();
            context.User.Identity.AuthenticationType.ShouldBe("test");
            context.UserContext["key1"].ShouldBe("value1");
            yield return "1";
        }
    }

    public class ServiceTestClass : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
}
