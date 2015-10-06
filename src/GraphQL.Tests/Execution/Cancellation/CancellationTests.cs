using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Should;

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

            Field<StringGraphType>("one", resolve: GetOne);
            Field<StringGraphType>("two", resolve: GetTwo);
        }

        public Task<string> GetOne(ResolveFieldContext context)
        {
            if (!context.CancellationToken.CanBeCanceled)
            {
                throw new Exception("Should have token!");
            }

            return Task.FromResult("one");
        }

        public Task<string> GetTwo(ResolveFieldContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult("two");
        }
    }

    public class CancellationTests : QueryTestBase<CancellationSchema>
    {
        [Test]
        public void cancellation_token_in_context()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                AssertQuerySuccess("{one}", "{one: 'one'}", cancellationToken: tokenSource.Token);
            }
        }

        [Test]
        public void cancellation_is_propagated()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                try
                {
                    tokenSource.Cancel();
                    AssertQuerySuccess("{two}", "{two: 'two'}", cancellationToken: tokenSource.Token);
                }
                catch(AggregateException aggExc)
                {
                    aggExc.InnerException.ShouldBeType<TaskCanceledException>();
                    return;
                }
            }

            throw new Exception("Cancellation did not propagate!");
        }
    }
}
