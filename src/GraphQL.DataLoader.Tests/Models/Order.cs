using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.DataLoader.Tests.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderedOn { get; set; }
        public int UserId { get; set; }
    }
}
