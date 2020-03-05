namespace GraphQL.Types
{
    public interface IGraphType : IProvideMetadata, INamedType
    {
        string DeprecationReason { get; }
    }
}
