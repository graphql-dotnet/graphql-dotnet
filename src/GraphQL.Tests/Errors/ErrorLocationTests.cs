using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;
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
            location.Line.ShouldBe(2);
            location.Column.ShouldBe(5);
        }

        [Theory]
        [InlineData("{\n    test\n}", 6, 12, 2, 5)]
        [InlineData("{  test\n}", 3, 9, 1, 4)]
        public void should_calculate_line_and_column(string body, int start, int end, int line, int column)
        {
            var node = new TestNode();
            node.Location = new GraphQLLocation() { Start = start, End = end };
            var source = new TestSource() { Body = body };
            var field = new Field().WithLocation(node, source);
            field.SourceLocation.ShouldBe(new SourceLocation(line, column, start, end));
        }

        public class TestSource : ISource
        {
            public string Body { get; set; }

            public string Name { get; set; }
        }

        public class TestNode : ASTNode
        {
            public override ASTNodeKind Kind { get { return ASTNodeKind.Field; } }
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
