using System;
using GraphQL.Execution;
using GraphQL.Http;
using GraphQL.Types;
using GraphQL.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Should;

namespace GraphQL.Tests
{
    public class QueryTestBase<TSchema> : QueryTestBase<TSchema, AntlrDocumentBuilder>
        where TSchema : Schema, new()
    {
    }

    public class QueryTestBase<TSchema, TDocumentBuilder>
        where TSchema : Schema, new()
        where TDocumentBuilder : IDocumentBuilder, new()
    {
        public QueryTestBase()
        {
            Schema = new TSchema();
            Executer = new DocumentExecuter(new TDocumentBuilder(), new DocumentValidator());
            Writer = new DocumentWriter(Formatting.Indented);
        }

        public TSchema Schema { get; private set; }

        public IDocumentExecuter Executer { get; private set; }

        public IDocumentWriter Writer { get; private set; }

        public void AssertQuerySuccess(string query, string expected, Inputs inputs = null, object root = null)
        {
            var queryResult = CreateQueryResult(expected);
            AssertQuery(query, queryResult, inputs, root);
        }

        public void AssertQuery(string query, ExecutionResult executionResult, Inputs inputs, object root)
        {
            var runResult = Executer.Execute(Schema, root, query, null, inputs).Result;

            var writtenResult = Writer.Write(runResult);
            var expectedResult = Writer.Write(executionResult);

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
