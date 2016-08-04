using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Http;
using GraphQL.Validation;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GraphQl.SchemaGenerator.Tests
{
    public static class GraphAssert
    {
        public static void QuerySuccess(GraphQL.Types.Schema schema, string query, string expected)
        {
            var exec = new DocumentExecuter(new AntlrDocumentBuilder(), new DocumentValidator());
            var result = exec.ExecuteAsync(schema, null, query, null).Result;

            var writer = new DocumentWriter(indent: true);
            var writtenResult = writer.Write(result.Data);
            var expectedResult = writer.Write(CreateQueryResult(expected));

            Assert.Equal(expectedResult, writtenResult);
        }

        private static ExecutionResult CreateQueryResult(string result)
        {
            object expected = null;
            if (!string.IsNullOrWhiteSpace(result))
            {
                expected = JObject.Parse(result);
            }
            return new ExecutionResult { Data = expected };
        }
    }
}
