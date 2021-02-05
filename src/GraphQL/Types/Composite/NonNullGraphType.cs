using System;

namespace GraphQL.Types
{
    /// <inheritdoc cref="NonNullGraphType"/>
    public class NonNullGraphType<T> : NonNullGraphType
        where T : GraphType
    {
        /// <inheritdoc cref="NonNullGraphType(Type)"/>
        public NonNullGraphType()
            : base(typeof(T))
        {
        }
    }

    /// <summary>
    /// Represents a graph type that, for output graphs, is never null, or for input graphs, is not optional.
    /// In other words the NonNull type wraps another type, and denotes that the resulting value will never be null.
    /// </summary>
    public class NonNullGraphType : GraphType, IProvideResolvedType
    {
        /// <summary>
        /// Initializes a new instance for the specified inner graph type.
        /// </summary>
        public NonNullGraphType(IGraphType type)
        {
            if (type is NonNullGraphType)
            {
                // http://spec.graphql.org/draft/#sec-Type-System.Non-Null.Type-Validation
                throw new ArgumentOutOfRangeException(nameof(type), "Cannot nest NonNull inside NonNull.");
            }

            ResolvedType = type;
        }

        /// <inheritdoc cref="NonNullGraphType(IGraphType)"/>
        protected NonNullGraphType(Type type)
        {
            if (typeof(NonNullGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Cannot nest NonNull inside NonNull.");
            }

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
        public override string ToString() => $"{ResolvedType}!";
    }
}
