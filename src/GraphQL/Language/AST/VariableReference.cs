namespace GraphQL.Language.AST
{
    public class VariableReference : AbstractNode, IValue
    {
        public VariableReference(NameNode name)
        {
            Name = name.Name;
            NameNode = name;
        }

        object IValue.Value => Name;
        public string Name { get; }
        public NameNode NameNode { get; }

        /// <inheritdoc />
        public override string ToString() => $"VariableReference{{name={Name}}}";
    }
}
