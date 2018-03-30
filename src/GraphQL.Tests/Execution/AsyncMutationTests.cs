using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class AsyncMutationTests : QueryTestBase<AsyncMutationSchema>
    {
        [Fact]
        public void runs_async_mutation_returning_Task_of_private_class()
        {
            var query = @"
                mutation M {
                  addAsync {
                    value
                  }
                }
            ";

            var expected = @"
                {
                  'addAsync': {
                    'value': 29
                  }
                }";

            AssertQuerySuccess(query, expected, root: new AsyncMutationRoot(6));
        }
    }

    public class AsyncMutationRoot
    {
        public AsyncMutationRoot(int addend)
        {
            Addend = addend;
        }

        public int Addend { get; }
    }

    public class AsyncMutationSchema : Schema
    {
        public AsyncMutationSchema()
        {
            Mutation = new AsyncMutationGraphType();
        }
    }

    public class AsyncMutationGraphType : ObjectGraphType
    {
        public AsyncMutationGraphType()
        {
            Name = "Mutation";

            var field = new FieldType
            {
                Name = "addAsync",
                ResolvedType = new AsyncMutationOutputGraphType(),
                Resolver = new FuncFieldResolver<Task<AsyncMutationOutput>>(context =>
                {
                    var source = context.Source as AsyncMutationRoot;
                    return Task.FromResult(new AsyncMutationOutput(23 + source.Addend));
                })
            };
            AddField(field);
        }

        private class AsyncMutationOutput
        {
            public AsyncMutationOutput(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        private class AsyncMutationOutputGraphType : ObjectGraphType<AsyncMutationOutput>
        {
            public AsyncMutationOutputGraphType()
            {
                Name = "AsyncMutationOutput";
                Field("value", o => o.Value);
            }
        }
    }
}
