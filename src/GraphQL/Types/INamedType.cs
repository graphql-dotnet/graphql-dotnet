namespace GraphQL.Types
{
    public interface INamedType
    {
        string Name { get; }

        string Description { get; }
    }
}
