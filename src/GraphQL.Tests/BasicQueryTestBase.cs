using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphQL.Types;
using GraphQL.Http;
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
            object userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
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

            var writtenResult = Writer.Write(runResult);
            var expectedResult = Writer.Write(expectedExecutionResult);

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
            object userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null)
        {
            var runResult = Executer.ExecuteAsync(
                schema,
                root,
                query,
                null,
                inputs,
                userContext,
                cancellationToken,
                rules
                ).Result;

            var writtenResult = Writer.Write(runResult);
            var expectedResult = Writer.Write(expectedExecutionResult);

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
