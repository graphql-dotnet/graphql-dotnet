using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue4077
{
    [Fact]
    public async Task empty_string_for_operation_name_is_treated_as_null()
    {
        var schema = new Schema { Query = new QueryGraphType() };

        var response = await schema.ExecuteAsync(_ =>
        {
            // to trigger the bug, the document must contain a named operation,
            _.Query = "query abc { test }";
            // along with an empty string for the operation name
            _.OperationName = "";
        });

        response.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "test": "abc"
                }
            }
            """);
    }

    public class QueryGraphType : ObjectGraphType
    {
        public QueryGraphType()
        {
            Field<StringGraphType>("test").Resolve(ctx => "abc");
        }
    }
}
