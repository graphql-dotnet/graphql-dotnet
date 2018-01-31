using System;
using System.Linq;
using System.Threading;
using GraphQL.DataLoader;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.DataLoader.Tests.Types;
using GraphQL.Execution;
using GraphQL.Http;
using GraphQL.Types;
using GraphQLParser.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using Shouldly;

namespace GraphQL.DataLoader.Tests
{
    public abstract class QueryTestBase
    {
        private readonly IDocumentExecuter executer = new DocumentExecuter();
        private readonly IDocumentWriter writer = new DocumentWriter(indent: true);

        protected IServiceProvider Services { get; }

        public QueryTestBase()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            Services = services.BuildServiceProvider();
        }

        protected virtual void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<DataLoaderTestSchema>();
            services.AddSingleton<QueryType>();
            services.AddSingleton<OrderType>();
            services.AddSingleton<UserType>();
            services.AddSingleton<OrdersStore>();
            services.AddSingleton<UsersStore>();
            services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            services.AddTransient<DataLoaderDocumentListener>();
        }

        public ExecutionResult AssertQuerySuccess<TSchema>(
            string query,
            string expected,
            Inputs inputs = null,
            object userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            Type listenerType = null)
            where TSchema : ISchema
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery<TSchema>(query, queryResult, inputs, userContext, cancellationToken, listenerType);
        }

        public ExecutionResult AssertQuerySuccess<TSchema>(Action<ExecutionOptions> options, string expected)
            where TSchema : ISchema
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery<TSchema>(options, queryResult);
        }

        public ExecutionResult AssertQuery<TSchema>(Action<ExecutionOptions> options, ExecutionResult expectedExecutionResult)
            where TSchema : ISchema
        {
            var schema = Services.GetRequiredService<TSchema>();

            // Run the executer within an async context to make sure there are no deadlock issues
            var runResult = AsyncContext.Run(() => executer.ExecuteAsync((opts) =>
            {
                options(opts);
                opts.Schema = schema;
                opts.ExposeExceptions = true;
            }));

            var writtenResult = writer.Write(runResult);
            var expectedResult = writer.Write(expectedExecutionResult);

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

        public ExecutionResult AssertQuery<TSchema>(
            string query,
            ExecutionResult expectedExecutionResult,
            Inputs inputs = null,
            object userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            Type listenerType = null)
            where TSchema : ISchema
        {
            return AssertQuery<TSchema>(_ =>
            {
                _.Query = query;
                _.Inputs = inputs;
                _.UserContext = userContext;
                _.CancellationToken = cancellationToken;

                if (listenerType != null)
                {
                    var listener = (IDocumentExecutionListener)Services.GetRequiredService(listenerType);
                    _.Listeners.Add(listener);
                }

            }, expectedExecutionResult);
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
