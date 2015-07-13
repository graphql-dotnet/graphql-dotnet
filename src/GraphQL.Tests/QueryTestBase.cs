using System;
using GraphQL.Http;
using Newtonsoft.Json.Linq;
using Should;

namespace GraphQL.Tests
{
    public class QueryTestBase<TSchema>
        where TSchema : Schema, new()
    {
        public QueryTestBase()
        {
            Schema = new TSchema();
        }

        public TSchema Schema { get; private set; }

        public void AssertQuerySuccess(string query, string expected, Inputs inputs = null)
        {
            var queryResult = CreateQueryResult(expected);
            AssertQuery(query, queryResult, inputs);
        }

        public void AssertQuery(string query, ExecutionResult executionResult, Inputs inputs)
        {
            var executer = new DocumentExecuter();
            var writer = new DocumentWriter();

            var runResult = executer.Execute(Schema, query, null, inputs);

            var writtenResult = writer.Write(runResult);
            var expectedResult = writer.Write(executionResult);

            Console.WriteLine(writtenResult);

            writtenResult.ShouldEqual(expectedResult);
        }

        public ExecutionResult CreateQueryResult(string result)
        {
            var expected = JObject.Parse(result);
            return new ExecutionResult { Data = expected, Errors = new ExecutionErrors()};
        }

        public ExecutionResult CreateErrorQueryResult()
        {
            return new ExecutionResult();
        }
    }
}
