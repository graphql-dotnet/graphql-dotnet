using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
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
            Field<StringGraphType>("three", resolve: GetThreeAsync);
        }

        public Task<string> GetOneAsync(IResolveFieldContext<object> context)
        {
            if (!context.CancellationToken.CanBeCanceled)
            {
                throw new Exception("Should have token!");
            }

            return Task.FromResult("one");
        }

        public Task<string> GetTwoAsync(IResolveFieldContext<object> context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult("two");
        }

        public async Task<string> GetThreeAsync(IResolveFieldContext<object> context)
        {
            await Task.Yield();
            ((CancellationTokenSource)context.RootValue).Cancel();
            await Task.Delay(1000, context.CancellationToken);
            // should never execute
            return "three";
        }
    }

    public class CancellationTests : QueryTestBase<CancellationSchema>
    {
        [Fact]
        public void cancellation_token_in_context()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                AssertQuerySuccess("{one}", @"{ ""one"": ""one"" }", cancellationToken: tokenSource.Token);
            }
        }

        [Fact]
        public void cancellation_is_propagated()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                tokenSource.Cancel();
                Should.Throw<OperationCanceledException>(() =>
                {
                    var result = AssertQueryWithErrors("{two}", null, cancellationToken: tokenSource.Token, expectedErrorCount: 1);
                });
            }
        }

        [Fact]
        public void cancellation_is_propagated_async()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                Should.Throw<OperationCanceledException>(() =>
                {
                    var result = AssertQueryWithErrors("{three}", null, cancellationToken: tokenSource.Token, expectedErrorCount: 1, root: tokenSource);
                });
            }
        }

        [Fact]
        public void unhandled_exception_delegate_is_not_called()
        {
            bool ranDelegate = false;
            Action<UnhandledExceptionContext> unhandledExceptionDelegate = (context) => ranDelegate = true;
            using (var tokenSource = new CancellationTokenSource())
            {
                Should.Throw<OperationCanceledException>(() =>
                {
                    var result = AssertQueryWithErrors("{three}", null, cancellationToken: tokenSource.Token, expectedErrorCount: 1, root: tokenSource, unhandledExceptionDelegate: unhandledExceptionDelegate);
                });
            }
            ranDelegate.ShouldBeFalse();
        }
    }
}
