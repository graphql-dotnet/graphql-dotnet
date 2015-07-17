using GraphQL.Types;

namespace GraphQL.Language
{
    public class Variable : IHaveDefaultValue
    {
        public string Name { get; set; }

        public VariableType Type { get; set; }

        public object DefaultValue { get; set; }

        public object Value { get; set; }
    }
}
