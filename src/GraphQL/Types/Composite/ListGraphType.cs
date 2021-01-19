using System;

namespace GraphQL.Types
{
    /// <inheritdoc cref="ListGraphType"/>
    public class ListGraphType<T> : ListGraphType
        where T : IGraphType
    {
        /// <inheritdoc cref="ListGraphType.ListGraphType(Type)"/>
        public ListGraphType()
            : base(typeof(T))
        {
        }
    }

    /// <summary>
    /// Represents a list of objects. A GraphQL schema may describe that a field represents a list of another type.
    /// The List type is provided for this reason, and wraps another type.
    /// </summary>
    public class ListGraphType : GraphType, IProvideResolvedType
    {
        /// <summary>
        /// Initializes a new instance for the specified inner graph type.
        /// </summary>
        public ListGraphType(IGraphType type)
        {
            ResolvedType = type;
        }

        /// <inheritdoc cref="ListGraphType.ListGraphType(IGraphType)"/>
        protected ListGraphType(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Returns the .NET type of the inner (wrapped) graph type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets or sets the instance of the inner (wrapped) graph type.
        /// </summary>
        public IGraphType ResolvedType { get; set; }

        /// <inheritdoc/>
        public override string ToString() => $"[{ResolvedType}]";
    }
}
