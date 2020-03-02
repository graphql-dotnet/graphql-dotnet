using GraphQL.Resolvers;

namespace GraphQL.Types
{
    public interface IFieldType : IHaveDefaultValue, IProvideMetadata, INamedType
    {
        string DeprecationReason { get; }

        QueryArguments Arguments { get; }

        IFieldResolver Resolver { get; }
    }
}
