namespace GraphQL.Language
{
    public class Directive
    {
        public string Name { get; set; }

        public object Value { get; set; }

        public Variable Variable { get; set; }
    }
}