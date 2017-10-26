namespace GraphQL.Language.AST
{
    public interface IValue : INode
    {
        object GetValue();
    }
}
