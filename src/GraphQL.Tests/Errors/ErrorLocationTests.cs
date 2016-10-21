using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Errors
{
    public class ErrorLocationTests : QueryTestBase<ErrorLocationTests.TestSchema>
    {
        [Fact]
        public async Task should_show_location_when_exception_thrown()
        {
            const string query = @"{
    test
}";

            var result =  await Executer.ExecuteAsync(
                Schema,
                null,
                query,
                null
                );

            result.Errors.Count.ShouldBe(1);
            var error = result.Errors.First();
            error.Locations.Count().ShouldBe(1);
            var location = error.Locations.First();
            location.Line.ShouldBe(1);
            location.Column.ShouldBe(4);
        }

        public class TestQuery : ObjectGraphType
        {
            public TestQuery()
            {
                Name = "Query";

                Field<StringGraphType>()
                    .Name("test")
                    .Resolve(_ => { throw new Exception("wat"); });
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
}
