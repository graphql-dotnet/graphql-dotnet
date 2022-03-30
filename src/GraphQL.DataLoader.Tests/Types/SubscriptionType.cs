using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests.Types
{
    public class SubscriptionType : ObjectGraphType
    {
        private readonly IOrdersStore ordersStore;
        public SubscriptionType(IOrdersStore ordersStore)
        {
            this.ordersStore = ordersStore;

            Name = "Subscription";

            AddField(new FieldType
            {
                Name = "orderAdded",
                Type = typeof(OrderType),
                Resolver = new FuncFieldResolver<Order>(ResolveMessage),
                StreamResolver = new SourceStreamResolver<Order>(ResolveStream)
            });
        }

        private Order ResolveMessage(IResolveFieldContext context)
        {
            return context.Source as Order;
        }

        private IObservable<Order> ResolveStream(IResolveFieldContext context)
        {
            return ordersStore.GetOrderObservable();
        }
    }
}
