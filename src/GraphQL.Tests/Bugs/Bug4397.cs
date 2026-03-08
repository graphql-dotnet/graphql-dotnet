using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug4397
{
    [Fact]
    public async Task Union_field_named_same_as_fragment_with_aliased_field_with_arg()
    {
        var schema = new Schema { Query = new Bug4397Query() };
        schema.RegisterType<Bug4397Type1>();

        var result = await schema.ExecuteAsync(o => o.Query = """
            {
              union1 {
                __typename
                ...frag1
              }
            }

            fragment frag1 on Union1 {
              ... on Type1 {
                field1
                field2: field1(arg1: "abc")
              }
            }
            """);

        result.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "union1": {
                        "__typename": "Type1",
                        "field1": "field1value",
                        "field2": "abc"
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task Union_field_with_inline_fragment_on_union_type_with_aliased_field_with_arg()
    {
        var schema = new Schema { Query = new Bug4397Query() };
        schema.RegisterType<Bug4397Type1>();

        var result = await schema.ExecuteAsync(o => o.Query = """
            {
              union1 {
                __typename
                ... on Union1 {
                  ... on Type1 {
                    field1
                    field2: field1(arg1: "abc")
                  }
                }
              }
            }
            """);

        result.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "union1": {
                        "__typename": "Type1",
                        "field1": "field1value",
                        "field2": "abc"
                    }
                }
            }
            """);
    }

    public class Bug4397Query : ObjectGraphType
    {
        public Bug4397Query()
        {
            Name = "Query";
            Field<Bug4397Union1>("union1").Resolve(_ => new Bug4397Type1Model());
        }
    }

    public class Bug4397Union1 : UnionGraphType
    {
        public Bug4397Union1()
        {
            Name = "Union1";
            Type<Bug4397Type1>();
        }
    }

    public class Bug4397Type1 : ObjectGraphType<Bug4397Type1Model>
    {
        public Bug4397Type1()
        {
            Name = "Type1";
            Field<StringGraphType>("field1")
                .Argument<StringGraphType>("arg1")
                .Resolve(ctx => ctx.GetArgument<string>("arg1") ?? "field1value");
        }
    }

    public class Bug4397Type1Model { }
}
