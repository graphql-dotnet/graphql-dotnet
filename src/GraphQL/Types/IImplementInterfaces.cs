using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IImplementInterfaces
    {
        IEnumerable<Type> Interfaces { get; }
        IEnumerable<IInterfaceGraphType> ResolvedInterfaces { get; }
    }
}
