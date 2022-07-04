using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public sealed class CamelCaseArgumentTest : QueryTestBase<CamelCaseSchema>
{
    [Fact]
    public void get_argument_pascal_to_camel_case()
    {
        var query = "{ query(argumentValue: 42) }";
        var expectedResult = @"{ ""query"": 42 }";
        AssertQuery(query, CreateQueryResult(expectedResult), null, null);
    }

    [Fact]
    public void get_argument_directly_without_schema()
    {
        var ctx = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue> { { "argumentValue", new ArgumentValue("42", ArgumentSource.Literal) } }
        };

        var result = ctx.GetArgument("ArgumentValue", "defaultValue");
        result.ShouldBe("defaultValue");
    }
}

public sealed class CamelCaseSchema : Schema
{
    public CamelCaseSchema()
    {
        var query = new ObjectGraphType();
        query.Field<IntGraphType>(
            name: "Query",
            arguments: new QueryArguments(new QueryArgument<IntGraphType> { Name = "ArgumentValue" }),
            resolve: context => context.GetArgument<int>("ArgumentValue")
        );
        Query = query;
    }
}
