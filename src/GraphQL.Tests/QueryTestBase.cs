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

        public void AssertQuerySuccess(string query, string expected)
        {
            var queryResult = CreateQueryResult(expected);
            AssertQuery(query, queryResult);
        }

        public void AssertQuery(string query, ExecutionResult executionResult)
        {
            var executer = new DocumentExecuter();
            var writer = new DocumentWriter();

            var runResult = executer.Execute(Schema, query, null);

            var writtenResult = writer.Write(runResult);
            var expectedResult = writer.Write(executionResult);

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
