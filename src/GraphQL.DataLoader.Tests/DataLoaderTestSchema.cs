using GraphQL.DataLoader.Tests.Types;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests;

public class DataLoaderTestSchema : Schema
{
    public DataLoaderTestSchema(IServiceProvider services, QueryType query, MutationType mutation, SubscriptionType subscriptionType)
        : base(services)
    {
        Query = query; // runs with parallel execution strategy
        Mutation = mutation; // runs with serial execution strategy
        Subscription = subscriptionType;
    }
}
