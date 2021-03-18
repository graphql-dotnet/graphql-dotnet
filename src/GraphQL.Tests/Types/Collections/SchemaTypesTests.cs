using System;
using System.Collections.Generic;
using GraphQL.StarWars;
using GraphQL.StarWars.IoC;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types.Collections
{
    public class SchemaTypesTests
    {
        [Fact]
        public void does_not_request_instance_more_than_once()
        {
            var baseProvider = new SimpleContainer();
            baseProvider.Singleton<StarWarsData>();
            var testProvider = new TestProvider(baseProvider);
            var schema = new StarWarsSchema(testProvider);
            schema.Initialize();
            testProvider.Count.ShouldBe(7); // number of types used in the star wars schema
        }

        private class TestProvider : IServiceProvider
        {
            private readonly IServiceProvider _baseServiceProvider;
            private readonly HashSet<Type> _requestedTypes = new HashSet<Type>();

            public TestProvider(IServiceProvider baseServiceProvider)
            {
                _baseServiceProvider = baseServiceProvider;
            }

            public object GetService(Type serviceType)
            {
                if (!_requestedTypes.Add(serviceType))
                    throw new InvalidOperationException("This type has already been requested");

                return _baseServiceProvider.GetService(serviceType);
            }

            public int Count => _requestedTypes.Count;
        }
    }
}
