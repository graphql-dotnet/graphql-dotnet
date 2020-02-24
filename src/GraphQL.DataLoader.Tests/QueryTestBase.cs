using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.DataLoader.Tests.Types;
using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQLParser.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nito.AsyncEx;
using Shouldly;

namespace GraphQL.DataLoader.Tests
{
    public abstract class QueryTestBase : DataLoaderTestBase
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
            services.AddSingleton<SubscriptionType>();
            services.AddSingleton<QueryType>();
            services.AddSingleton<OrderType>();
            services.AddSingleton<UserType>();
            services.AddSingleton<OrderItemType>();
            services.AddSingleton<ProductType>();
            services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            services.AddSingleton<IDocumentExecutionListener, DataLoaderDocumentListener>();

            var ordersMock = new Mock<IOrdersStore>();
            var usersMock = new Mock<IUsersStore>();
            var productsMock = new Mock<IProductsStore>();

            services.AddSingleton(ordersMock);
            services.AddSingleton(ordersMock.Object);
            services.AddSingleton(usersMock);
            services.AddSingleton(usersMock.Object);
            services.AddSingleton(productsMock);
            services.AddSingleton(productsMock.Object);
        }

        public ExecutionResult AssertQuerySuccess<TSchema>(
            string query,
            string expected,
            Inputs inputs = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default)
            where TSchema : ISchema
        {
            var queryResult = CreateQueryResult(expected);
            return AssertQuery<TSchema>(query, queryResult, inputs, userContext, cancellationToken);
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
            var runResult = AsyncContext.Run(() => executer.ExecuteAsync(opts =>
            {
                options(opts);
                opts.Schema = schema;
                opts.ExposeExceptions = true;
            }));

            var writtenResult = AsyncContext.Run(() => writer.WriteToStringAsync(runResult));
            var expectedResult = AsyncContext.Run(() => writer.WriteToStringAsync(expectedExecutionResult));

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

        public Task<ExecutionResult> ExecuteQueryAsync<TSchema>(string query)
            where TSchema : ISchema
        {
            var schema = Services.GetRequiredService<TSchema>();

            // Run the executer within an async context to make sure there are no deadlock issues
            return executer.ExecuteAsync(opts =>
            {
                opts.Schema = schema;
                opts.ExposeExceptions = true;
                opts.Query = query;
                foreach (var listener in Services.GetRequiredService<IEnumerable<IDocumentExecutionListener>>())
                {
                    opts.Listeners.Add(listener);
                }
            });
        }

        public ExecutionResult AssertQuery<TSchema>(
            string query,
            ExecutionResult expectedExecutionResult,
            Inputs inputs = null,
            IDictionary<string, object> userContext = null,
            CancellationToken cancellationToken = default)
            where TSchema : ISchema
        {
            return AssertQuery<TSchema>(
                opts =>
                {
                    opts.Query = query;
                    opts.Inputs = inputs;
                    opts.UserContext = userContext;
                    opts.CancellationToken = cancellationToken;

                    foreach (var listener in Services.GetRequiredService<IEnumerable<IDocumentExecutionListener>>())
                    {
                        opts.Listeners.Add(listener);
                    }
                },
                expectedExecutionResult);
        }

        public ExecutionResult CreateQueryResult(string result)
        {
            object expected = string.IsNullOrWhiteSpace(result) ? null : result.ToDictionary();
            return new ExecutionResult { Data = expected };
        }
    }
}
