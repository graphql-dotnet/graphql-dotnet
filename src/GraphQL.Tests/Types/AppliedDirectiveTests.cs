using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types;

public class DirectiveTests
{
    [Fact]
    public async Task applied_directives_arguments_are_validated()
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryType };
        var sampleDirective = new Directive("sample", DirectiveLocation.Field);
        schema.Directives.Register(sampleDirective);
        var result = await schema.ExecuteAsync(o => o.Query = """
            { dummy @sample(value: "123456") }
            """);
        result.ShouldBeSimilarTo("""
            {
                "errors": [
                    {
                        "message": "Unknown argument 'value' on directive 'sample'.",
                        "locations": [
                            {
                                "line": 1,
                                "column": 17
                            }
                        ],
                        "extensions": {
                            "code": "KNOWN_ARGUMENT_NAMES",
                            "codes": [
                                "KNOWN_ARGUMENT_NAMES"
                            ],
                            "number": "5.4.1"
                        }
                    }
                ]
            }
            """);
    }

    [Fact]
    public async Task applied_directives_are_validated()
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryType };
        var result = await schema.ExecuteAsync(o => o.Query = """
            { dummy @sample(value: "123456") }
            """);
        result.ShouldBeSimilarTo("""
            {
                "errors": [
                    {
                        "message": "Unknown directive \u0027sample\u0027.",
                        "locations": [
                            {
                                "line": 1,
                                "column": 9
                            }
                        ],
                        "extensions": {
                            "code": "KNOWN_DIRECTIVES",
                            "codes": [
                                "KNOWN_DIRECTIVES"
                            ],
                            "number": "5.7.1"
                        }
                    }
                ]
            }
            """);
    }

    [Fact]
    public async Task applied_directives_arguments_are_parsed()
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<StringGraphType>("dummy");
        var schema = new Schema() { Query = queryType };
        var sampleDirective = new Directive("sample", DirectiveLocation.Field);
        var arg = new QueryArgument<StringGraphType> { Name = "value" };
        arg.ParseValue(val => ((string)val) + "x");
        arg.Validate(val =>
        {
            if (((string)val).Length > 5)
            {
                throw new InvalidOperationException($"Maximum length of value is 5 characters but was '{val}'.");
            }
        });
        sampleDirective.Arguments = new QueryArguments(arg);
        schema.Directives.Register(sampleDirective);
        var result = await schema.ExecuteAsync(o => o.Query = """
            { dummy @sample(value: "123456") }
            """);
        result.ShouldBeSimilarTo("""
            {
                "errors": [
                    {
                        "message": "Invalid value for argument 'value' of directive 'sample' for field 'dummy'. Maximum length of value is 5 characters but was '123456x'.",
                        "locations": [
                            {
                                "line": 1,
                                "column": 24
                            }
                        ],
                        "extensions": {
                            "code": "INVALID_VALUE",
                            "codes": [
                                "INVALID_VALUE",
                                "INVALID_OPERATION"
                            ],
                            "number": "5.6"
                        }
                    }
                ]
            }
            """);
    }
}
