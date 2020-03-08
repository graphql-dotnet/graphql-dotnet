using System.Collections.Generic;
using GraphQL.SystemTextJson;
using Xunit;

namespace GraphQL.Tests
{
    /// <summary>
    /// Tests for <see cref="IDocumentWriter"/> implementations and the custom converters
    /// that are used in the process of serializing an <see cref="ExecutionResult"/> to JSON.
    /// </summary>
    public class DocumentWriterTests
    {
        [Theory]
        [ClassData(typeof(DocumentWritersTestData))]
        public async void Can_Write_Execution_Result(IDocumentWriter writer)
        {
            var executionResult = new ExecutionResult
            {
                Data = @"{ ""someType"": { ""someProperty"": ""someValue"" } }".ToDictionary(),
                Errors = new ExecutionErrors
                {
                    new ExecutionError("some error 1"),
                    new ExecutionError("some error 2"),
                },
                Extensions = new Dictionary<string, object>
                {
                    { "someExtension", new { someProperty = "someValue", someOtherPropery = 1 } }
                }
            };

            var expected = @"{
              ""data"": {
                ""someType"": {
                    ""someProperty"": ""someValue""
                }
              },
              ""errors"": [
                {
                  ""message"": ""some error 1""
                },
                {
                  ""message"": ""some error 2""
                }
              ],
              ""extensions"": {
                ""someExtension"": {
                  ""someProperty"": ""someValue"",
                  ""someOtherPropery"": 1
                }
              }
            }";

            var actual = await writer.WriteToStringAsync(executionResult);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(DocumentWritersTestData))]
        public async void Writes_Correct_Execution_Result_With_Null_Data_And_Null_Errors(IDocumentWriter writer)
        {
            var executionResult = new ExecutionResult();

            var expected = @"{
              ""data"": null
            }";

            var actual = await writer.WriteToStringAsync(executionResult);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(DocumentWritersTestData))]
        public async void Writes_Correct_Execution_Result_With_Null_Data_And_Some_Errors(IDocumentWriter writer)
        {
            // "If an error was encountered before execution begins, the data entry should not be present in the result."
            // Source: https://github.com/graphql/graphql-spec/blob/master/spec/Section%207%20--%20Response.md#data

            var executionResult = new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError("some error 1"),
                    new ExecutionError("some error 2"),
                }
            };

            var expected = @"{
              ""errors"": [{""message"":""some error 1""},{""message"":""some error 2""}]
            }";

            var actual = await writer.WriteToStringAsync(executionResult);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(DocumentWritersTestData))]
        public async void Writes_Correct_Execution_Result_With_Empty_Data_Errors_And_Extensions(IDocumentWriter writer)
        {
            var executionResult = new ExecutionResult
            {
                Data = new Dictionary<string, object>(),
                Errors = new ExecutionErrors(),
                Extensions = new Dictionary<string, object>()
            };

            var expected = @"{ ""data"": {} }";

            var actual = await writer.WriteToStringAsync(executionResult);

            actual.ShouldBeCrossPlatJson(expected);
        }
    }
}
