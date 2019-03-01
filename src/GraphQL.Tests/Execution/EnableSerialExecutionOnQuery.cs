using System.Threading;
using GraphQL.Conversion;
using GraphQL.Http;
using GraphQL.Tests.StarWars;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class EnableSerialExecutionOnQuery : StarWarsTestBase
    {
        private readonly ExecutionOptions _executionOptions;

        public EnableSerialExecutionOnQuery()
        {
            _executionOptions = new ExecutionOptions()
            {
                SetFieldMiddleware = false,
                EnableMetrics = false,
                Schema = Schema,
                Query = @"
               {
                  human(id: ""1"") {
                    name
                    friends {
                      name
                      appearsIn
                    }
                  }
               }
            ",
                Root = null,
                Inputs = null,
                UserContext = null,
                CancellationToken = default(CancellationToken),
                ValidationRules = null,
                FieldNameConverter = new CamelCaseFieldNameConverter(),
                EnableSerialExecutionOnQuery = true
            };
        }

        [Fact]
        public async void EnableSerialExecutionOnQuery_NoError()
        {
            var docExec = new DocumentExecuter();
            var result = await docExec.ExecuteAsync(_executionOptions).ConfigureAwait(false);
            Assert.Null(result.Errors);
        }


        [Fact]
        public async void EnableSerialExecutionOnQuery_Success()
        {
            var expected =
                "{\r\n  \"data\": {\r\n    \"human\": {\r\n      \"name\": \"Luke\",\r\n      \"friends\": [\r\n        {\r\n          \"name\": \"R2-D2\",\r\n          \"appearsIn\": [\r\n            \"NEWHOPE\",\r\n            \"EMPIRE\",\r\n            \"JEDI\"\r\n          ]\r\n        },\r\n        {\r\n          \"name\": \"C-3PO\",\r\n          \"appearsIn\": [\r\n            \"NEWHOPE\",\r\n            \"EMPIRE\",\r\n            \"JEDI\"\r\n          ]\r\n        }\r\n      ]\r\n    }\r\n  }\r\n}";

            var docExec = new DocumentExecuter();
            var resultObj = await docExec.ExecuteAsync(_executionOptions).ConfigureAwait(false);
            var writer = new DocumentWriter(indent: true);
            var resultJson = JObject.Parse(writer.WriteToStringAsync(resultObj).GetAwaiter().GetResult());
            var expectedJson = JObject.Parse(expected);

            Assert.Equal(expectedJson.ToString(), resultJson.ToString());
        }
    }
}
