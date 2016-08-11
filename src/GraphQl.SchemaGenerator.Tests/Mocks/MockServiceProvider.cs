using System;

namespace GraphQL.SchemaGenerator.Tests.Mocks
{
    class MockServiceProvider : IServiceProvider
    {
        public object Data { get; set; }

        public MockServiceProvider()
        {
            
        }

        public MockServiceProvider(object data)
        {
            Data = data;
        }

        public object GetService(Type serviceType)
        {
            if (Data == null)
            {
                return Activator.CreateInstance(serviceType);
            }

            return Data;
        }
    }
}
