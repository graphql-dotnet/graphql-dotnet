using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public interface IImplementInterfaces
    {
        IEnumerable<IInterfaceGraphType> Interfaces { get; }
    }
}
