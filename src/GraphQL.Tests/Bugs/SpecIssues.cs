using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class SpecIssues
{
    // https://github.com/graphql/graphql-spec/pull/1056
    [Fact]
    public async Task Issue1056_CoerceVariableValues()
    {
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<StringGraphType, string>("field")
            .Argument<NonNullGraphType<StringGraphType>>("arg", a => a.DefaultValue = "defaultValue")
            .Resolve(ctx => ctx.GetArgument<string>("arg"));
        var schema = new Schema { Query = queryType };
        var query = """
            query ($var: String) {
              field(arg: $var)
            }
            """;
        var result = await new DocumentExecuter().ExecuteAsync(new()
        {
            Query = query,
            Schema = schema,
            Variables = Inputs.Empty,
        });
        result.ShouldBeSimilarTo("""
            {"data":{"field":"defaultValue"}}
            """);
    }

    // https://github.com/graphql/graphql-spec/pull/1057
    [Fact]
    public async Task Issue1057_CoerceListValues_ListList()
    {
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<ListGraphType<ListGraphType<IntGraphType>>, int[][]>("field")
            .Argument<ListGraphType<ListGraphType<IntGraphType>>>("arg")
            .Resolve(ctx => ctx.GetArgument<int[][]>("arg"));
        var schema = new Schema { Query = queryType };
        var query = """
            {
              field(arg: [[1,2,3]])
            }
            """;
        var result = await new DocumentExecuter().ExecuteAsync(new()
        {
            Query = query,
            Schema = schema,
            Variables = Inputs.Empty,
        });
        result.ShouldBeSimilarTo("""
            {"data":{"field":[[1,2,3]]}}
            """);
    }

    // https://github.com/graphql/graphql-spec/pull/1057
    [Fact]
    public async Task Issue1057_CoerceListValues_List()
    {
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<ListGraphType<ListGraphType<IntGraphType>>, int[][]>("field")
            .Argument<ListGraphType<ListGraphType<IntGraphType>>>("arg")
            .Resolve(ctx => ctx.GetArgument<int[][]>("arg"));
        var schema = new Schema { Query = queryType };
        var query = """
            {
              field(arg: [1,2,3])
            }
            """;
        var result = await new DocumentExecuter().ExecuteAsync(new()
        {
            Query = query,
            Schema = schema,
            Variables = Inputs.Empty,
        });
        result.ShouldBeSimilarTo("""
            {"data":{"field":[[1],[2],[3]]}}
            """);
    }

    // https://github.com/graphql/graphql-spec/pull/1057
    [Fact]
    public async Task Issue1057_CoerceListValues_ListWithNull()
    {
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<ListGraphType<ListGraphType<IntGraphType>>, int[][]>("field")
            .Argument<ListGraphType<ListGraphType<IntGraphType>>>("arg")
            .Resolve(ctx => ctx.GetArgument<int[][]>("arg"));
        var schema = new Schema { Query = queryType };
        var query = """
            {
              field(arg: [1,null,3])
            }
            """;
        var result = await new DocumentExecuter().ExecuteAsync(new()
        {
            Query = query,
            Schema = schema,
            Variables = Inputs.Empty,
        });
        result.ShouldBeSimilarTo("""
            {"data":{"field":[[1],null,[3]]}}
            """);
    }

    // https://github.com/graphql/graphql-spec/pull/1057
    [Fact]
    public async Task Issue1057_CoerceListValues_ListScalar()
    {
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<ListGraphType<ListGraphType<IntGraphType>>, int[][]>("field")
            .Argument<ListGraphType<ListGraphType<IntGraphType>>>("arg")
            .Resolve(ctx => ctx.GetArgument<int[][]>("arg"));
        var schema = new Schema { Query = queryType };
        var query = """
            {
              field(arg: [1])
            }
            """;
        var result = await new DocumentExecuter().ExecuteAsync(new()
        {
            Query = query,
            Schema = schema,
            Variables = Inputs.Empty,
        });
        result.ShouldBeSimilarTo("""
            {"data":{"field":[[1]]}}
            """);
    }

    // https://github.com/graphql/graphql-spec/pull/1057
    [Fact]
    public async Task Issue1057_CoerceListValues_Scalar()
    {
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<ListGraphType<ListGraphType<IntGraphType>>, int[][]>("field")
            .Argument<ListGraphType<ListGraphType<IntGraphType>>>("arg")
            .Resolve(ctx => ctx.GetArgument<int[][]>("arg"));
        var schema = new Schema { Query = queryType };
        var query = """
        {
            field(arg: 1)
        }
        """;
        var result = await new DocumentExecuter().ExecuteAsync(new()
        {
            Query = query,
            Schema = schema,
            Variables = Inputs.Empty,
        });
        result.ShouldBeSimilarTo("""
        {"data":{"field":[[1]]}}
        """);
    }
}
