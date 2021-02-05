using System;

namespace GraphQL.Types
{
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
            ResolvedType = type;
        }

        /// <summary>
        /// Returns the .NET type of the inner (wrapped) graph type.
        /// </summary>
        public virtual Type Type => null;

        private IGraphType _resolvedType;

        /// <summary>
        /// Gets or sets the instance of the inner (wrapped) graph type.
        /// </summary>
        public IGraphType ResolvedType
        {
            get => _resolvedType;
            set
            {
                if (value is NonNullGraphType) //TODO: null check here or in ctor
                {
                    // http://spec.graphql.org/draft/#sec-Type-System.Non-Null.Type-Validation
                    throw new ArgumentOutOfRangeException("type", "Cannot nest NonNull inside NonNull.");
                }

                if (value != null && Type != null && !Type.IsAssignableFrom(value.GetType()))
                    throw new InvalidOperationException($"Type '{Type.Name}' should be assignable from ResolvedType '{value.Name}'.");

                _resolvedType = value;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{ResolvedType}!";
    }

    /// <inheritdoc cref="NonNullGraphType"/>
    public class NonNullGraphType<T> : NonNullGraphType
        where T : GraphType
    {
        /// <summary>
        /// Initializes a new instance for the specified inner graph type.
        /// </summary>
        public NonNullGraphType()
            : base(null)
        {
            if (typeof(NonNullGraphType).IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentOutOfRangeException("type", "Cannot nest NonNull inside NonNull.");
            }
        }

        /// <inheritdoc/>
        public override Type Type => typeof(T);
    }
}
