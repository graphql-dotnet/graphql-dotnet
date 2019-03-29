using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types.Relay.DataObjects
{
    public class Connection<TNode, TEdge>
        where TEdge : Edge<TNode>
    {
        public int TotalCount { get; set; }

        public PageInfo PageInfo { get; set; }

        public List<TEdge> Edges { get; set; }

        public List<TNode> Items
        {
            get { return Edges?.Select(edge => edge.Node).ToList(); }
        }
    }

    public class Connection<TNode> : Connection<TNode, Edge<TNode>>
    {

    }
}
