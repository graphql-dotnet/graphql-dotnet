namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a fragment definition node or an operation node.
    /// </summary>
    public interface IDefinition : INode, IHaveName, IHaveDirectives
    {
    }
}
