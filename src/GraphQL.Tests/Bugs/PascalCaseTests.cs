using GraphQL.Conversion;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public sealed class PascalCaseTests : QueryTestBase<PascalCaseSchema>
    {
        [Fact]
        public void get_argument_camel_to_pascal_case()
        {
            var query = "{ Query(ArgumentValue: 42) }";
            var expectedResult = @"{ ""Query"": 42 }";
            AssertQuery(query, CreateQueryResult(expectedResult), null, null, fieldNameConverter: PascalCaseFieldNameConverter.Instance);
        }
    }

    public sealed class PascalCaseSchema : Schema
    {
        public PascalCaseSchema()
        {
            var query = new ObjectGraphType();
            query.Field<IntGraphType>(
                name: "query",
                arguments: new QueryArguments(new QueryArgument<IntGraphType> { Name = "argumentValue" }),
                resolve: context => context.GetArgument<int>("argumentValue")
            );
            Query = query;
        }
    }
}
