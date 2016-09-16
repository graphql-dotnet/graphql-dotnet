using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution.Cancellation
{
    public class CancellationSchema : Schema
    {
        public CancellationSchema()
        {
            Query = new CancellationTestType();
        }
    }

    public class CancellationTestType : ObjectGraphType
    {
        public CancellationTestType()
        {
            Name = "CancellationTestType";

            Field<StringGraphType>("one", resolve: GetOneAsync);
            Field<StringGraphType>("two", resolve: GetTwoAsync);
        }

        public Task<string> GetOneAsync(ResolveFieldContext<object> context)
        {
            if (!context.CancellationToken.CanBeCanceled)
            {
                throw new Exception("Should have token!");
            }

            return Task.FromResult("one");
        }

        public Task<string> GetTwoAsync(ResolveFieldContext<object> context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult("two");
        }
    }

    public class CancellationTests : QueryTestBase<CancellationSchema>
    {
        [Fact]
        public void cancellation_token_in_context()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                AssertQuerySuccess("{one}", "{one: 'one'}", cancellationToken: tokenSource.Token);
            }
        }

        [Fact]
        public void cancellation_is_propagated()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                tokenSource.Cancel();
                var result = AssertQueryWithErrors("{two}", null, cancellationToken: tokenSource.Token, expectedErrorCount: 1);
                var aggExc = result.Errors.Single();
                aggExc.InnerException.ShouldBeOfType<OperationCanceledException>();
            }
        }
    }
}
