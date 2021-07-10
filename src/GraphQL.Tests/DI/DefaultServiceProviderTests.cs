using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.DI
{
    public class DefaultServiceProviderTests
    {
        private readonly IServiceProvider _serviceProvider = new DefaultServiceProvider();

        [Fact]
        public void throws_for_null_service_type()
        {
            Should.Throw<ArgumentNullException>(() => _serviceProvider.GetService(null));
        }

        [Fact]
        public void returns_self_for_iserviceprovider()
        {
            _serviceProvider.GetService(typeof(IServiceProvider)).ShouldBe(_serviceProvider);
        }

        [Fact]
        public void returns_null_for_interfaces()
        {
            _serviceProvider.GetService(typeof(IEnumerable<Class1>)).ShouldBeNull();
        }

        [Fact]
        public void returns_new_instance_for_classes()
        {
            var obj1 = _serviceProvider.GetService(typeof(Class1)).ShouldBeOfType<Class1>().ShouldNotBeNull();
            var obj2 = _serviceProvider.GetService(typeof(Class1)).ShouldBeOfType<Class1>().ShouldNotBeNull();
            obj1.ShouldNotBe(obj2);
        }

        private class Class1
        {
        }
    }
}
