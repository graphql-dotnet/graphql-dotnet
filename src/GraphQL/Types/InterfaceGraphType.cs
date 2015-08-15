using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public class InterfaceGraphType : GraphType
    {
        public Func<object, ObjectGraphType> ResolveType { get; set; }

        public bool IsPossibleType(IEnumerable<GraphType> types)
        {
            return types.Any(i => i == this);
        }
    }
}
