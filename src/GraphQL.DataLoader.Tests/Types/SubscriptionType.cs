using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Resolvers;
using GraphQL.Subscription;
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

            AddField(new EventStreamFieldType
            {
                Name = "orderAdded",
                Type = typeof(OrderType),
                Resolver = new FuncFieldResolver<Order>(ResolveMessage),
                Subscriber = new EventStreamResolver<Order>(Subscribe)
            });
        }

        private Order ResolveMessage(IResolveFieldContext context)
        {
            return context.Source as Order;
        }

        private IObservable<Order> Subscribe(IResolveEventStreamContext context)
        {
            return ordersStore.GetOrderObservable();
        }
    }
}
