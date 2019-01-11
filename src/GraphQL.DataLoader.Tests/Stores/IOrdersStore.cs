using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;

namespace GraphQL.DataLoader.Tests.Stores
{
    public interface IOrdersStore
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<IEnumerable<Order>> GetOrderByIdAsync(IEnumerable<int> orderIds);
        Task<ILookup<int, Order>> GetOrdersByUserIdAsync(IEnumerable<int> userIds, CancellationToken cancellationToken);
        Task<ILookup<int, OrderItem>> GetItemsByOrderIdAsync(IEnumerable<int> orderIds);
        IObservable<Order> GetOrderObservable();
    }
}
