using System.Text.Json;
using GraphQL.Execution;
using GraphQL.SystemTextJson;

namespace GraphQL.Tests.Bugs;

public class Bug2553
{
    [Fact]
    public void ShouldSerializeErrorExtensionsAccordingToOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new ExecutionErrorJsonConverter(new TestErrorInfoProvider()));

        var executionResult = new ExecutionResult();
        executionResult.AddError(new ExecutionError("An error occurred."));

        var writer = new GraphQLSerializer(options);
        string json = writer.Serialize(executionResult);

        json.ShouldBeCrossPlatJson(@"{
                ""errors"": [{
                    ""message"":""An error occurred."",
                    ""extensions"": {
                        ""violations"": {
                            ""message"":""An error occurred on field Email."",
                            ""field"":""Email""
                        }
                    }
                }]
            }");
    }
}

internal class TestErrorInfoProvider : ErrorInfoProvider
{
    public override ErrorInfo GetInfo(ExecutionError executionError)
    {
        return base.GetInfo(executionError) with
        {
            Extensions = new Dictionary<string, object>
            {
                { "violations", new TestExecutionError("An error occurred on field Email.", "Email") }
            }
        };
    }
}

internal class TestExecutionError
{
    public string Message { get; }

    public string Field { get; }

    public TestExecutionError(string message, string field)
    {
        Message = message;
        Field = field;
    }
}
