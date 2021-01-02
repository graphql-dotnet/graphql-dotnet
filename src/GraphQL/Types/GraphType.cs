using System;
using System.Reflection;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    public abstract class GraphType : MetadataProvider, IGraphType
    {
        private string _name;

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

        /// <summary>
        /// Type name that must conform to the specification: https://graphql.github.io/graphql-spec/June2018/#sec-Names
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetName(value, validate: true);
        }

        public string Description { get; set; }

        public string DeprecationReason { get; set; }

        public override string ToString() => Name;

        protected bool Equals(IGraphType other) => string.Equals(Name, other.Name, StringComparison.InvariantCulture);

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

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
    }
}
