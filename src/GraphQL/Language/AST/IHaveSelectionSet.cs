namespace GraphQL.Language.AST
{
    public interface IHaveSelectionSet : INode
    {
        SelectionSet SelectionSet { get; set; }
    }
}
