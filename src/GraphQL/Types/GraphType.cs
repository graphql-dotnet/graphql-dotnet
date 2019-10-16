using GraphQL.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                // GraphType must always have a valid name so set it to default name in ctor.
                // This name can be always changed later to any valid value.
                var name = GetType().Name.Replace('`', '_');
                if (name.EndsWith(nameof(GraphType), StringComparison.InvariantCulture))
                    name = name.Substring(0, name.Length - nameof(GraphType).Length);

                // skip validation only for well-known types including introspection 
                SetName(name, validate: GetType().Assembly != typeof(GraphType).Assembly);
            }
        }

        private bool IsTypeModifier => this is ListGraphType || this is NonNullGraphType;

        internal void SetName(string name, bool validate)
        {
            if (_name != name)
            {
                if (validate)
                {
                    NameValidator.ValidateName(name, "type");

                    if (IsTypeModifier)
                        throw new ArgumentOutOfRangeException("A type modifier (List, NonNull) name must be null");
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
            set => SetName(value, true);
        }

        public string Description { get; set; }

        public string DeprecationReason { get; set; }

        public virtual string CollectTypes(TypeCollectionContext context)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = GetType().Name;
            }

            return Name;
        }

        public override string ToString() =>
            string.IsNullOrWhiteSpace(Name)
                ? GetType().Name
                : Name;

        protected bool Equals(IGraphType other) => string.Equals(Name, other.Name, StringComparison.InvariantCulture);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((IGraphType)obj);
        }

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// This sucks, find a better way
    /// </summary>
    public class TypeCollectionContext
    {
        public TypeCollectionContext(
            Func<Type, IGraphType> resolver,
            Action<string, IGraphType, TypeCollectionContext> addType)
        {
            ResolveType = resolver;
            AddType = addType;
        }

        public Func<Type, IGraphType> ResolveType { get; private set; }
        public Action<string, IGraphType, TypeCollectionContext> AddType { get; private set; }
    }
}
