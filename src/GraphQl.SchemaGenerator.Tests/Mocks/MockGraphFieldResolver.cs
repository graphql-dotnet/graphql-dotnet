using System;
using System.Net.Http;
using System.Web.Http;

namespace GraphQl.SchemaGenerator.Tests.Mocks
{
    class MockGraphFieldResolver : GraphFieldResolver
    {
        public MockGraphFieldResolver():base(new MockServiceProvider())
        {
        }

    }

    class MockServiceProvider : IServiceProvider
    {
        public object GetService(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }
}

