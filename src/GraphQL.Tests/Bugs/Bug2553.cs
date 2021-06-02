using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.SystemTextJson;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug2553
    {
        [Fact]
        public async Task ShouldSerializeErrorExtensionsAccordingToOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new ExecutionResultJsonConverter(new TestErrorInfoProvider()));

            var executionResult = new ExecutionResult();
            executionResult.AddError(new ExecutionError("An error occurred."));

            var writer = new DocumentWriter(options);
            string json = await writer.WriteToStringAsync(executionResult);

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
            var info = base.GetInfo(executionError);

            info.Extensions = new Dictionary<string, object>
            {
                {"violations", new TestExecutionError("An error occurred on field Email.", "Email")}
            };
            return info;
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
}
