namespace GraphQL.Language
{
    public interface IHaveSelectionSet : INode
    {
        SelectionSet SelectionSet { get; set; }
    }
}
