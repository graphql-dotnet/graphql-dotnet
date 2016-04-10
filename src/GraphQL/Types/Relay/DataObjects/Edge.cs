namespace GraphQL.Types.Relay.DataObjects
{
    public class Edge<T>
    {
        public string Cursor { get; set; }

        public T Node { get; set; }
    }
}
