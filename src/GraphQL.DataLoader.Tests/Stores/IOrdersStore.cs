using GraphQL.DataLoader.Tests.Models;

namespace GraphQL.DataLoader.Tests.Stores;

public interface IOrdersStore
{
    public Task<IEnumerable<Order>> GetAllOrdersAsync();
    public Task<IEnumerable<Order>> GetOrderByIdAsync(IEnumerable<int> orderIds);
    public Task<ILookup<int, Order>> GetOrdersByUserIdAsync(IEnumerable<int> userIds, CancellationToken cancellationToken);
    public Task<ILookup<int, OrderItem>> GetItemsByOrderIdAsync(IEnumerable<int> orderIds);
    public IObservable<Order> GetOrderObservable();
}
