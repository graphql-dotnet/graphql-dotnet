using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IProvideMetadata
    {
        IDictionary<string, object> Metadata { get; }
        TType GetMetadata<TType>(string key, TType defaultValue = default);
        TType GetMetadata<TType>(string key, Func<TType> defaultValueFactory);
        bool HasMetadata(string key);
    }
}
