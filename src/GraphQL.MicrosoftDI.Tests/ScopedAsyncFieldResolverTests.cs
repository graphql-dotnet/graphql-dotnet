namespace GraphQL.MicrosoftDI.Tests
{
    public class ScopedAsyncFieldResolverTests : ScopedContextBase
    {
        [Fact]
        public async Task TReturn_only()
        {
            var resolver = new ScopedAsyncFieldResolver<string>(context =>
            {
                context.RequestServices.ShouldBe(_scopedServiceProvider);
                return Task.FromResult("success");
            });
            (await resolver.ResolveAsync(_scopedContext)).ShouldBe("success");
            VerifyScoped();
        }

        [Fact]
        public async Task TSource_and_TReturn()
        {
            var resolver = new ScopedAsyncFieldResolver<string, int>(context =>
            {
                context.Source.ShouldBe("test");
                context.RequestServices.ShouldBe(_scopedServiceProvider);
                return Task.FromResult(2);
            });
            _scopedContext.Source = "test";
            (await resolver.ResolveAsync(_scopedContext)).ShouldBe(2);
            VerifyScoped();
        }

        [Fact]
        public void RequiresRequestServices_TReturn_only()
        {
            var resolver = new ScopedAsyncFieldResolver<int>(context => Task.FromResult(5));
            Should.Throw<MissingRequestServicesException>(async () => await resolver.ResolveAsync(new ResolveFieldContext()));
        }

        [Fact]
        public void RequiresRequestServices_TSource_and_TReturn()
        {
            var resolver = new ScopedAsyncFieldResolver<string, int>(context => Task.FromResult(5));
            Should.Throw<MissingRequestServicesException>(async () => await resolver.ResolveAsync(new ResolveFieldContext()));
        }
    }
}
