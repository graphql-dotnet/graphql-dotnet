using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.Http;
using GraphQL.StarWars.IoC;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQLParser.Exceptions;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace GraphQL.Tests
{
    public class QueryTestBase<TSchema> : QueryTestBase<TSchema, GraphQLDocumentBuilder>
        where TSchema : ISchema
    {
    }

    public class QueryTestBase<TSchema, TDocumentBuilder>
        where TSchema : ISchema
        where TDocumentBuilder : IDocumentBuilder, new()
    {
        public QueryTestBase()
        {
            Services = new SimpleContainer();
            //Executer = new DocumentExecuter(new TDocumentBuilder(), new DocumentValidator(), new ComplexityAnalyzer());
            Executer = new DI.DIDocumentExecuter(Services);
            Writer = new DocumentWriter(indent: true);
        }

        public ISimpleContainer Services { get; set; }

        public TSchema Schema => Services.Get<TSchema>();

        public IDocumentExecuter Executer { get; private set; }

        public IDocumentWriter Writer { get; private set; }

        public ExecutionResult AssertQuerySuccess(
            string query,
            string expected,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null,
            IFieldNameConverter fieldNameConverter = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(query, queryResult, inputs, root, userContext, cancellationToken, rules, null, fieldNameConverter);
        }

        public ExecutionResult AssertQueryWithErrors(
            string query,
            string expected,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            int expectedErrorCount = 0,
            bool renderErrors = false,
            Action<UnhandledExceptionContext> unhandledExceptionDelegate = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQueryIgnoreErrors(
                query,
                queryResult,
                inputs,
                root,
                userContext,
                cancellationToken,
                expectedErrorCount,
                renderErrors,
                unhandledExceptionDelegate);
        }

        public ExecutionResult AssertQueryIgnoreErrors(
            string query,
            ExecutionResult expectedExecutionResult,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            int expectedErrorCount = 0,
            bool renderErrors = false,
            Action<UnhandledExceptionContext> unhandledExceptionDelegate = null)
        {
            var runResult = Executer.ExecuteAsync(options =>
            {
                options.Schema = Schema;
                options.Query = query;
                options.Root = root;
                options.Inputs = inputs;
                options.UserContext = userContext;
                options.CancellationToken = cancellationToken;
                options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (ctx => { });
            }).GetAwaiter().GetResult();

            var renderResult = renderErrors ? runResult : new ExecutionResult { Data = runResult.Data };

            var writtenResult = Writer.WriteToStringAsync(renderResult).GetAwaiter().GetResult();
            var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

            writtenResult.ShouldBeCrossPlat(expectedResult);

            var errors = runResult.Errors ?? new ExecutionErrors();

            errors.Count().ShouldBe(expectedErrorCount);

            return runResult;
        }

        public ExecutionResult AssertQuery(
            string query,
            ExecutionResult expectedExecutionResult,
            Inputs inputs,
            object root,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null,
            Action<UnhandledExceptionContext> unhandledExceptionDelegate = null,
            IFieldNameConverter fieldNameConverter = null)
        {
            var runResult = Executer.ExecuteAsync(options =>
            {
                options.Schema = Schema;
                options.Query = query;
                options.Root = root;
                options.Inputs = inputs;
                options.UserContext = userContext;
                options.CancellationToken = cancellationToken;
                options.ValidationRules = rules;
                options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (ctx => { });
                options.FieldNameConverter = fieldNameConverter ?? CamelCaseFieldNameConverter.Instance;
            }).GetAwaiter().GetResult();

            var writtenResult = Writer.WriteToStringAsync(runResult).GetAwaiter().GetResult();
            var expectedResult = Writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

            string additionalInfo = null;

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo = string.Join(Environment.NewLine, runResult.Errors
                    .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                    .Select(x => x.InnerException.Message));
            }

            writtenResult.ShouldBeCrossPlat(expectedResult, additionalInfo);

            return runResult;
        }

        public static ExecutionResult CreateQueryResult(string result, ExecutionErrors errors = null)
        {
            object data = null;
            if (!string.IsNullOrWhiteSpace(result))
            {
                data = JObject.Parse(result);
            }

            return new ExecutionResult { Data = data, Errors = errors };
        }
    }
}
