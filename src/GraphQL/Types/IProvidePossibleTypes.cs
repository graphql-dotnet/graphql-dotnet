using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IProvidePossibleTypes
    {
        IEnumerable<GraphType> PossibleTypes();
    }
}
