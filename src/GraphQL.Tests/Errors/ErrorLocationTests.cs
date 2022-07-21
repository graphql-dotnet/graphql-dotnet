using GraphQL.Execution;
using GraphQL.Types;
using GraphQLParser;

namespace GraphQL.Tests.Errors;

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
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        var error = result.Errors.First();
        error.Locations.Count.ShouldBe(1);
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
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        var error = result.Errors.First();
        error.Locations.Count.ShouldBe(1);
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
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        var error = result.Errors.First();
        error.Path.ShouldBe(new[] { "testSub", "two" });
    }

    [Fact]
    public async Task should_include_path_with_list_index()
    {
        var result = await Executer.ExecuteAsync(_ =>
        {
            _.Schema = Schema;
            _.Query = @"{ testSubList { one two } }";
        }).ConfigureAwait(false);

        result.Errors.Count.ShouldBe(1);
        var error = result.Errors.First();
        error.Path.ShouldBe(new object[] { "testSubList", 0, "two" });
    }

    [Fact]
    public void async_field_with_errors()
    {
        var error = new UnhandledError("Error trying to resolve field 'testasync'.", new Exception());
        error.AddLocation(new Location(1, 3));
        error.Path = new[] { "testasync" };

        var errors = new ExecutionErrors { error };

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

            Field<StringGraphType>("test")
                .Resolve(_ => throw new Exception("wat"));

            Field<StringGraphType>("testasync")
                .ResolveAsync(_ => throw new Exception("wat"));

            Field<TestSubObject>("testSub")
                .Resolve(_ => new { One = "One", Two = "Two" });

            Field<ListGraphType<TestSubObject>>("testSubList")
                .Resolve(_ => new[] { new Thing { One = "One", Two = "Two" } });
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
            Field<StringGraphType>("one");

            Field<StringGraphType>("two")
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
