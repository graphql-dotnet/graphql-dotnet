using System;

namespace GraphQL.Types
{
    public interface IHaveDefaultValue
    {
        object DefaultValue { get; }
        Type Type { get; }
        IGraphType ResolvedType { get; }
    }
}
