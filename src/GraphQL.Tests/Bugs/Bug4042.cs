using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug4042
{
    public class TestInput
    {
        public int? NullableId { get; set; }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Test1(bool withExpression)
    {
        var inputGraphType = new InputObjectGraphType<TestInput>
        {
            Name = "TestInput",
        };
        if (withExpression)
        {
            inputGraphType.Field(x => x.NullableId, type: typeof(IdGraphType));
        }
        else
        {
            inputGraphType.Field<IdGraphType>("nullableId");
        }

        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field<IdGraphType>("inputNullableId")
            .Argument(inputGraphType, "input")
            .Resolve(ctx =>
            {
                var input = ctx.GetArgument<TestInput>("input");
                return input.NullableId;
            });

        var schema = new Schema() { Query = queryType };
        schema.Initialize();

        var query = "query { inputNullableId(input: { nullableId: \"1\" })}";

        var executer = new DocumentExecuter();
        var executionOptions = new ExecutionOptions
        {
            Schema = schema,
            Query = query
        };

        var result = await executer.ExecuteAsync(executionOptions);
        var ret = new SystemTextJson.GraphQLSerializer().Serialize(result);
        ret.ShouldBe("""
            {"data":{"inputNullableId":"1"}}
            """, StringCompareShould.IgnoreLineEndings);
    }
}
