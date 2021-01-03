using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    /// <summary>
    /// Provides properties for enumerating supported GraphQL interface types for an output graph type.
    /// </summary>
    public interface IImplementInterfaces
    {
        /// <summary>
        /// Gets or sets a list of .NET types of supported GraphQL interface types.
        /// </summary>
        IEnumerable<Type> Interfaces { get; set; }

        /// <summary>
        /// Gets or sets a list of instances of supported GraphQL interface types.
        /// </summary>
        IEnumerable<IInterfaceGraphType> ResolvedInterfaces { get; set; }
    }
}
