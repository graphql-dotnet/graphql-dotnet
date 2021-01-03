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
    /// Represents a list of objects.
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
        /// Returns the .NET type of the inner graph type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets or sets the inner graph type.
        /// </summary>
        public IGraphType ResolvedType { get; set; }

        /// <inheritdoc/>
        public override string CollectTypes(TypeCollectionContext context)
        {
            var innerType = context.ResolveType(Type);
            ResolvedType = innerType;
            var name = innerType.CollectTypes(context);
            context.AddType(name, innerType, context);
            return $"[{name}]";
        }

        /// <inheritdoc/>
        public override string ToString() => $"[{ResolvedType}]";
    }
}
