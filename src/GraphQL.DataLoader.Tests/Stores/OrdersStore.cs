using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;

namespace GraphQL.DataLoader.Tests.Stores
{
    public class OrdersStore
    {
        private readonly List<Order> _orders = new List<Order>();

        public void AddOrders(params Order[] orders)
        {
            _orders.AddRange(orders);
        }

        private int _getOrdersByUserIdCalled;
        public int GetOrdersByUserIdCalledCount => _getOrdersByUserIdCalled;

        private IEnumerable<int> _getOrdersByUserId_UserIds;
        public IEnumerable<int> GetOrdersByUserId_UserIds => _getOrdersByUserId_UserIds;

        private int _getOrderByIdCalled;
        public int GetOrderByIdCalledCount => _getOrderByIdCalled;

        private int _getAllOrdersCalled;
        public int GetAllOrdersCalledCount => _getAllOrdersCalled;

        public async Task<ILookup<int, Order>> GetOrdersByUserIdAsync(IEnumerable<int> userIds)
        {
            Interlocked.Increment(ref _getOrdersByUserIdCalled);
            Interlocked.Exchange(ref _getOrdersByUserId_UserIds, userIds);

            await Task.Yield();

            return _orders
                .Join(userIds, o => o.UserId, x => x, (o, _) => o)
                .ToLookup(o => o.UserId);
        }

        public async Task<IEnumerable<Order>> GetOrderByIdAsync(IEnumerable<int> orderIds)
        {
            Interlocked.Increment(ref _getOrderByIdCalled);

            await Task.Delay(1);

            return _orders
                .Join(orderIds, o => o.OrderId, x => x, (o, _) => o);
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            Interlocked.Increment(ref _getAllOrdersCalled);

            await Task.Delay(1);

            return _orders.AsReadOnly();
        }
    }
}
