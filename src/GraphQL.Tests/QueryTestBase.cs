using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.StarWars.IoC;
using GraphQL.SystemTextJson;
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
            Writer = new GraphQLSerializer(indent: true);
        }

        public ISimpleContainer Services { get; set; }

        /// <summary>
        /// WARNING! By default each time you access this property a new schema instance is created.
        /// <br/>
        /// Call Services.Singleton&lt;TSchema&gt;(); in your test constructor to configure schema as singleton.
        /// </summary>
        public TSchema Schema => Services.Get<TSchema>();

        public IDocumentExecuter Executer { get; private set; }

        public IGraphQLTextSerializer Writer { get; private set; }

        public ExecutionResult AssertQuerySuccess(
            string query,
            string expected,
            Inputs variables = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null,
            INameConverter nameConverter = null,
            IGraphQLTextSerializer writer = null)
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery(query, queryResult, variables, root, userContext, cancellationToken, rules, null, nameConverter, writer);
        }

        public ExecutionResult AssertQueryWithErrors(
            string query,
            string expected,
            Inputs variables = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null,
            int expectedErrorCount = 0,
            bool renderErrors = false,
            Func<UnhandledExceptionContext, Task> unhandledExceptionDelegate = null,
            bool executed = true)
        {
            var queryResult = CreateQueryResult(expected, executed: executed);
            return AssertQueryIgnoreErrors(
                query,
                queryResult,
                variables,
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
            Inputs variables = null,
            object root = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null,
            int expectedErrorCount = 0,
            bool renderErrors = false,
            Func<UnhandledExceptionContext, Task> unhandledExceptionDelegate = null)
        {
            var runResult = Executer.ExecuteAsync(options =>
            {
                options.Schema = Schema;
                options.Query = query;
                options.Root = root;
                options.Variables = variables;
                options.UserContext = userContext;
                options.CancellationToken = cancellationToken;
                options.ValidationRules = rules;
                options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (_ => Task.CompletedTask);
            }).GetAwaiter().GetResult();

            var renderResult = renderErrors ? runResult : new ExecutionResult { Data = runResult.Data, Executed = runResult.Executed };

            var writtenResult = Writer.Write(renderResult);
            var expectedResult = Writer.Write(expectedExecutionResult);

            writtenResult.ShouldBeCrossPlat(expectedResult);

            var errors = runResult.Errors ?? new ExecutionErrors();

            errors.Count.ShouldBe(expectedErrorCount);

            return runResult;
        }

        public ExecutionResult AssertQuery(
            string query,
            object expectedExecutionResultOrJson,
            Inputs variables,
            object root,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default,
            IEnumerable<IValidationRule> rules = null,
            Func<UnhandledExceptionContext, Task> unhandledExceptionDelegate = null,
            INameConverter nameConverter = null,
            IGraphQLTextSerializer writer = null)
        {
            var schema = Schema;
            schema.NameConverter = nameConverter ?? CamelCaseNameConverter.Instance;
            var runResult = Executer.ExecuteAsync(options =>
            {
                options.Schema = schema;
                options.Query = query;
                options.Root = root;
                options.Variables = variables;
                options.UserContext = userContext;
                options.CancellationToken = cancellationToken;
                options.ValidationRules = rules;
                options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (_ => Task.CompletedTask);
            }).GetAwaiter().GetResult();

            writer ??= Writer;

            var writtenResult = Writer.Write(runResult);
            var expectedResult = expectedExecutionResultOrJson is string s ? s : Writer.Write((ExecutionResult)expectedExecutionResultOrJson);

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

        public static ExecutionResult CreateQueryResult(string result, ExecutionErrors errors = null, bool executed = true)
            => result.ToExecutionResult(errors, executed);
    }
}
