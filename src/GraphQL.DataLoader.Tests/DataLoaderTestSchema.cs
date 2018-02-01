using System;
using GraphQL.DataLoader.Tests.Types;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests
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
