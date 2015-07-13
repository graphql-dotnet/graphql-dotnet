namespace GraphQL.Language
{
    public class InlineFragment : IHaveFragmentType
    {
        public string Type { get; set; }

        public Directives Directives { get; set; }

        public Selections Selections { get; set; }
    }
}
