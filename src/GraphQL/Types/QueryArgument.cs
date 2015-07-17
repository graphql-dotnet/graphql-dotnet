namespace GraphQL.Types
{
    public class QueryArgument : IHaveDefaultValue
    {
        public string Name { get; set; }

        public object DefaultValue { get; set; }

        public GraphType Type { get; set; }
    }
}
