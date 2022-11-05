using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Execution;

public class DocumentExecuterTests
{
    [Fact]
    public async Task Uses_ExecutionStrategySelector()
    {
        var queryStrategy = new TestQueryExecutionStrategy();
        var mutationStrategy = new TestMutationExecutionStrategy();
        var selector = new DefaultExecutionStrategySelector(
            new[]
            {
                new ExecutionStrategyRegistration(queryStrategy, GraphQLParser.AST.OperationType.Query),
                new ExecutionStrategyRegistration(mutationStrategy, GraphQLParser.AST.OperationType.Mutation),
            });
        var executer = new DocumentExecuter(
            new GraphQLDocumentBuilder(),
            new DocumentValidator(),
            selector,
            new IConfigureExecution[] { });
        var schema = new Schema();
        var graphType = new AutoRegisteringObjectGraphType<SampleGraph>();
        schema.Query = graphType;
        schema.Mutation = graphType;
        schema.Initialize();
        var ret = await executer.ExecuteAsync(new ExecutionOptions()
        {
            Schema = schema,
            Query = "{hero}",
            Root = new SampleGraph(),
        }).ConfigureAwait(false);
        ret.Errors.ShouldBeNull();
        queryStrategy.Executed.ShouldBeTrue();
        ret = await executer.ExecuteAsync(new ExecutionOptions()
        {
            Schema = schema,
            Query = "mutation{hero}",
            Root = new SampleGraph(),
        }).ConfigureAwait(false);
        ret.Errors.ShouldBeNull();
        mutationStrategy.Executed.ShouldBeTrue();
    }

    [Fact]
    public async Task TypedInterfaceMapping()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema<Schema1>()
            .AddSchema<Schema2>()
            .AddSystemTextJson());
        services.AddSingleton(typeof(StringExecuter<>));
        var provider = services.BuildServiceProvider();

        // verify executing with Schema1 works with custom class
        var executer1 = provider.GetRequiredService<StringExecuter<Schema1>>();
        var result1 = await executer1.ExecuteAsync("{hero}").ConfigureAwait(false);
        result1.ShouldBe("{\"data\":{\"hero\":\"hello\"}}");

        // verify executing with Schema2 works with IDocumentExecuter<> directly
        var executer2 = provider.GetRequiredService<IDocumentExecuter<Schema2>>();
        var result2 = await executer2.ExecuteAsync(new ExecutionOptions { Query = "{hero}", RequestServices = provider }).ConfigureAwait(false);
        var serializer = provider.GetRequiredService<IGraphQLTextSerializer>();
        serializer.Serialize(result2).ShouldBe("{\"data\":{\"hero\":\"hello2\"}}");

        // verify that you cannot specify Schema with this implementation
        var err = await Should.ThrowAsync<InvalidOperationException>(async () => await executer2.ExecuteAsync(new ExecutionOptions { Schema = new Schema1(provider), Query = "{hero}", RequestServices = provider }).ConfigureAwait(false)).ConfigureAwait(false);
        err.Message.ShouldBe("ExecutionOptions.Schema must be null when calling this typed IDocumentExecuter<> implementation; it will be pulled from the dependency injection provider.");
    }

    [Fact]
    public async Task Honors_IConfigureExecution_SortOrder()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .ConfigureExecution(new MyConfigureExecution(0, 1, 1)) //executes 1st
            .ConfigureExecution(new MyConfigureExecution(4, 5, 5)) //executes 3rd
            .ConfigureExecution(new MyConfigureExecution(1, 4, 2)) //executes 2nd
            .ConfigureExecution(new MyConfigureExecution((options, _) => //executes 5th
            {
                options.MaxParallelExecutionCount.ShouldBe(6);
                return Task.FromResult<ExecutionResult>(null!); // no need to actually execute a query
            }, 6))
            .ConfigureExecution(new MyConfigureExecution(5, 6, 5)) //executes 4th
        );
        using var provider = services.BuildServiceProvider();
        var executer = provider.GetRequiredService<IDocumentExecuter>();
        var ret = await executer.ExecuteAsync(new()).ConfigureAwait(false);
        ret.ShouldBeNull(); // validates that all configured executions have run, as normally this would never be null
    }

    private class MyConfigureExecution : IConfigureExecution
    {
        private readonly int _expected;
        private readonly int _setValue;
        private readonly int _sortOrder;
        private readonly Func<ExecutionOptions, ExecutionDelegate, Task<ExecutionResult>> _action;

        public MyConfigureExecution(int expected, int setValue, int sortOrder)
        {
            _expected = expected;
            _setValue = setValue;
            _sortOrder = sortOrder;
        }

        public MyConfigureExecution(Func<ExecutionOptions, ExecutionDelegate, Task<ExecutionResult>> action, int sortOrder)
        {
            _action = action;
            _sortOrder = sortOrder;
        }

        public Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
        {
            if (_action != null)
                return _action(options, next);
            (options.MaxParallelExecutionCount ?? 0).ShouldBe(_expected);
            options.MaxParallelExecutionCount = _setValue;
            return next(options);
        }

        public float SortOrder => _sortOrder;
    }

    private class StringExecuter<TSchema> where TSchema : ISchema
    {
        private readonly IDocumentExecuter<TSchema> _executer;
        private readonly IGraphQLTextSerializer _serializer;
        private readonly IServiceScopeFactory _scopeFactory;

        public StringExecuter(IDocumentExecuter<TSchema> executer, IGraphQLTextSerializer serializer, IServiceScopeFactory scopeFactory)
        {
            _executer = executer;
            _serializer = serializer;
            _scopeFactory = scopeFactory;
        }

        public async Task<string> ExecuteAsync(string query)
        {
            using var scope = _scopeFactory.CreateScope();
            var result = await _executer.ExecuteAsync(new ExecutionOptions
            {
                Query = query,
                RequestServices = scope.ServiceProvider,
            }).ConfigureAwait(false);
            return _serializer.Serialize(result);
        }
    }

    private class Schema1 : Schema
    {
        public Schema1(IServiceProvider provider) : base(provider)
        {
            var graph = new ObjectGraphType { Name = "Query" };
            graph.Field<StringGraphType>("hero").Resolve(_ => "hello");
            Query = graph;
        }
    }

    private class Schema2 : Schema
    {
        public Schema2(IServiceProvider provider) : base(provider)
        {
            var graph = new ObjectGraphType { Name = "Query" };
            graph.Field<StringGraphType>("hero").Resolve(_ => "hello2");
            Query = graph;
        }
    }

    private class SampleGraph
    {
        public string Hero => "hello";
    }

    private class TestQueryExecutionStrategy : ParallelExecutionStrategy
    {
        public bool Executed = false;
        public override Task<ExecutionResult> ExecuteAsync(GraphQL.Execution.ExecutionContext context)
        {
            Executed.ShouldBeFalse();
            Executed = true;
            return base.ExecuteAsync(context);
        }
    }

    private class TestMutationExecutionStrategy : SerialExecutionStrategy
    {
        public bool Executed = false;
        public override Task<ExecutionResult> ExecuteAsync(GraphQL.Execution.ExecutionContext context)
        {
            Executed.ShouldBeFalse();
            Executed = true;
            return base.ExecuteAsync(context);
        }
    }
}
