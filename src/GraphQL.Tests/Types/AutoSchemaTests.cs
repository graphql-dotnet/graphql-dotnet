using GraphQL.MicrosoftDI;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Types;

public class AutoSchemaTests
{
    [Theory]
    [InlineData("{hero}", @"{""data"":{""hero"":""Luke Skywalker""}}")]
    [InlineData(@"mutation {hero(name:""Darth Vader"")}", @"{""data"":{""hero"":""Darth Vader""}}")]
    [InlineData("{droids{name}}", @"{""data"":{""droids"":[{""name"":""R2D2""},{""name"":""C3PO""}]}}")]
    public async Task AutoSchemaWorks(string query, string expectedResult)
    {
        // sample configuration of DI
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<Query>(s => s.WithMutation<Mutation>())
            .AddSystemTextJson());
        var provider = services.BuildServiceProvider();

        // sample execution from DI
        var result = await provider.GetRequiredService<IDocumentExecuter>().ExecuteAsync(o =>
        {
            o.RequestServices = provider;
            o.Schema = provider.GetRequiredService<ISchema>();
            o.Query = query;
        }).ConfigureAwait(false);
        var resultString = provider.GetRequiredService<IGraphQLTextSerializer>().Serialize(result);
        resultString.ShouldBe(expectedResult);
    }

    // sample schema
    private class Query
    {
        public static string Hero => "Luke Skywalker";
        public static IEnumerable<Droid> Droids => new Droid[] { new Droid("R2D2"), new Droid("C3PO") };
    }

    private class Mutation
    {
        public static string Hero(string name) => name;
    }

    private record Droid(string Name);
}
