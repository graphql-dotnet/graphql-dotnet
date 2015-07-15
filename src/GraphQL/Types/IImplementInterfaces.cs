using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Types
{
    public interface IImplementInterfaces
    {
        IEnumerable<InterfaceGraphType> Interfaces { get; }
    }
}
