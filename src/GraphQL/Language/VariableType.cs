namespace GraphQL.Language
{
    public class VariableType
    {
        public VariableType()
        {
            AllowsNull = true;
        }

        public string Name { get; set; }

        public bool IsList { get; set; }

        public bool AllowsNull { get; set; }
    }
}