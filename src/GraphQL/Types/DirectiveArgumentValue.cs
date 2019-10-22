namespace GraphQL.Types
{
    public class DirectiveArgumentValue
    {
        public string Name { get; set; }

        public object Value { get; set; }

        public IGraphType ResolvedType { get; set; }
    }
}
