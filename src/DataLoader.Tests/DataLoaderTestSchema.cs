using System;
using DataLoader.Tests.Types;
using GraphQL.Types;

namespace DataLoader.Tests
{
    public class DataLoaderTestSchema : Schema
    {
        public DataLoaderTestSchema(IServiceProvider services, QueryType query)
            : base(new DependencyResolver(services))
        {
            Query = query;
        }
    }
}
