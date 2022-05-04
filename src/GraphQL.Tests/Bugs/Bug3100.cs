using GraphQL.Types;
using GraphQL.MicrosoftDI;
using GraphQL.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Bugs;

public class Bug3100
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OverrideAutoGraphTypeWithinDI(bool useMyAutoGraphType)
    {
        // set up service collection and default services
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema<Schema1>()
            .AddAutoClrMappings(true, true));
        services.AddTransient<Query1>();

        // this test works without the next line of code here
        if (useMyAutoGraphType)
            services.AddTransient(typeof(AutoRegisteringObjectGraphType<>), typeof(MyAutoGraphType<>));

        var provider = services.BuildServiceProvider();

        // verify that the query field resolver type maps to a properly initialized instance
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var queryType = schema.AllTypes["Query1"].ShouldBeOfType<Query1>();
        foreach (var field in queryType.Fields)
        {
            var resolvedType = field.ResolvedType.GetNamedType().ShouldBeAssignableTo<ObjectGraphType<Class2>>();
            if (useMyAutoGraphType)
                resolvedType.ShouldBeOfType<MyAutoGraphType<Class2>>();
            else
                resolvedType.ShouldBeOfType<AutoRegisteringObjectGraphType<Class2>>();
            var class2Type = schema.AllTypes["Class2"].ShouldNotBeNull();
            resolvedType.ShouldBe(class2Type);
            resolvedType.Fields.Find("id").ShouldNotBeNull();
        }

        // run the sample query
        var executer = provider.GetRequiredService<IDocumentExecuter<ISchema>>();
        var serializer = new GraphQLSerializer();
        var result = await executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{ class2 { id } }",
            RequestServices = provider,
        }).ConfigureAwait(false);
        var actual = serializer.Serialize(result);

        // verify the result
        actual.ShouldBe(@"{""data"":{""class2"":[{""id"":""test""}]}}");
    }

    private class MyAutoGraphType<T> : AutoRegisteringObjectGraphType<T>
    {
        // this class contains no code and should perform identical to the type it derives from
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
            Field(
                type: typeof(NonNullGraphType<ListGraphType<NonNullGraphType<GraphQLClrOutputTypeReference<Class2>>>>),
                name: "Class2b",
                resolve: context => new Class2[] { new Class2() });
        }
    }

    private class Class2
    {
        public string Id { get; set; } = "test";
    }
}
