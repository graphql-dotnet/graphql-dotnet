namespace GraphQL.Language
{
    public class Field
    {
        public string Name { get; set; }

        public string Alias { get; set; }

        public Directives Directives { get; set; }

        public Arguments Arguments { get; set; }

        public Selections Selections { get; set; }
    }
}