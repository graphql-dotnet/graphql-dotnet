using GraphQL.NewtonsoftJson;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue4092
{
    [Fact]
    public async Task ResolveLists()
    {
        var query = new ObjectGraphType() { Name = "Query" };
        query.Field<ListGraphType<StringGraphType>>("test")
            .Description("Just a test")
            .Resolve(_ => new List<string> { "test1", "test2", "test3" });

        var schema = new Schema()
        {
            Query = query,
        };

        var result = await new DocumentExecuter().ExecuteAsync(_ =>
        {
            _.Schema = schema;
            _.Query = "{test}";
        });

        var resultJson = new GraphQLSerializer().Serialize(result);

        resultJson.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "test": ["test1", "test2", "test3"]
                }
            }
            """);
    }
}
