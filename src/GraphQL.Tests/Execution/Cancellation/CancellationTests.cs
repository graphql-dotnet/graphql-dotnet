using GraphQL.Types;

namespace GraphQL.Tests.Execution.Cancellation;

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

        Field<StringGraphType>("one").ResolveAsync(async context => await GetOneAsync(context).ConfigureAwait(false));
        Field<StringGraphType>("two").ResolveAsync(async context => await GetTwoAsync(context).ConfigureAwait(false));
        Field<StringGraphType>("three").ResolveAsync(async context => await GetThreeAsync(context).ConfigureAwait(false));
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
        await Task.Delay(1000, context.CancellationToken).ConfigureAwait(false);
        // should never execute
        return "three";
    }
}

public class CancellationTests : QueryTestBase<CancellationSchema>
{
    [Fact]
    public void cancellation_token_in_context()
    {
        using var tokenSource = new CancellationTokenSource();
        AssertQuerySuccess("{one}", @"{ ""one"": ""one"" }", cancellationToken: tokenSource.Token);
    }

    [Fact]
    public void cancellation_is_propagated()
    {
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        Should.Throw<OperationCanceledException>(() => _ = AssertQueryWithErrors("{two}", null, cancellationToken: tokenSource.Token, expectedErrorCount: 1));
    }

    [Fact]
    public void cancellation_is_propagated_async()
    {
        using var tokenSource = new CancellationTokenSource();
        Should.Throw<OperationCanceledException>(() => _ = AssertQueryWithErrors("{three}", null, cancellationToken: tokenSource.Token, expectedErrorCount: 1, root: tokenSource));
    }

    [Fact]
    public void unhandled_exception_delegate_is_not_called()
    {
        bool ranDelegate = false;
        using (var tokenSource = new CancellationTokenSource())
        {
            Should.Throw<OperationCanceledException>(() => _ = AssertQueryWithErrors("{three}", null, cancellationToken: tokenSource.Token, expectedErrorCount: 1, root: tokenSource, unhandledExceptionDelegate: _ => { ranDelegate = true; return Task.CompletedTask; }));
        }
        ranDelegate.ShouldBeFalse();
    }
}
