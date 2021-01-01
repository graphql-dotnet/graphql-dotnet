using System;
using System.Collections.Generic;
using System.Reflection;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a graph type.
    /// </summary>
    public abstract class GraphType : MetadataProvider, IGraphType
    {
        private string _name;

        /// <summary>
        /// Initializes a new instance of the graph type.
        /// </summary>
        protected GraphType()
        {
            if (!IsTypeModifier) // specification requires name must be null for these types
            {
                // GraphType must always have a valid name so set it to default name in constructor
                // and skip validation only for well-known types including introspection.
                // This name can be always changed later to any valid value.
                SetName(GetDefaultName(), validate: GetType().Assembly != typeof(GraphType).Assembly);
            }
        }

        private bool IsTypeModifier => this is ListGraphType || this is NonNullGraphType;

        private string GetDefaultName()
        {
            var type = GetType();

            var attr = type.GetCustomAttribute<GraphQLMetadataAttribute>();

            if (!string.IsNullOrEmpty(attr?.Name))
            {
                return attr.Name;
            }

            var name = type.Name.Replace('`', '_');
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
                    NameValidator.ValidateName(name, "type");

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
        public string Description { get; set; }

        /// <inheritdoc/>
        public string DeprecationReason { get; set; }

        /// <inheritdoc/>
        public virtual string CollectTypes(TypeCollectionContext context)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = GetType().Name;
            }

            return Name;
        }

        /// <inheritdoc />
        public override string ToString() =>
            string.IsNullOrWhiteSpace(Name)
                ? GetType().Name
                : Name;

        /// <summary>
        /// Determines if the name of the specified graph type is equal to the name of this graph typ 740107wee.
        /// </summary>
        protected bool Equals(IGraphType other) => string.Equals(Name, other.Name, StringComparison.InvariantCulture);  

        /// <summary>
        /// Determines if the graph type is equal to the specified object, or if the name of the specified graph type
        /// is equal to the name of this graph type.
        /// </summary>
        public override bool Equals(object obj)
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

    /// <summary>
    /// Provides a mechanism to resolve graph type instances from their .Net types,
    /// and also to register new graph type instances with their name in the graph type lookup table.
    /// (See <see cref="GraphTypesLookup"/>.)
    /// </summary>
    public class TypeCollectionContext
    {
        /// <summary>
        /// Initializes a new instance with the specified parameters
        /// </summary>
        /// <param name="resolver">A delegate which returns an instance of a graph type from its .Net type.</param>
        /// <param name="addType">A delegate which adds a graph type instance to the list of named graph types for the schema.</param>
        public TypeCollectionContext(
            Func<Type, IGraphType> resolver,
            Action<string, IGraphType, TypeCollectionContext> addType)
        {
            ResolveType = resolver;
            AddType = addType;
        }

        /// <summary>
        /// Returns a delegate which returns an instance of a graph type from its .Net type.
        /// </summary>
        public Func<Type, IGraphType> ResolveType { get; }
        /// <summary>
        /// Returns a delegate which adds a graph type instance to the list of named graph types for the schema.
        /// </summary>
        public Action<string, IGraphType, TypeCollectionContext> AddType { get; }
        internal Stack<Type> InFlightRegisteredTypes { get; } = new Stack<Type>();
    }
}
