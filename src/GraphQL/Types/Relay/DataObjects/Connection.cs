using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types.Relay.DataObjects
{
    public class Connection<T>
    {
        public int TotalCount { get; set; }

        public PageInfo PageInfo { get; set; }

        public List<Edge<T>> Edges { get; set; }

        public List<T> Items
        {
            get { return Edges?.Select(edge => edge.Node).ToList(); }
        }
    }
}
