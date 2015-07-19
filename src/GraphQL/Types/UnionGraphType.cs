using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public class UnionGraphType : GraphType, IProvidePossibleTypes
    {
        public IEnumerable<GraphType> PossibleTypes()
        {
            throw new NotImplementedException();
        }
    }
}
