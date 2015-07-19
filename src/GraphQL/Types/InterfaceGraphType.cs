using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public class InterfaceGraphType : GraphType, IProvidePossibleTypes
    {
        public Func<object, ObjectGraphType> ResolveType { get; set; }

        public IEnumerable<GraphType> PossibleTypes()
        {
            throw new NotImplementedException();
        }

        public bool IsPossibleType(IImplementInterfaces type)
        {
            return type != null
                ? type.Interfaces.Any(i => i.Name == Name)
                : false;
        }
    }
}
