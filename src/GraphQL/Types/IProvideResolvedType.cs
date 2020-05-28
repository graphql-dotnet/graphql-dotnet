namespace GraphQL.Types
{
    public interface IProvideResolvedType
    {
        IGraphType ResolvedType { get; }
    }
}
