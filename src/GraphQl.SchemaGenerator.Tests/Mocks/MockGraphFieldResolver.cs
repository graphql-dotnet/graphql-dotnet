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
            var instance = Activator.CreateInstance(type);
            var apiInstance = instance as ApiController;

            if (apiInstance == null)
            {
                return instance;
            }

            apiInstance.Request = new HttpRequestMessage();
            apiInstance.Configuration = new HttpConfiguration();

            return apiInstance;
        }
    }
}

