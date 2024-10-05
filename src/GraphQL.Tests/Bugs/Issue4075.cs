using System.Numerics;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs.Bug1046;

public class Issue4075
{
    [Fact]
    public async Task id_should_be_readable_as_string_or_integer()
    {
        // this test ensures that an ID, passed as either a string or integer, can be read as either type
        // note that Apollo Router coerces IDs that are strings containing only digits to integers, so this is a common scenario
        // even when the client sends IDs as strings

        var schema = new Schema { Query = new QueryGraphType() };

        var response = await schema.ExecuteAsync(_ =>
        {
            _.Query = """
                query q($idStr: ID!, $idInt: ID!, $largeInt: ID!, $veryLargeInt: ID!) {
                  string1: string(arg: $idStr)
                  int1: int(arg: $idStr)
                  string2: string(arg: $idInt)
                  int2: int(arg: $idInt)
                  largeInt: string(arg: $largeInt)
                  veryLargeInt: string(arg: $veryLargeInt)
                }
                """;
            _.Variables = new Dictionary<string, object?> {
                { "idStr", "123" },
                { "idInt", 123 },
                { "largeInt", 123456789012345678L },
                { "veryLargeInt", BigInteger.Parse("123456789012345678901234567890") },
            }.ToInputs();
        });

        response.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "string1": "123",
                    "int1": 123,
                    "string2": "123",
                    "int2": 123,
                    "largeInt": "123456789012345678",
                    "veryLargeInt": "123456789012345678901234567890"
                }
            }
            """);
    }

    public class QueryGraphType : ObjectGraphType
    {
        public QueryGraphType()
        {
            Field<NonNullGraphType<StringGraphType>>("string")
                .Argument<NonNullGraphType<IdGraphType>>("arg")
                .Resolve(ctx => ctx.GetArgument<string>("arg"));
            Field<NonNullGraphType<IntGraphType>>("int")
                .Argument<NonNullGraphType<IdGraphType>>("arg")
                .Resolve(ctx => ctx.GetArgument<int>("arg"));
        }
    }
}
