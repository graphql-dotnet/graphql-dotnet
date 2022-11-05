namespace GraphQL.MicrosoftDI.Tests;

public class ScopedAsyncFieldResolverTests : ScopedContextBase
{
    [Fact]
    public async Task TReturn_only()
    {
        var resolver = new ScopedFieldResolver<string>(context =>
        {
            context.RequestServices.ShouldBe(_scopedServiceProvider);
            return new ValueTask<string>("success");
        });
        (await resolver.ResolveAsync(_scopedContext).ConfigureAwait(false)).ShouldBe("success");
        VerifyScoped();
    }

    [Fact]
    public async Task TSource_and_TReturn()
    {
        var resolver = new ScopedFieldResolver<string, int>(context =>
        {
            context.Source.ShouldBe("test");
            context.RequestServices.ShouldBe(_scopedServiceProvider);
            return new ValueTask<int>(2);
        });
        _scopedContext.Source = "test";
        (await resolver.ResolveAsync(_scopedContext).ConfigureAwait(false)).ShouldBe(2);
        VerifyScoped();
    }

    [Fact]
    public void RequiresRequestServices_TReturn_only()
    {
        var resolver = new ScopedFieldResolver<int>(context => new ValueTask<int>(5));
        Should.Throw<MissingRequestServicesException>(async () => await resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false));
    }

    [Fact]
    public void RequiresRequestServices_TSource_and_TReturn()
    {
        var resolver = new ScopedFieldResolver<string, int>(context => new ValueTask<int>(5));
        Should.Throw<MissingRequestServicesException>(async () => await resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false));
    }
}
