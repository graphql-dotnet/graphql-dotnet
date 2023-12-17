using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a graph type.
    /// </summary>
    public abstract class GraphType : MetadataProvider, IGraphType
    {
        private string _name;
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the graph type.
        /// </summary>
        protected GraphType()
            : this(null)
        {
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        internal GraphType(GraphType? cloneFrom)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            if (cloneFrom == null)
            {
                if (!IsTypeModifier) // specification requires name must be null for these types
                {
                    // GraphType must always have a valid name so set it to default name in constructor
                    // and skip validation only for well-known types including introspection.
                    // This name can be always changed later to any valid value.
                    SetName(GetDefaultName(), validate: GetType().Assembly != typeof(GraphType).Assembly);
                }
                return;
            }
            _name = cloneFrom._name;
            cloneFrom.CopyMetadataTo(this);
        }

        /// <inheritdoc/>
        public virtual void Initialize(ISchema schema)
        {
            if (_initialized)
                throw new InvalidOperationException($"This graph type '{GetType().GetFriendlyName()}' with name '{Name}' has already been initialized. Make sure that you do not use the same instance of a graph type in multiple schemas. It may be so if you registered this graph type as singleton; see https://graphql-dotnet.github.io/docs/getting-started/dependency-injection/ for more info.");
            _initialized = true;
        }

        private bool IsTypeModifier => this is ListGraphType || this is NonNullGraphType; // lgtm [cs/type-test-of-this]

        private string GetDefaultName()
        {
            var type = GetType();

            if (!GlobalSwitches.UseLegacyTypeNaming)
                return type.GraphQLName();

            string name = type.Name;
            if (GlobalSwitches.UseDeclaringTypeNames)
            {
                var parent = type.DeclaringType;
                while (parent != null)
                {
                    name = parent.Name + "_" + name;
                    parent = parent.DeclaringType;
                }
            }

            name = name.Replace('`', '_')
                       .Replace('@', '_'); // https://github.com/graphql-dotnet/graphql-dotnet/issues/3472
            if (name.EndsWith(nameof(GraphType), StringComparison.InvariantCulture))
                name = name.Substring(0, name.Length - nameof(GraphType).Length);

            return name;
        }

        internal void SetName(string name, bool validate)
        {
            if (_name != name)
            {
                if (validate)
                {
                    NameValidator.ValidateName(name, NamedElement.Type);

                    if (IsTypeModifier)
                        throw new ArgumentOutOfRangeException(nameof(name), "A type modifier (List, NonNull) name must be null");
                }

                _name = name;
            }
        }

        /// <inheritdoc/>
        public string Name
        {
            get => _name;
            set => SetName(value, validate: true);
        }

        /// <inheritdoc/>
        public string? Description { get; set; }

        /// <inheritdoc/>
        public string? DeprecationReason
        {
            get => this.GetDeprecationReason();
            set => this.SetDeprecationReason(value);
        }

        /// <inheritdoc />
        public override string ToString() => Name;

        /// <summary>
        /// Determines if the name of the specified graph type is equal to the name of this graph type.
        /// </summary>
        protected bool Equals(IGraphType other) => string.Equals(Name, other.Name, StringComparison.InvariantCulture);

        /// <summary>
        /// Determines if the graph type is equal to the specified object,
        /// or if the name of the specified graph type is equal to the name of this graph type.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((IGraphType)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
    }
}
