using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Types
{
    public interface IProvideResolvedType
    {
        IGraphType ResolvedType { get; }
    }
}
