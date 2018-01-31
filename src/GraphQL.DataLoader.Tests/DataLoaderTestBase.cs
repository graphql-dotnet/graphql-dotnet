using GraphQL.DataLoader.Tests.Stores;

namespace GraphQL.DataLoader.Tests
{
    public abstract class DataLoaderTestBase
    {
        protected UsersStore Users { get; } = new UsersStore();
        protected OrdersStore Orders { get; } = new OrdersStore();
    }
}
