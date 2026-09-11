using System.Text;
using GraphQL.SystemTextJson;
using GraphQL.Types;

namespace GraphQL.Tests.Execution;

public class ResolveFieldContextParentTests
{
    [Fact]
    public async Task Parent_should_skip_list_item_node()
    {
        var schema = new Schema
        {
            Query = new ParentQuery(),
        };

        var actual = await schema.ExecuteAsync(options => options.Query = """
            query {
              multi {
                single { parentTypeTree }
                inner {
                  single { parentTypeTree }
                  inner {
                    single { parentTypeTree }
                  }
                }
                list { parentTypeTree }
              }
            }
            """);

        actual.ShouldBeCrossPlatJson("""
            {
              "data": {
                "multi": {
                  "single": {
                    "parentTypeTree": "Ping - PingMulti - Query"
                  },
                  "inner": {
                    "single": {
                      "parentTypeTree": "Ping - PingMulti - PingMulti - Query"
                    },
                    "inner": {
                      "single": {
                        "parentTypeTree": "Ping - PingMulti - PingMulti - PingMulti - Query"
                      }
                    }
                  },
                  "list": [
                    {
                      "parentTypeTree": "Ping - PingMulti - Query"
                    },
                    {
                      "parentTypeTree": "Ping - PingMulti - Query"
                    }
                  ]
                }
              }
            }
            """);
    }

    public class ParentQuery : ObjectGraphType
    {
        public ParentQuery()
        {
            Name = "Query";

            Field<NonNullGraphType<PingMultiType>>("multi")
                .Resolve(_ => new PingMulti());
        }
    }

    public class PingMultiType : ObjectGraphType<PingMulti>
    {
        public PingMultiType()
        {
            Name = "PingMulti";

            Field<NonNullGraphType<PingMultiType>>("inner")
                .Resolve(_ => new PingMulti());

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<PingType>>>>("list")
                .Resolve(_ => new List<Ping> { new(), new() });

            Field<NonNullGraphType<PingType>>("single")
                .Resolve(_ => new Ping());
        }
    }

    public class PingType : ObjectGraphType<Ping>
    {
        public PingType()
        {
            Name = "Ping";

            Field<StringGraphType>("parentTypeTree")
                .Resolve(context =>
                {
                    var builder = new StringBuilder();
                    IResolveFieldContext? current = context;

                    while (current != null)
                    {
                        if (builder.Length > 0)
                            builder.Append(" - ");

                        builder.Append(current.ParentType?.Name ?? "%null");
                        current = current.Parent;
                    }

                    return builder.ToString();
                });
        }
    }

    public sealed record PingMulti;

    public sealed record Ping;
}
