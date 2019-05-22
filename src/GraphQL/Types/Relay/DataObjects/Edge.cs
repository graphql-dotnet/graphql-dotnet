namespace GraphQL.Types.Relay.DataObjects
{
    public class Edge<TNode>
    {
        public string Cursor { get; set; }

        public TNode Node { get; set; }
    }
}
