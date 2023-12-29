using GraphQL.Instrumentation;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.DataLoader.Tests;

public class ApolloTracingTests
{
    // verifies that the time spent executing the data loader is included with the
    // resolver duration, as the resolver has not 'completed' until the data loader completes
    //
    // note that (a) for batch data loaders, the time spent in the data loader is counted for all resolvers
    // waiting on that data loader, and (b) any delay caused by the execution engine is counted within the
    // resolver execution time
    //
    // perhaps it would be more ideal to separately record the data loader execution time within the apollo
    // tracing output (e.g. 'extensions.tracing.execution.dataloaders'), but this does not currently happen,
    // and the execution engine is not aware of which queued data loaders run as a single batch operation or not,
    // making implementation difficult
    [Fact]
    public async Task DataLoaderTimeRecorded()
    {
        var query = new ObjectGraphType() { Name = "Query" };
        query.Field<StringGraphType>("test")
            .Resolve(_ => new SimpleDataLoader<string>(async _ =>
            {
                await Task.Delay(2200); // 2.2 seconds
                return "Ok";
            }));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddGraphQL(b => b
            .AddSchema(provider => new Schema(provider) { Query = query })
            .AddSystemTextJson()
            .UseApolloTracing());
        var services = serviceCollection.BuildServiceProvider();
        var serializer = services.GetRequiredService<IGraphQLTextSerializer>();
        var executer = services.GetRequiredService<IDocumentExecuter>();
        var ret = await executer.ExecuteAsync(new()
        {
            Query = "{test}",
            RequestServices = services,
        });
        var apolloTrace = ret.Extensions["tracing"].ShouldBeOfType<ApolloTrace>();
        var resolverData = apolloTrace.Execution.Resolvers.Single();
        resolverData.Path.ShouldBe(new object[] { "test" });
        resolverData.Duration.ShouldBeGreaterThanOrEqualTo(2000000000L); // 2 seconds
    }
}
