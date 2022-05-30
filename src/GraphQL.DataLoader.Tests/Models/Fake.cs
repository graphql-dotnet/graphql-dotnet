using Bogus;

namespace GraphQL.DataLoader.Tests.Models;

/// <summary>
/// Helper class to generate fake model data
/// </summary>
public class Fake
{
    private int userId;
    private int orderId;
    private int productId;

    public Faker<Order> Orders { get; }
    public Faker<OrderItem> OrderItems { get; }
    public Faker<Product> Products { get; }
    public Faker<User> Users { get; }

    public Fake()
    {
        Users = new Faker<User>()
            .RuleFor(x => x.UserId, _ => ++userId)
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.Email, f => f.Internet.Email());

        Orders = new Faker<Order>()
            .RuleFor(x => x.OrderId, _ => ++orderId)
            .RuleFor(x => x.OrderedOn, f => f.Date.Recent(7).Date)
            .RuleFor(x => x.UserId, f => (userId > 0) ? f.Random.Number(1, userId) : 0);

        OrderItems = new Faker<OrderItem>()
            .RuleFor(x => x.OrderItemId, f => f.IndexFaker + 1)
            //.RuleFor(x => x.OrderId, f => (orderId > 0) ? f.Random.Number(1, orderId) : 0)
            .RuleFor(x => x.ProductId, f => (productId > 0) ? f.Random.Number(1, productId) : 0)
            .RuleFor(x => x.Quantity, f => f.Random.Number(1, 2))
            .RuleFor(x => x.UnitPrice, f => f.Finance.Amount());

        Products = new Faker<Product>()
            .RuleFor(x => x.ProductId, f => ++productId)
            .RuleFor(x => x.Name, f => f.Commerce.ProductName())
            .RuleFor(x => x.Price, f => f.Finance.Amount())
            .RuleFor(x => x.Description, f => f.Lorem.Paragraphs(2));
    }

    public List<Order> GenerateOrdersForUsers(IEnumerable<User> users, int each)
    {
        var orders = new List<Order>();

        foreach (var user in users)
        {
            foreach (var userOrder in Orders.Generate(each))
            {
                userOrder.UserId = user.UserId;

                orders.Add(userOrder);
            }
        }

        return orders;
    }

    public List<OrderItem> GetItemsForOrders(IEnumerable<Order> orders, int each)
    {
        var allItems = new List<OrderItem>();

        foreach (var order in orders)
        {
            var items = OrderItems.Generate(each);

            foreach (var item in items)
            {
                item.OrderId = order.OrderId;
            }

            allItems.AddRange(items);
        }

        return allItems;
    }
}
