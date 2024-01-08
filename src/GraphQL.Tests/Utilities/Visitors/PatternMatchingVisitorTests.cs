using GraphQL.Attributes;
using GraphQL.Types;
using GraphQL.Utilities.Visitors;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Utilities.Visitors;

public class PatternMatchingVisitorTests
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
    }

    private class Query
    {
        public static string Hello(
            [Directive("pattern", "regex", "[A-Z]+")] // uppercase only
            string arg)
            => arg;
    }
}
