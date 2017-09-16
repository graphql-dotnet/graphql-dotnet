using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IImplementInterfaces
    {
        IEnumerable<Type> Interfaces { get; set; }
        IEnumerable<IInterfaceGraphType> ResolvedInterfaces { get; set; }
    }
}
