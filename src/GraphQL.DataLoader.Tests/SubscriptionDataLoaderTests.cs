using System.Reactive.Linq;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.DataLoader.Tests;

public class SubscriptionDataLoaderTests
{
    [Fact]
    public async Task EnsureDataLoadersAreCleanedUp()
    {
        var parentRepository = new Dictionary<int, ClassParent>
        {
            { 1, new ClassParent() { Id = 1, Name = "Parent1", ChildId = 10 } },
            { 2, new ClassParent() { Id = 2, Name = "Parent2", ChildId = 11 } }
        };
        var childRepository = new Dictionary<int, ClassChild>
        {
            { 10, new ClassChild() { Id = 10, Name = "Child1" } },
            { 11, new ClassChild() { Id = 2, Name = "Child2" } }
        };

        var subject = new System.Reactive.Subjects.ReplaySubject<ClassParent>();

        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<StringGraphType>("test").Resolve(_ => "test");

        var childType = new ObjectGraphType<ClassChild>();
        childType.Field(x => x.Id);
        childType.Field(x => x.Name);

        var requestedCount = 0;

        var parentType = new ObjectGraphType<ClassParent>();
        parentType.Field(x => x.Id);
        parentType.Field(x => x.Name);
        parentType.Field("child", childType)
            .Resolve(ctx =>
            {
                var dataLoaderContextAccessor = ctx.RequestServices!.GetRequiredService<IDataLoaderContextAccessor>();
                var dataLoader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader<int, ClassChild>("GetChildById", (ids, _) =>
                {
                    ids.ShouldBe([10]);
                    requestedCount++;
                    var children = childRepository.Values.Where(x => ids.Contains(x.Id));
                    return Task.FromResult<IDictionary<int, ClassChild>>(children.ToDictionary(x => x.Id));
                });
                return dataLoader.LoadAsync(ctx.Source.ChildId);
            });

        var subscriptionType = new ObjectGraphType() { Name = "Subscription" };
        subscriptionType.Field("parents", parentType)
            .ResolveStream(context => subject);

        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema<ISchema>(provider =>
            {
                return new Schema(provider)
                {
                    Query = queryType,
                    Subscription = subscriptionType
                };
            })
            .AddDataLoader()
            .AddSystemTextJson()
        );
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();

        var executer = provider.GetRequiredService<IDocumentExecuter>();
        using var scope = provider.CreateScope();
        var result = await executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "subscription { parents { id name child { id name } } }",
            Schema = schema,
            RequestServices = scope.ServiceProvider,
        });

        result.Streams.ShouldNotBeNull().TryGetValue("parents", out var parentsOut).ShouldBeTrue();
        var collectedEvents = parentsOut.ToListObservable();

        // send the events
        subject.OnNext(parentRepository[1]);
        subject.OnNext(parentRepository[1]);
        subject.OnCompleted();

        // ensure that events were received
        collectedEvents.Count.ShouldBe(2);

        // ensure that the data loader was called twice -- once for each data event -- and not cached
        requestedCount.ShouldBe(2);
    }

    public class ClassParent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ChildId { get; set; }
    }

    public class ClassChild
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
