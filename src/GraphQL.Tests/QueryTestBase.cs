using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.StarWars.IoC;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQLParser.Exceptions;
using Shouldly;

namespace GraphQL.Tests
{
    public class QueryTestBase<TSchema> : QueryTestBase<TSchema, GraphQLDocumentBuilder, SimpleContainer>
        where TSchema : Schema
    {
    }

    public class QueryTestBase<TSchema, TIocContainer> : QueryTestBase<TSchema, GraphQLDocumentBuilder, TIocContainer>
       where TSchema : Schema
       where TIocContainer : ISimpleContainer, new()
    {
    }

    public class QueryTestBase<TSchema, TDocumentBuilder, TIocContainer>
        where TSchema : Schema
        where TDocumentBuilder : IDocumentBuilder, new()
        where TIocContainer : ISimpleContainer, new()
    {
        public QueryTestBase()
        {
            Services = new TIocContainer();
            Executer = new DocumentExecuter(new TDocumentBuilder(), new DocumentValidator(), new ComplexityAnalyzer());
        }

        public ISimpleContainer Services { get; set; }

        /// <summary>
        /// WARNING! By default each time you access this property a new schema instance is created.
        /// <br/>
        /// Call Services.Singleton&lt;TSchema&gt;(); in your test constructor to configure schema as singleton.
        /// </summary>
        public TSchema Schema => Services.Get<TSchema>();

        public IDocumentExecuter Executer { get; private set; }

        public ExecutionResult AssertQuerySuccess(
            string query,
            string expected,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null,
            INameConverter nameConverter = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(query, queryResult, inputs, root, userContext, cancellationToken, rules, null, nameConverter);
        }

        public ExecutionResult AssertQueryWithErrors(
            string query,
            string expected,
            Inputs inputs = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null,
            int expectedErrorCount = 0,
            bool renderErrors = false,
            Action<UnhandledExceptionContext> unhandledExceptionDelegate = null,
            bool executed = true)
        {
            var queryResult = CreateQueryResult(expected, executed: executed);
            return AssertQueryIgnoreErrors(
                query,
                queryResult,
                inputs,
                root,
                userContext,
                cancellationToken,
                rules,
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
            IEnumerable<IValidationRule> rules = null,
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
                options.ValidationRules = rules;
                options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (ctx => { });
            }).GetAwaiter().GetResult();

            var renderResult = renderErrors ? runResult : new ExecutionResult { Data = runResult.Data, Executed = runResult.Executed };

            foreach (var writer in DocumentWritersTestData.AllWriters)
            {
                var writtenResult = writer.WriteToStringAsync(renderResult).GetAwaiter().GetResult();
                var expectedResult = writer.WriteToStringAsync(expectedExecutionResult).GetAwaiter().GetResult();

                writtenResult.ShouldBeCrossPlat(expectedResult);

                var errors = runResult.Errors ?? new ExecutionErrors();

                errors.Count.ShouldBe(expectedErrorCount);
            }

            return runResult;
        }

        public ExecutionResult AssertQuery(
            string query,
            object expectedExecutionResultOrJson,
            Inputs inputs,
            object root,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null,
            Action<UnhandledExceptionContext> unhandledExceptionDelegate = null,
            INameConverter nameConverter = null)
        {
            var schema = Schema;
            schema.NameConverter = nameConverter ?? CamelCaseNameConverter.Instance;
            var runResult = Executer.ExecuteAsync(options =>
            {
                options.Schema = schema;
                options.Query = query;
                options.Root = root;
                options.Inputs = inputs;
                options.UserContext = userContext;
                options.CancellationToken = cancellationToken;
                options.ValidationRules = rules;
                options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (ctx => { });
            }).GetAwaiter().GetResult();

            foreach (var writer in DocumentWritersTestData.AllWriters)
            {
                var writtenResult = writer.WriteToStringAsync(runResult).GetAwaiter().GetResult();
                var expectedResult = expectedExecutionResultOrJson is string s ? s : writer.WriteToStringAsync((ExecutionResult)expectedExecutionResultOrJson).GetAwaiter().GetResult();

                string additionalInfo = $"{writer.GetType().FullName} failed: ";

                if (runResult.Errors?.Any() == true)
                {
                    additionalInfo += string.Join(Environment.NewLine, runResult.Errors
                        .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                        .Select(x => x.InnerException.Message));
                }

                writtenResult.ShouldBeCrossPlat(expectedResult, additionalInfo);
            }

            return runResult;
        }

        public static ExecutionResult CreateQueryResult(string result, ExecutionErrors errors = null, bool executed = true)
            => result.ToExecutionResult(errors, executed);
    }
}
