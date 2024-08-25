using System.Text.RegularExpressions;
using GraphQL.Types;
using GraphQL.Utilities.Visitors;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Utilities.Visitors;

public partial class PatternMatchingVisitorTests
{
    [Fact]
    public async Task UppercaseRegex()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<Query>()
            .ConfigureSchema(s =>
            {
                s.Directives.Register(new PatternMatchingDirective());
                s.RegisterVisitor(new PatternMatchingVisitor());
            }));
        services.AddSingleton<RegexGraphType>();

        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();

        var result = await schema.ExecuteAsync(o => o.Query = """{ hello(arg: "HELLO") }""");
        result.ShouldBeSimilarTo("""{"data":{"hello":"HELLO"}}""");

        result = await schema.ExecuteAsync(o => o.Query = """{ hello(arg: "hello") }""");
        result.ShouldBeSimilarTo("""
            {
                "errors": [
                    {
                        "message": "Invalid value for argument \u0027arg\u0027 of field \u0027hello\u0027. Value 'hello' does not match the regex pattern '[A-Z]+'.",
                        "locations": [
                            {
                                "line": 1,
                                "column": 14
                            }
                        ],
                        "extensions": {
                            "code": "INVALID_VALUE",
                            "codes": [
                                "INVALID_VALUE",
                                "ARGUMENT"
                            ],
                            "number": "5.6"
                        }
                    }
                ]
            }
            """);

        var sdl = schema.Print();
        sdl.ShouldBe("""
            "Used to specify a regex pattern for an input field or argument."
            directive @pattern(
              "The regex pattern that the input field or argument must match."
              regex: Regex!) on INPUT_FIELD_DEFINITION | ARGUMENT_DEFINITION
            
            type Query {
              hello(arg: String! @pattern(regex: "[A-Z]+")): String!
            }
            
            scalar Regex
            
            """, StringCompareShould.IgnoreLineEndings);
    }

    private class Query
    {
        public static string Hello(
            [Directive("pattern", "regex", "[A-Z]+")] // uppercase only
            string arg)
            => arg;
    }

    [Fact]
    public async Task AotRegex()
    {
        var regex =
#if NET7_0_OR_GREATER
            Patterns.AlphabeticalPattern();
#else
            new Regex("^[A-Z]+$", RegexOptions.Compiled);
#endif

        var query = new ObjectGraphType { Name = "Query" };
        query.Field<string>("hello")
            .Argument<string>("arg", false, c => c.ApplyDirective("pattern", "regex", regex))
            .Resolve(ctx => ctx.GetArgument<string>("arg"));
        var schema = new Schema { Query = query };
        schema.Directives.Register(new PatternMatchingDirective());
        schema.RegisterVisitor(new PatternMatchingVisitor());
        schema.Initialize();

        var result = await schema.ExecuteAsync(o => o.Query = """{ hello(arg: "HELLO") }""");
        result.ShouldBeSimilarTo("""{"data":{"hello":"HELLO"}}""");

        result = await schema.ExecuteAsync(o => o.Query = """{ hello(arg: "hello") }""");
        result.ShouldBeSimilarTo("""
            {
                "errors": [
                    {
                        "message": "Invalid value for argument \u0027arg\u0027 of field \u0027hello\u0027. Value 'hello' does not match the regex pattern '[A-Z]+'.",
                        "locations": [
                            {
                                "line": 1,
                                "column": 14
                            }
                        ],
                        "extensions": {
                            "code": "INVALID_VALUE",
                            "codes": [
                                "INVALID_VALUE",
                                "ARGUMENT"
                            ],
                            "number": "5.6"
                        }
                    }
                ]
            }
            """);

        var sdl = schema.Print();
        sdl.ShouldBe("""
            "Used to specify a regex pattern for an input field or argument."
            directive @pattern(
              "The regex pattern that the input field or argument must match."
              regex: Regex!) on INPUT_FIELD_DEFINITION | ARGUMENT_DEFINITION

            type Query {
              hello(arg: String! @pattern(regex: "[A-Z]+")): String!
            }

            scalar Regex

            """, StringCompareShould.IgnoreLineEndings);
    }

#if NET7_0_OR_GREATER
    public static partial class Patterns
    {
        [GeneratedRegex("^[A-Z]+$")]
        public static partial Regex AlphabeticalPattern();
    }
#endif
}
