namespace GraphQL.Types
{
    public interface INamedType
    {
        string Name { get; set; }
    }

    public interface IGraphType : IProvideMetadata, INamedType
    {
        string Description { get; set; }
        string DeprecationReason { get; set; }

        string CollectTypes(TypeCollectionContext context);
    }
}
