namespace GraphQL.Types
{
    public interface IFieldType : IHaveDefaultValue, IProvideMetadata
    {
        string Name { get; set; }

        string Description { get; set; }

        string DeprecationReason { get; set; }

        QueryArguments Arguments { get; set; }
    }
}
