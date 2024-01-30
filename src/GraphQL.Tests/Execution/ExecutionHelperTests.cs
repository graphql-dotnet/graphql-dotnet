using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Tests.Execution;

public class ExecutionHelperTests
{
    [Fact]
    public async Task Argument_Validation_Supports_ValidationError()
    {
        var query = new ObjectGraphType();
        query.Field<StringGraphType>("test")
            .Argument<StringGraphType>("arg", configure: arg => arg.Validate(_ => throw new MyValidationError()));
        var schema = new Schema { Query = query };
        var result = await schema.ExecuteAsync(_ => _.Query = "{ test(arg: \"123\") }");
        result.ShouldBeCrossPlatJson("""
            {
              "errors": [
                {
                  "message": "My validation error",
                  "locations": [
                    {
                      "line": 1,
                      "column": 8
                    }
                  ],
                  "extensions": {
                    "code": "MY_VALIDATION",
                    "codes": [
                      "MY_VALIDATION"
                    ]
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Argument_Validation_Supports_ExecutionError()
    {
        var query = new ObjectGraphType();
        query.Field<StringGraphType>("test")
            .Argument<StringGraphType>("arg", configure: arg => arg.Validate(_ => throw new MyValidationError2()));
        var schema = new Schema { Query = query };
        var result = await schema.ExecuteAsync(_ => _.Query = "{ test(arg: \"123\") }");
        result.ShouldBeCrossPlatJson("""
            {
              "errors": [
                {
                  "message": "My validation error2",
                  "locations": [
                    {
                      "line": 1,
                      "column": 8
                    }
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Argument_Validation_Supports_WrapsUnknownErrors()
    {
        var query = new ObjectGraphType();
        query.Field<StringGraphType>("test")
            .Argument<StringGraphType>("arg", configure: arg => arg.Validate(_ => throw new InvalidOperationException("Sample error.")));
        var schema = new Schema { Query = query };
        var result = await schema.ExecuteAsync(_ => _.Query = "{ test(arg: \"123\") }");
        result.ShouldBeCrossPlatJson("""
            {
              "errors": [
                {
                  "message": "Invalid value for argument 'arg' of field 'test'. Sample error.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 13
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

    [Fact]
    public async Task InputField_Validation_Supports_ValidationError()
    {
        var input = new InputObjectGraphType();
        input.Field<StringGraphType>("test")
            .Validate(_ => throw new MyValidationError());
        var query = new ObjectGraphType();
        query.Field<StringGraphType>("test")
            .Arguments(new QueryArguments(new QueryArgument(input) { Name = "arg" }));
        var schema = new Schema { Query = query };
        var result = await schema.ExecuteAsync(_ => _.Query = "{ test(arg: { test: \"123\" }) }");
        result.ShouldBeCrossPlatJson("""
            {
              "errors": [
                {
                  "message": "My validation error",
                  "locations": [
                    {
                      "line": 1,
                      "column": 15
                    }
                  ],
                  "extensions": {
                    "code": "MY_VALIDATION",
                    "codes": [
                      "MY_VALIDATION"
                    ]
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task InputField_Validation_Supports_ExecutionError()
    {
        var input = new InputObjectGraphType();
        input.Field<StringGraphType>("test")
            .Validate(_ => throw new MyValidationError2());
        var query = new ObjectGraphType();
        query.Field<StringGraphType>("test")
            .Arguments(new QueryArguments(new QueryArgument(input) { Name = "arg" }));
        var schema = new Schema { Query = query };
        var result = await schema.ExecuteAsync(_ => _.Query = "{ test(arg: { test: \"123\" }) }");
        result.ShouldBeCrossPlatJson("""
            {
              "errors": [
                {
                  "message": "My validation error2",
                  "locations": [
                    {
                      "line": 1,
                      "column": 15
                    }
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task InputField_Validation_Supports_WrapsUnknownErrors()
    {
        var input = new InputObjectGraphType();
        input.Field<StringGraphType>("test1")
            .Validate(_ => throw new InvalidOperationException("Sample error."));
        var query = new ObjectGraphType();
        query.Field<StringGraphType>("test")
            .Arguments(new QueryArguments(new QueryArgument(input) { Name = "arg" }));
        var schema = new Schema { Query = query };
        var result = await schema.ExecuteAsync(_ => _.Query = "{ test(arg: { test1: \"123\" }) }");
        result.ShouldBeCrossPlatJson("""
            {
              "errors": [
                {
                  "message": "Invalid value for argument 'arg' of field 'test'. Sample error.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 15
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

    [Fact]
    public async Task Directive_Validation_Wrapping()
    {
        var input = new InputObjectGraphType();
        input.Field<StringGraphType>("test1")
            .Validate(_ => throw new InvalidOperationException("Sample error."));
        var directive = new Directive("test2")
        {
            Arguments = new QueryArguments(new QueryArgument(input) { Name = "arg" })
        };
        directive.Locations.Add(DirectiveLocation.Field);
        var query = new ObjectGraphType();
        query.Field<StringGraphType>("test");
        var schema = new Schema { Query = query };
        schema.Directives.Register(directive);
        var result = await schema.ExecuteAsync(_ => _.Query = "{ test @test2(arg: { test1: \"123\" }) }");
        result.ShouldBeCrossPlatJson("""
            {
              "errors": [
                {
                  "message": "Invalid value for argument 'arg' of directive 'test2' for field 'test'. Sample error.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 22
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

    private class MyValidationError : ValidationError
    {
        public MyValidationError()
            : base("My validation error")
        {
        }
    }

    private class MyValidationError2 : ExecutionError
    {
        public MyValidationError2()
            : base("My validation error2")
        {
        }
    }
}
