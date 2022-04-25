using GraphQL.Types;
using GraphQL.MicrosoftDI;
using GraphQL.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Bugs;

public class Bug3100
{
    [Fact]
    public async Task OverrideAutoGraphTypeWithinDI()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema<Schema1>()
            .AddAutoClrMappings(true, true));
        services.AddTransient<Query1>();
        services.AddTransient(typeof(AutoRegisteringObjectGraphType<>), typeof(MyAutoGraphType<>));
        var provider = services.BuildServiceProvider();
        var executer = provider.GetRequiredService<IDocumentExecuter<ISchema>>();
        var serializer = new GraphQLSerializer();

        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();

        var result = await executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{class2{id}}",
            RequestServices = provider,
        }).ConfigureAwait(false);
        var actual = serializer.Serialize(result);
        actual.ShouldBe(@"{""data"":{""class2"":[{""id"":""test""}]}}");
    }

    private class MyAutoGraphType<T> : AutoRegisteringObjectGraphType<T>
    {
    }

    private class Schema1 : Schema
    {
        public Schema1(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<Query1>();
        }
    }

    private class Query1 : ObjectGraphType
    {
        public Query1()
        {
            Field(
                type: typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<Class2>>>>),
                name: "Class2",
                resolve: context => new Class2[] { new Class2() });
        }
    }

    private class Class2
    {
        public string Id { get; set; } = "test";
    }
}
