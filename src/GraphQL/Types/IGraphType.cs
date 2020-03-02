namespace GraphQL.Types
{
    public interface INamedType
    {
        string Name { get; }

        string Description { get; }
    }

    public interface IGraphType : IProvideMetadata, INamedType
    {
        string DeprecationReason { get; }

        string CollectTypes(TypeCollectionContext context);
    }
}
