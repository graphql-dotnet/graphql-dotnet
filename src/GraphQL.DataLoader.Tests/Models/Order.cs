using System;
using System.Collections.Generic;

namespace GraphQL.DataLoader.Tests.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderedOn { get; set; }
        public int UserId { get; set; }

        public IList<OrderItem> Items { get; set; }

        public decimal Total { get; set; }
    }
}
