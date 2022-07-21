using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL.Tests.Bugs;

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
    public void Should_Ignore_Unknown_Directives()
    {
        var query = @"
{
  fieldWithoutDirectives @unknown
}
";
        var expected = @"{
  ""fieldWithoutDirectives"": ""4""
}";
        // empty validation rules to bypass validation error from KnownDirectivesInAllowedLocations
        AssertQuerySuccess(query, expected, rules: Array.Empty<IValidationRule>());
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
        Field<StringGraphType>("fieldWith2Literal")
            .Resolve(ctx =>
        {
            ctx.Directives.ShouldNotBeNull();
            ctx.HasDirectives().ShouldBeTrue();
            ctx.Directives.Count.ShouldBe(2);

            ctx.HasDirective("skip").ShouldBeTrue();
            ctx.HasDirective("include").ShouldBeTrue();
            ctx.HasDirective("ooops").ShouldBeFalse();

            var dir1Info = ctx.Directives["skip"];
            dir1Info.Arguments.Count.ShouldBe(1);
            dir1Info.Arguments["if"].Source.ShouldBe(GraphQL.Execution.ArgumentSource.Literal);
            dir1Info.Arguments["if"].Value.ShouldBe(false);
            dir1Info.GetArgument<bool>("if").ShouldBeFalse();
            dir1Info.GetArgument("notexists", "12345").ShouldBe("12345");

            var dir2Info = ctx.GetDirective("include");
            dir2Info.Arguments.Count.ShouldBe(1);
            dir2Info.Arguments["if"].Source.ShouldBe(GraphQL.Execution.ArgumentSource.Literal);
            dir2Info.Arguments["if"].Value.ShouldBe(true);
            dir2Info.GetArgument<bool>("if").ShouldBeTrue();

            return "1";
        });
        Field<StringGraphType>("fieldWithVariable")
            .Resolve(ctx =>
        {
            ctx.Directives.ShouldNotBeNull();
            ctx.HasDirectives().ShouldBeTrue();
            ctx.Directives.Count.ShouldBe(1);

            ctx.HasDirective("include").ShouldBeTrue();
            ctx.HasDirective("ooops").ShouldBeFalse();

            var dir1Info = ctx.Directives["include"];
            dir1Info.Arguments.Count.ShouldBe(1);
            dir1Info.Arguments["if"].Source.ShouldBe(GraphQL.Execution.ArgumentSource.Variable);
            dir1Info.Arguments["if"].Value.ShouldBe(true);
            dir1Info.GetArgument<bool>("if").ShouldBeTrue();

            return "2";
        });
        Field<StringGraphType>("fieldWithVariableDefault")
            .Resolve(ctx =>
        {
            ctx.Directives.ShouldNotBeNull();
            ctx.HasDirectives().ShouldBeTrue();
            ctx.Directives.Count.ShouldBe(1);

            var dir1Info = ctx.Directives["include"];
            dir1Info.Arguments.Count.ShouldBe(1);
            dir1Info.Arguments["if"].Source.ShouldBe(GraphQL.Execution.ArgumentSource.VariableDefault);
            dir1Info.Arguments["if"].Value.ShouldBe(true);

            return "3";
        });
        Field<StringGraphType>("fieldWithoutDirectives")
            .Resolve(ctx =>
        {
            ctx.Directives.ShouldBeNull();
            ctx.HasDirectives().ShouldBeFalse();
            return "4";
        });
    }
}
