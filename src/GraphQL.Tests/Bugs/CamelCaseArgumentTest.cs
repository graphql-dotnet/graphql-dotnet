using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public sealed class CamelCaseArgumentTest : QueryTestBase<CamelCaseSchema>
    {
        [Fact]
        public void get_argument_pascal_to_camel_case()
        {
            var query = "{ query(argument: 42) }";
            var expectedResult = "{ 'query': 42 }";
            AssertQuery(query, CreateQueryResult(expectedResult), null, null);
        }
    }

    public sealed class CamelCaseSchema : Schema
    {
        public CamelCaseSchema()
        {
            var query = new ObjectGraphType();
            query.Field<IntGraphType>(
                name: "Query",
                arguments: new QueryArguments(new QueryArgument<IntGraphType> { Name = "Argument" }),
                resolve: context => context.GetArgument<int>("Argument")
            );
            Query = query;
        }
    }
}
