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
        public async Task should_show_location_when_exception_thrown(string query, int line, int column)
        {
            var result = await Executer.ExecuteAsync(_ =>
            {
                _.Schema = Schema;
                _.Query = query;
            });

            result.Errors.Count.ShouldBe(1);
            var error = result.Errors.First();
            error.Locations.Count().ShouldBe(1);
            var location = error.Locations.First();
            location.Line.ShouldBe(line);
            location.Column.ShouldBe(column);
        }

        [Theory]
        [InlineData("{\n    testasync\n}", 2, 5)]
        [InlineData("{  testasync\n}", 1, 4)]
        [InlineData("{\n    testasync\n  cat\n}", 3, 3)]
        public async Task should_show_location_when_exception_thrown_with_async_field(string query, int line, int column)
        {
            var result = await Executer.ExecuteAsync(_ =>
            {
                _.Schema = Schema;
                _.Query = query;
            });

            result.Errors.Count.ShouldBe(1);
            var error = result.Errors.First();
            error.Locations.Count().ShouldBe(1);
            var location = error.Locations.First();
            location.Line.ShouldBe(line);
            location.Column.ShouldBe(column);
        }

        [Fact]
        public async Task should_include_path()
        {
            var result = await Executer.ExecuteAsync(_ =>
            {
                _.Schema = Schema;
                _.Query = @"{ testSub { one two } }";
            });

            result.Errors.Count.ShouldBe(1);
            var error = result.Errors.First();
            error.Path.ShouldBe(new[] {"testSub", "two"});
        }

        [Fact]
        public async Task should_include_path_with_list_index()
        {
            var result = await Executer.ExecuteAsync(_ =>
            {
                _.Schema = Schema;
                _.Query = @"{ testSubList { one two } }";
            });

            result.Errors.Count.ShouldBe(1);
            var error = result.Errors.First();
            error.Path.ShouldBe(new[] {"testSubList", "0", "two"});
        }

        [Fact]
        public void async_field_with_errors()
        {
            var error = new ExecutionError("Error trying to resolve testasync.");
            error.AddLocation(1, 3);
            error.Path = new[] {"testasync"};

            var errors = new ExecutionErrors {error};

            AssertQueryIgnoreErrors(
                "{ testasync }",
                CreateQueryResult(@"{
   ""testasync"": null
}", errors),
                expectedErrorCount: 1,
                renderErrors: true);
        }

        public class TestQuery : ObjectGraphType
        {
            public TestQuery()
            {
                Name = "Query";

                Field<StringGraphType>()
                    .Name("test")
                    .Resolve(_ => throw new Exception("wat"));

                FieldAsync<StringGraphType>(
                    "testasync",
                    resolve: async _ => throw new Exception("wat"));

                Field<TestSubObject>()
                    .Name("testSub")
                    .Resolve(_ => new {One = "One", Two = "Two"});

                Field<ListGraphType<TestSubObject>>()
                    .Name("testSubList")
                    .Resolve(_ => new[] {new Thing {One = "One", Two = "Two"}});
            }
        }

        public class Thing
        {
            public string One { get; set; }
            public string Two { get; set; }
        }

        public class TestSubObject : ObjectGraphType
        {
            public TestSubObject()
            {
                Name = "Sub";
                Field<StringGraphType>()
                    .Name("one");

                Field<StringGraphType>()
                    .Name("two")
                    .Resolve(_ => throw new Exception("wat"));
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
