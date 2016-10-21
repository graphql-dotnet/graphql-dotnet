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
        [Theory]
        [InlineData("{\n    test\n}", 2, 5)]
        [InlineData("{  test\n}", 1, 4)]
        [InlineData("{\n    test\n  cat\n}", 3, 3)]
        public async Task should_show_location_when_exception_thrown(string body, int line, int column)
        {
            var result = await Executer.ExecuteAsync(
                Schema,
                null,
                body,
                null
                );

            result.Errors.Count.ShouldBe(1);
            var error = result.Errors.First();
            error.Locations.Count().ShouldBe(1);
            var location = error.Locations.First();
            location.Line.ShouldBe(line);
            location.Column.ShouldBe(column);
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
