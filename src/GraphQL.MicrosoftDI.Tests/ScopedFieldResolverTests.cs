namespace GraphQL.MicrosoftDI.Tests;

public class ScopedFieldResolverTests : ScopedContextBase
{
    [Fact]
    public async Task TReturn_only()
    {
        var resolver = new ScopedFieldResolver<string>(context =>
        {
            context.RequestServices.ShouldBe(_scopedServiceProvider);
            return "success";
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
            return 2;
        });
        _scopedContext.Source = "test";
        (await resolver.ResolveAsync(_scopedContext).ConfigureAwait(false)).ShouldBe(2);
        VerifyScoped();
    }

    [Fact]
    public async Task RequiresRequestServices_TReturn_only()
    {
        var resolver = new ScopedFieldResolver<int>(context => 5);
        await Should.ThrowAsync<MissingRequestServicesException>(async () => await resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false)).ConfigureAwait(false);
    }

    [Fact]
    public async Task RequiresRequestServices_TSource_and_TReturn()
    {
        var resolver = new ScopedFieldResolver<string, int>(context => 5);
        await Should.ThrowAsync<MissingRequestServicesException>(async () => await resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false)).ConfigureAwait(false);
    }
}
