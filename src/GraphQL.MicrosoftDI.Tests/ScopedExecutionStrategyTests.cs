using System.Reactive.Subjects;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI.Tests;

public class ScopedExecutionStrategyTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VerifyScoped(bool scoped)
    {
        var observable = new Subject<Widget>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<Class1>();
        serviceCollection.AddGraphQL(b =>
        {
            b.AddAutoSchema<MyQuery>(s => s.WithSubscription<MySubscription>());
            if (scoped)
                b.AddScopedSubscriptionExecutionStrategy();
        });
        serviceCollection.AddSingleton(observable);
        var provider = serviceCollection.BuildServiceProvider();
        var executer = provider.GetRequiredService<IDocumentExecuter<ISchema>>();
        var result = await executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "subscription { widgets { value } }",
            RequestServices = provider,
        }).ConfigureAwait(false);
        var stream = result.Streams.ShouldNotBeNull().ShouldHaveSingleItem().Value;
        var results = new List<string>();
        stream.Subscribe(x =>
        {
            var str = (new GraphQLSerializer()).Serialize(x);
            lock (results)
                results.Add(str);
        });
        observable.OnNext(new Widget());
        observable.OnNext(new Widget());
        lock (results)
        {
            results.Count.ShouldBe(2);
            results[0].ShouldBe(@"{""data"":{""widgets"":{""value"":1}}}");
            results[1].ShouldBe(@"{""data"":{""widgets"":{""value"":" + (scoped ? 1 : 2) + "}}}");
        }
    }

    private class MyQuery
    {
        public static string Hero => "test";
    }

    private class MySubscription
    {
        public static IObservable<Widget> Widgets([FromServices] Subject<Widget> observable) => observable;
    }

    private class Widget
    {
        public int Value([FromServices] Class1 class1) => class1.GetValue();
    }

    private class Class1
    {
        private int _value;

        public int GetValue()
        {
            return Interlocked.Increment(ref _value);
        }
    }
}
