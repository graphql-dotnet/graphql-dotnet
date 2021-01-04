namespace GraphQL.Language.AST
{
    public class VariableReference : AbstractNode, IValue
    {
        public VariableReference(string name)
        {
            Name = name;
        }

        object IValue.Value => Name;
        public string Name { get; }

        /// <inheritdoc />
        public override string ToString() => $"VariableReference{{name={Name}}}";
    }
}
