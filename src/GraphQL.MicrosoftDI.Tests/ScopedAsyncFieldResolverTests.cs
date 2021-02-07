using System.Threading.Tasks;
using GraphQL.MicrosoftDI;
using Shouldly;
using Xunit;

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
            (await resolver.Resolve(_scopedContext)).ShouldBe("success");
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
            (await resolver.Resolve(_scopedContext)).ShouldBe(2);
            VerifyScoped();
        }
    }
}
