using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Bugs;

public class Bug4083
{
    [Fact]
    public async Task FragmentsCombineCorrectlyWithVariables()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<Query>()
        );
        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var ret = await schema.ExecuteAsync(o =>
        {
            o.Query = """
                query heroQuery($id: Int!) {
                    ...fragment1
                    ...fragment2
                }

                fragment fragment1 on Query {
                  hero(id: $id) {
                    id
                  }
                }

                fragment fragment2 on Query {
                  hero(id: $id) {
                    name
                  }
                }
                """;
            o.Variables = """{"id":1}""".ToInputs();
        });
        ret.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "hero": {
                        "id": 1,
                        "name": "John Doe"
                    }
                }
            }
            """);
    }

    public class Query
    {
        public static Class1 Hero(int id) => new Class1 { Id = id, Name = "John Doe" };
    }

    public class Class1
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
