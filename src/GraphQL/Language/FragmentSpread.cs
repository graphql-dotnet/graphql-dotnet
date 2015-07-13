namespace GraphQL.Language
{
    public class FragmentSpread : IFragment
    {
        public FragmentSpread()
        {
            Directives = new Directives();
        }

        public string Name { get; set; }

        public Directives Directives { get; set; }
    }
}
