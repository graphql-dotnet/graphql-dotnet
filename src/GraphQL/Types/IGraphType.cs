using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IGraphType : IProvideMetadata
    {
        string Name { get; }
        string Description { get; }
        string DeprecationReason { get; }

        string CollectTypes(TypeCollectionContext context);
    }

    public interface IOutputGraphType : IGraphType
    {
    }

    public interface IInputGraphType : IGraphType
    {
    }
}
