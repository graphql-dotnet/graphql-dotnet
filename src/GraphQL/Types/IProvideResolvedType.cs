using System;

namespace GraphQL.Types
{
    public interface IProvideResolvedType
    {
        IGraphType ResolvedType { get; }

        /// <summary>
        /// Returns the graph type of this argument or field.
        /// </summary>
        Type Type { get; }
    }
}
