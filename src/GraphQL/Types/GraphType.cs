using System;
using System.Collections.Generic;
using GraphQL.Builders;

namespace GraphQL.Types
{
    public abstract class GraphType
    {
        private readonly List<FieldType> _fields = new List<FieldType>();

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<FieldType> Fields
        {
            get { return _fields; }
            private set
            {
                _fields.Clear();
                _fields.AddRange(value);
            }
        }

        public FieldBuilder<TGraphType, object, TGraphType> Field<TGraphType>()
            where TGraphType : GraphType
        {
            var builder = FieldBuilder.Create<TGraphType>();
            _fields.Add(builder.FieldType);
            return builder;
        }

        public ConnectionBuilder<TGraphType, object> Connection<TGraphType>()
            where TGraphType : ObjectGraphType
        {
            var builder = ConnectionBuilder.Create<TGraphType>();
            _fields.Add(builder.FieldType);
            return builder;
        }

        public FieldType Field(
            Type type,
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
        {
            if (_fields.Exists(x => x.Name == name))
            {
                throw new ArgumentOutOfRangeException("name", "A field with that name is already registered.");
            }

            if (!type.IsSubclassOf(typeof(GraphType)))
            {
                throw new ArgumentOutOfRangeException("type", "Field type must derive from GraphType.");
            }

            var fieldType = new FieldType
            {
                Name = name,
                Description = description,
                Type = type,
                Arguments = arguments,
                Resolve = resolve,
            };

            _fields.Add(fieldType);

            return fieldType;
        }

        public FieldType Field<TType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
            where TType : GraphType
        {
            return Field(typeof(TType), name, description, arguments, resolve);
        }

        public virtual string CollectTypes(TypeCollectionContext context)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = GetType().Name;
            }

            return Name;
        }

        protected bool Equals(GraphType other)
        {
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GraphType) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }

    /// <summary>
    /// This sucks, find a better way
    /// </summary>
    public class TypeCollectionContext
    {
        public TypeCollectionContext(
            Func<Type, GraphType> resolver,
            Action<string, GraphType, TypeCollectionContext> addType)
        {
            ResolveType = resolver;
            AddType = addType;
        }

        public Func<Type, GraphType> ResolveType { get; private set; }
        public Action<string, GraphType, TypeCollectionContext> AddType { get; private set; }
    }
}
