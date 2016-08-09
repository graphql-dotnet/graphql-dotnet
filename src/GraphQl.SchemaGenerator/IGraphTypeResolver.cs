using System;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator
{
    /// <summary>
    ///     Converts an unknown type to a graph type.
    /// </summary>
    public interface IGraphTypeResolver
    {
        GraphType ResolveType(Type type);
    }

}
