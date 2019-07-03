using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug1205VeryLongInt : QueryTestBase<Bug1205VeryLongIntSchema>
    {
        [Fact]
        public void ArrayOfArray_Should_Return_As_Is()
        {
            var query = "{ big }";
            var expected = @"{
  ""big"": 636474637870330463
}";
            AssertQuerySuccess(query, expected);
        }
    }

    public class Bug1205VeryLongIntSchema : Schema
    {
        public Bug1205VeryLongIntSchema()
        {
            Query = new Bug1205VeryLongIntQuery();
        }
    }

    public class Bug1205VeryLongIntQuery : ObjectGraphType
    {
        public Bug1205VeryLongIntQuery()
        {
            Field<IntGraphType>(
                "big",
                resolve: ctx =>
                {
                    return 636474637870330463;
                });
        }
    }
}
