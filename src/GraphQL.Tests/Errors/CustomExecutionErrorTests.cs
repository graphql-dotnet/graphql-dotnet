using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Errors
{
    public class CustomExecutionErrorTests : QueryTestBase<CustomExecutionErrorTests.TestSchema>
    {
        [Fact]
        public async Task should_show_custom_error_when_manual_ExecutionError_is_thrown()
        {
            var body = "{\n    test\n}";
            var result = await Executer.ExecuteAsync(
                Schema,
                null,
                body,
                null
                );

            result.Errors.Count.ShouldBe(1);
            var error = result.Errors.First();
            error.Locations.Count().ShouldBe(1);
            error.Message.ShouldBe("Custom Error");
        }

        public class TestQuery : ObjectGraphType
        {
            public TestQuery()
            {
                Name = "Query";

                Field<StringGraphType>()
                    .Name("test")
                    .Resolve(_ => { throw new ExternalExecutionError("Custom Error"); });
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
