using GraphQL.Types;

namespace GraphQL.Tests.Errors;

public class QueryErrorTests : QueryTestBase<QueryErrorTests.TestSchema>
{
    [Theory]
    [InlineData("unknownoperation { firstAsync }", 1, 1)]
    [InlineData("{ { firstAsync } }", 1, 3)]
    [InlineData("{ firstAsync", 1, 13)]
    [InlineData("{ firstAsync( }", 1, 15)]
    public async Task parsing_error_for_bad_query(string query, int errorLine, int errorColumn)
    {
        var result = await Executer.ExecuteAsync(_ =>
        {
            _.Schema = Schema;
            _.Query = query;
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        var error = result.Errors.First();
        error.Code.ShouldBe("SYNTAX_ERROR");
        error.Locations.ShouldNotBeNull();
        error.Locations.Count.ShouldBe(1);
        error.Locations.First().Line.ShouldBe(errorLine);
        error.Locations.First().Column.ShouldBe(errorColumn);
        error.Message.ShouldStartWith("Error parsing query:");
        error.InnerException.ShouldBeOfType<GraphQLParser.Exceptions.GraphQLSyntaxErrorException>();
    }

    public class TestQuery : ObjectGraphType
    {
        public TestQuery()
        {
            Name = "Query";
            Field<StringGraphType>("firstSync").Resolve(_ => "3");
        }
    }

    public class TestSchema : Schema
    {
        public TestSchema()
        {
            Query = new TestQuery();
        }
    }
}
