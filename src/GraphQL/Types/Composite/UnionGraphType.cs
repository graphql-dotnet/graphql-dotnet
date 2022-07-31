namespace GraphQL.Types
{
    /// <summary>
    /// Represents a GraphQL union graph type.
    /// </summary>
    public class UnionGraphType : GraphType, IAbstractGraphType
    {
        private List<Type>? _types;

        /// <inheritdoc/>
        public PossibleTypes PossibleTypes { get; } = new PossibleTypes();

        /// <inheritdoc/>
        public Func<object, IObjectGraphType?>? ResolveType { get; set; }

        /// <inheritdoc/>
        public void AddPossibleType(IObjectGraphType type)
        {
            PossibleTypes.Add(type);
        }

        /// <summary>
        /// Gets or sets a list of graph types that this union represents.
        /// </summary>
        public IEnumerable<Type> Types
        {
            get => _types ?? Enumerable.Empty<Type>();
            set
            {
                EnsureTypes();

                _types!.Clear();
                _types.AddRange(value);
            }
        }

        /// <summary>
        /// Adds a graph type to the list of graph types that this union represents.
        /// </summary>
        public void Type<TType>()
            where TType : IObjectGraphType
        {
            EnsureTypes();

            if (!_types!.Contains(typeof(TType)))
                _types.Add(typeof(TType));
        }

        /// <inheritdoc cref="Type{TType}"/>
        public void Type(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(IObjectGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Added union type '{type.Name}' must implement {nameof(IObjectGraphType)}", nameof(type));
            }

            EnsureTypes();

            if (!_types!.Contains(type))
                _types.Add(type);
        }

        private void EnsureTypes() => _types ??= new();
    }
}
