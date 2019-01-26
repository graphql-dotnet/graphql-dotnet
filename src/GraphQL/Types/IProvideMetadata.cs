using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IProvideMetadata
    {
        IDictionary<string, object> Metadata { get; }
        TType GetMetadata<TType>(string key, TType defaultValue = default);
        bool HasMetadata(string key);
    }
}
