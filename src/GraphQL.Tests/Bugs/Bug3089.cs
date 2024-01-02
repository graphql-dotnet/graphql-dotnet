using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/3089
public class Bug3089
{
    [Fact]
    public async Task ValidationAndListenerErrorsAreMixed()
    {
        var schema = Schema.For("""
            type Query {
                foo: String
            }
            """);
        var result = await schema.ExecuteAsync(o =>
        {
            o.Query = "{ dummy }";
            o.Listeners.Add(new MyListener());
        });
        result.ShouldBeSimilarTo("""
            {
                "errors": [
                    {
                        "message": "Cannot query field \u0027dummy\u0027 on type \u0027Query\u0027.",
                        "locations": [
                            {
                                "line": 1,
                                "column": 3
                            }
                        ],
                        "extensions": {
                            "code": "FIELDS_ON_CORRECT_TYPE",
                            "codes": [
                                "FIELDS_ON_CORRECT_TYPE"
                            ],
                            "number": "5.3.1"
                        }
                    },
                    {
                        "message": "Test1"
                    }
                ]
            }
            """);
    }

    private class MyListener : DocumentExecutionListenerBase
    {
        public override Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult)
        {
            context.Errors.Add(new ExecutionError("Test1"));
            return Task.CompletedTask;
        }
    }
}
