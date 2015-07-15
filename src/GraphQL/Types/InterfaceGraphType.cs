using System;
using System.Linq;

namespace GraphQL.Types
{
    public class InterfaceGraphType : ObjectGraphType
    {
        public Func<object, ObjectGraphType> ResolveType { get; set; }

        public bool IsPossibleType(IImplementInterfaces type)
        {
            return type != null
                ? type.Interfaces.Any(i => i.Name == this.Name)
                : false;
        }
    }
}
