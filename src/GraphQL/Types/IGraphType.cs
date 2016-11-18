namespace GraphQL.Types
{
    public interface IGraphType : IProvideMetadata
    {
        string Name { get; set; }
        string Description { get; set; }
        string DeprecationReason { get; set; }

        string CollectTypes(TypeCollectionContext context);
    }

    public interface IOutputGraphType : IGraphType
    {
    }

    public interface IInputGraphType : IGraphType
    {
    }
}
