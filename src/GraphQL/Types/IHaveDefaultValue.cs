using System;

namespace GraphQL.Types
{
    public interface IHaveDefaultValue
    {
        object DefaultValue { get; }
        IGraphType Type { get; }
    }
}
