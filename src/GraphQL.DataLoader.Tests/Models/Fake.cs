using System.Collections.Generic;
using System.Linq;
using Bogus;

namespace GraphQL.DataLoader.Tests.Models
{
    /// <summary>
    /// Helper class to generate fake model data
    /// </summary>
    public class Fake
    {
        public Faker<User> Users { get; }
        public Faker<Order> Orders { get; }

        public Fake()
        {
            Users = new Faker<User>()
                .RuleFor(x => x.UserId, f => f.IndexFaker + 1)
                .RuleFor(x => x.FirstName, f => f.Name.FirstName())
                .RuleFor(x => x.LastName, f => f.Name.LastName())
                .RuleFor(x => x.Email, f => f.Internet.Email());

            Orders = new Faker<Order>()
                .RuleFor(x => x.OrderId, f => f.IndexFaker + 1)
                .RuleFor(x => x.OrderedOn, f => f.Date.Recent(7));
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
    }
}
