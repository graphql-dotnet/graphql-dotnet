using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.Exceptions;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace GraphQL.Tests
{
    public class BasicQueryTestBase
    {
        protected readonly IDocumentExecuter Executer = new DocumentExecuter();

        public ExecutionResult AssertQuerySuccess(
            ISchema schema,
            string query,
            string expected,
            IDocumentWriter writer,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(schema, query, queryResult, inputs, root, writer, userContext, cancellationToken, rules);
        }

        public ExecutionResult AssertQuerySuccess(Action<ExecutionOptions> options, string expected, IDocumentWriter writer)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(options, queryResult, writer);
        }

        public ExecutionResult AssertQuery(Action<ExecutionOptions> options, ExecutionResult expectedExecutionResult, IDocumentWriter writer)
        {
            var runResult = Executer.ExecuteAsync(options).Result;

            var writtenResult = writer.WriteToStringAsync(runResult).Result;
            var expectedResult = writer.WriteToStringAsync(expectedExecutionResult).Result;

//#if DEBUG
//            Console.WriteLine(writtenResult);
//#endif

            string additionalInfo = null;

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                    .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                    .Select(x => x.InnerException.Message));
            }

            writtenResult.ShouldBe(expectedResult, additionalInfo);

            return runResult;
        }

        public ExecutionResult AssertQuery(
            ISchema schema,
            string query,
            ExecutionResult expectedExecutionResult,
            Inputs inputs,
            object root,
            IDocumentWriter writer,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null)
        {
            var runResult = Executer.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = query;
                _.Root = root;
                _.Inputs = inputs;
                _.UserContext = userContext;
                _.CancellationToken = cancellationToken;
                _.ValidationRules = rules;
            }).GetAwaiter().GetResult();

            var writtenResult = writer.WriteToStringAsync(runResult).GetAwaiter().GetResult();
            var expectedResult = writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

//#if DEBUG
//            Console.WriteLine(writtenResult);
//#endif

            string additionalInfo = null;

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                    .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                    .Select(x => x.InnerException.Message));
            }

            writtenResult.ShouldBe(expectedResult, additionalInfo);

            return runResult;
        }

        public ExecutionResult CreateQueryResult(string result)
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
