using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class AutoRegisteringGraphTypeTests
    {
        [Fact]
        public void DefaultServiceProvider_Should_Create_AutoRegisteringGraphTypes()
        {
            var provider = new DefaultServiceProvider();
            provider.GetService(typeof(AutoRegisteringObjectGraphType<Dummy>)).ShouldNotBeNull();
            provider.GetService(typeof(AutoRegisteringInputObjectGraphType<Dummy>)).ShouldNotBeNull();
        }

        private class Dummy
        {
            public string Name { get; set; }
        }
    }
}
