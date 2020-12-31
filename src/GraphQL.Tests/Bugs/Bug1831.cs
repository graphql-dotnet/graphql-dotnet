using System.Collections.Generic;
using System.Linq;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/1831
    public class Bug1831 : QueryTestBase<Bug1831Schema>
    {
        [Fact]
        public void TestVariableObject() => AssertQuerySuccess("query($arg: Bug1831Input!) { test1 (arg: $arg) }", @"{ ""test1"": ""ok"" }", @"{ ""arg"": { ""id"": ""id"", ""rows"": [{""id"": ""id1"", ""name"": ""name1""}, {""id"": ""id2"", ""name"": ""name2""}]} }".ToInputs());

        [Fact]
        public void TestLiteralObject() => AssertQuerySuccess("{ test1 (arg: { id: \"id\", rows: [ {id: \"id1\", name: \"name1\"}, {id: \"id2\", name: \"name2\"}]}) }", @"{ ""test1"": ""ok"" }");

        [Theory]
        [InlineData("null")]
        [InlineData("1")]
        public void TestVariableObject_InvalidType(string param)
        {
            var error1 = new ValidationError(null, VariablesAreInputTypesError.NUMBER,
                VariablesAreInputTypesError.UndefinedVarMessage("arg", "abcdefg"))
            {
                Code = "VARIABLES_ARE_INPUT_TYPES"
            };
            error1.AddLocation(1, 7);
            var error2 = new ValidationError(null, KnownTypeNamesError.NUMBER,
                KnownTypeNamesError.UnknownTypeMessage("abcdefg", null))
            {
                Code = "KNOWN_TYPE_NAMES"
            };
            error2.AddLocation(1, 13);
            var expected = CreateQueryResult(null, new ExecutionErrors { error1, error2 });
            AssertQueryIgnoreErrors("query($arg: abcdefg) { test1 (arg: $arg) }", expected, inputs: $"{{ \"arg\": {param} }}".ToInputs(), expectedErrorCount: 2, renderErrors: true);
        }
    }

    public class Bug1831Schema : Schema
    {
        public Bug1831Schema()
        {
            Query = new Bug1831Query();
        }
    }

    public class Bug1831Query : ObjectGraphType
    {
        public Bug1831Query()
        {
            Field<StringGraphType>(
                "test1",
                arguments: new QueryArguments(
                    new QueryArgument(typeof(Bug1831InputGraphType)) { Name = "arg" }),
                resolve: context =>
                {
                    var arg = context.GetArgument<Bug1831Class>("arg");
                    arg.Id.ShouldBe("id");
                    arg.Rows.ShouldNotBeNull();
                    arg.Rows.Count().ShouldBe(2);
                    arg.Rows.First().Id.ShouldBe("id1");
                    arg.Rows.First().Name.ShouldBe("name1");
                    arg.Rows.Last().Id.ShouldBe("id2");
                    arg.Rows.Last().Name.ShouldBe("name2");
                    return "ok";
                });
        }
    }

    public class Bug1831Class
    {
        public string Id { get; set; }
        public IEnumerable<Bug1831Row> Rows { get; set; }
    }
    public class Bug1831Row
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Bug1831InputGraphType : InputObjectGraphType<Bug1831Class>
    {
        public Bug1831InputGraphType()
        {
            Field("id", x => x.Id, true, typeof(StringGraphType));
            Field("rows", x => x.Rows, true, typeof(ListGraphType<Bug1831RowInputGraphType>));
        }
    }
    public class Bug1831RowInputGraphType : InputObjectGraphType<Bug1831Row>
    {
        public Bug1831RowInputGraphType()
        {
            Field("id", x => x.Id, true, typeof(StringGraphType));
            Field("name", x => x.Name, true, typeof(StringGraphType));
        }
    }
}
