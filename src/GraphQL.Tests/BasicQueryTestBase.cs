using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphQL.NewtonsoftJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.Exceptions;
using Shouldly;
using Newtonsoft.Json.Linq;

namespace GraphQL.Tests
{
    public class BasicQueryTestBase
    {
        protected readonly IDocumentExecuter Executer = new DocumentExecuter();
        protected readonly IDocumentWriter Writer = new DocumentWriter(indent: true);

        public ExecutionResult AssertQuerySuccess(
            ISchema schema,
            string query,
            string expected,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(schema, query, queryResult, inputs, root, userContext, cancellationToken, rules);
        }

        public ExecutionResult AssertQuerySuccess(Action<ExecutionOptions> options, string expected)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(options, queryResult);
        }

        public ExecutionResult AssertQuery(Action<ExecutionOptions> options, ExecutionResult expectedExecutionResult)
        {
            var runResult = Executer.ExecuteAsync(options).Result;

            var writtenResult = Writer.WriteToStringAsync(runResult).Result;
            var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).Result;

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

            var writtenResult = Writer.WriteToStringAsync(runResult).GetAwaiter().GetResult();
            var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

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
