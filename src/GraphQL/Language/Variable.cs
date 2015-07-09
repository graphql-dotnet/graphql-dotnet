namespace GraphQL.Language
{
    public class Variable
    {
        public string Name { get; set; }

        public VariableType Type { get; set; }

        public object DefaultValue { get; set; }

        public object Value { get; set; }
    }
}