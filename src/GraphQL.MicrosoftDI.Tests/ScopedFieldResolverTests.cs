namespace GraphQL.MicrosoftDI.Tests
{
    public class ScopedFieldResolverTests : ScopedContextBase
    {
        [Fact]
        public void TReturn_only()
        {
            var resolver = new ScopedFieldResolver<string>(context =>
            {
                context.RequestServices.ShouldBe(_scopedServiceProvider);
                return "success";
            });
            resolver.Resolve(_scopedContext).ShouldBe("success");
            VerifyScoped();
        }

        [Fact]
        public void TSource_and_TReturn()
        {
            var resolver = new ScopedFieldResolver<string, int>(context =>
            {
                context.Source.ShouldBe("test");
                context.RequestServices.ShouldBe(_scopedServiceProvider);
                return 2;
            });
            _scopedContext.Source = "test";
            resolver.Resolve(_scopedContext).ShouldBe(2);
            VerifyScoped();
        }

        [Fact]
        public void RequiresRequestServices_TReturn_only()
        {
            var resolver = new ScopedFieldResolver<int>(context => 5);
            Should.Throw<MissingRequestServicesException>(() => resolver.Resolve(new ResolveFieldContext()));
        }

        [Fact]
        public void RequiresRequestServices_TSource_and_TReturn()
        {
            var resolver = new ScopedFieldResolver<string, int>(context => 5);
            Should.Throw<MissingRequestServicesException>(() => resolver.Resolve(new ResolveFieldContext()));
        }
    }
}
