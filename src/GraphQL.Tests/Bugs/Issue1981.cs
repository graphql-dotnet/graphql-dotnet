using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL.Tests.Bugs
{
    public class Issue1981 : QueryTestBase<Issue1981Schema>
    {
        [Fact]
        public void Should_Provide_Directives_In_Resolver()
        {
            var query = @"query Q($var1: Boolean!, $var2: Boolean! = true)
{
  fieldWith2Literal @skip(if: false) @include(if: true)
  fieldWithVariable @include(if: $var1)
  fieldWithVariableDefault @include(if: $var2)
  fieldWithoutDirectives
}
";
            var expected = @"{
  ""fieldWith2Literal"": ""1"",
  ""fieldWithVariable"": ""2"",
  ""fieldWithVariableDefault"": ""3"",
  ""fieldWithoutDirectives"": ""4""
}";
            AssertQuerySuccess(query, expected, @"{ ""var1"": true }".ToInputs());
        }

        [Fact]
        public void Should_Throw_On_Unknown_Directives()
        {
            var query = @"
{
  fieldWithoutDirectives @oops
}
";
            var expected = @"{
  ""fieldWithoutDirectives"": null
}";
            // empty validation rules to bypass vslidation error from KnownDirectivesInAllowedLocations
            var res = AssertQueryWithErrors(query, expected, expectedErrorCount: 1, rules: Array.Empty<IValidationRule>());
            res.Errors[0].Code.ShouldBe("ARGUMENT_OUT_OF_RANGE");
            res.Errors[0].Message.ShouldBe("Error trying to resolve field 'fieldWithoutDirectives'.");
            res.Errors[0].InnerException.Message.ShouldStartWith("Unknown directive 'oops' for field 'fieldWithoutDirectives'.");
        }
    }

    public class Issue1981Schema : Schema
    {
        public Issue1981Schema()
        {
            Query = new Issue1981Query();
        }
    }

    public class Issue1981Query : ObjectGraphType
    {
        public Issue1981Query()
        {
            Name = "Query";
            Field<StringGraphType>("fieldWith2Literal", resolve: ctx =>
            {
                ctx.Directives.ShouldNotBeNull();
                ctx.Directives.Count.ShouldBe(2);

                var dir1Args = ctx.Directives["skip"];
                dir1Args.Count.ShouldBe(1);
                dir1Args["if"].Source.ShouldBe(GraphQL.Execution.ArgumentSource.Literal);
                dir1Args["if"].Value.ShouldBe(false);

                var dir2Args = ctx.Directives["include"];
                dir2Args.Count.ShouldBe(1);
                dir2Args["if"].Source.ShouldBe(GraphQL.Execution.ArgumentSource.Literal);
                dir2Args["if"].Value.ShouldBe(true);

                return "1";
            });
            Field<StringGraphType>("fieldWithVariable", resolve: ctx =>
            {
                ctx.Directives.ShouldNotBeNull();
                ctx.Directives.Count.ShouldBe(1);

                var dir1Args = ctx.Directives["include"];
                dir1Args.Count.ShouldBe(1);
                dir1Args["if"].Source.ShouldBe(GraphQL.Execution.ArgumentSource.Variable);
                dir1Args["if"].Value.ShouldBe(true);

                return "2";
            });
            Field<StringGraphType>("fieldWithVariableDefault", resolve: ctx =>
            {
                ctx.Directives.ShouldNotBeNull();
                ctx.Directives.Count.ShouldBe(1);

                var dir1Args = ctx.Directives["include"];
                dir1Args.Count.ShouldBe(1);
                dir1Args["if"].Source.ShouldBe(GraphQL.Execution.ArgumentSource.VariableDefault);
                dir1Args["if"].Value.ShouldBe(true);

                return "3";
            });
            Field<StringGraphType>("fieldWithoutDirectives", resolve: ctx =>
            {
                ctx.Directives.ShouldBeNull();
                return "4";
            });
        }
    }
}
