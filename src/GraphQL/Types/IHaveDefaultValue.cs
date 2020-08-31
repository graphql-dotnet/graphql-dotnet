using System;

namespace GraphQL.Types
{
    public interface IHaveDefaultValue : IProvideResolvedType
    {
        object DefaultValue { get; }
        Type Type { get; }
    }
}
