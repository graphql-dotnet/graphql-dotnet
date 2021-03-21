using System;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace GraphQL.MicrosoftDI.Tests
{
    public class AutoCreatingServiceProviderTests
    {
        private readonly IServiceProvider _scopedServiceProvider1;
        private readonly IServiceProvider _scopedServiceProvider2;
        private readonly IServiceProvider _autoCreatingServiceProvider2;

        public AutoCreatingServiceProviderTests()
        {
            var services = new ServiceCollection();
            services.AddSingleton<TestSingleton>();
            services.AddScoped<TestScoped>();
            var rootServiceProvider = services.BuildServiceProvider();
            _scopedServiceProvider1 = rootServiceProvider.CreateScope().ServiceProvider;
            _scopedServiceProvider2 = rootServiceProvider.CreateScope().ServiceProvider;
            _autoCreatingServiceProvider2 = new SelfActivatingServiceProvider(_scopedServiceProvider2);
        }

        [Fact]
        public void prefers_pulling_from_service_provider()
        {
            var obj1 = _scopedServiceProvider1.GetRequiredService<TestSingleton>();
            var obj2 = _autoCreatingServiceProvider2.GetRequiredService<TestSingleton>();
            obj1.ShouldBe(obj2);
        }

        [Fact]
        public void pulls_from_scoped_service_provider()
        {
            var obj1 = _scopedServiceProvider1.GetRequiredService<TestScoped>();
            var obj2 = _scopedServiceProvider2.GetRequiredService<TestScoped>();
            var obj3 = _autoCreatingServiceProvider2.GetRequiredService<TestScoped>();
            obj1.ShouldNotBe(obj2);
            obj2.ShouldBe(obj3);
        }

        [Fact]
        public void creates_when_not_registered()
        {
            _autoCreatingServiceProvider2.GetRequiredService<TestUnregistered>();
        }

        [Fact]
        public void creates_when_not_registered_with_dependencies()
        {
            _autoCreatingServiceProvider2.GetRequiredService<TestUnregisteredWithDependencies>();
        }

        [Fact]
        public void fails_with_missing_dependencies()
        {
            Should.Throw<InvalidOperationException>(() => _autoCreatingServiceProvider2.GetRequiredService<TestUnregisteredWithMissingDependencies>());
        }

        private class TestSingleton
        {
        }

        private class TestScoped
        {
        }

        private class TestUnregistered
        {
        }

        private class TestUnregisteredWithDependencies
        {
            public TestUnregisteredWithDependencies(TestSingleton testSingleton)
            {
                if (testSingleton == null)
                    throw new ArgumentNullException(nameof(testSingleton));
            }
        }

        private class TestUnregisteredWithMissingDependencies
        {
            public TestUnregisteredWithMissingDependencies(TestUnregistered testUnregistered)
            {
                _ = testUnregistered; // ignore compile warning about unused member
            }
        }
    }
}
