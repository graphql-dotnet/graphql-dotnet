using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IGraphType
    {
        string Name { get; }
        string Description { get; }
        string DeprecationReason { get; }
        IDictionary<string, object> Metadata { get; }

        TType GetMetadata<TType>(string key, TType defaultValue = default(TType));
        bool HasMetadata(string key);

        string CollectTypes(TypeCollectionContext context);
    }

    public interface IOutputGraphType : IGraphType
    {
    }

    public interface IInputGraphType : IGraphType
    {
    }
}
