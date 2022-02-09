using GraphQL.Caching;
using GraphQL.DI;
using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace GraphQL.Tests.Execution
{
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
                new ComplexityAnalyzer(),
                DefaultDocumentCache.Instance,
                new IConfigureExecutionOptions[] { },
                selector);
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
            });
            ret.Errors.ShouldBeNull();
            queryStrategy.Executed.ShouldBeTrue();
            ret = await executer.ExecuteAsync(new ExecutionOptions()
            {
                Schema = schema,
                Query = "mutation{hero}",
                Root = new SampleGraph(),
            });
            ret.Errors.ShouldBeNull();
            mutationStrategy.Executed.ShouldBeTrue();
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
}
