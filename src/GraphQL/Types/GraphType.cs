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

        public bool HasField(string name)
        {
            return _fields.Any(x => string.Equals(x.Name, name));
        }

        public FieldBuilder<TGraphType, object, TGraphType> Field<TGraphType>()
            where TGraphType : GraphType
        {
            return Field<TGraphType, object>();
        }

        public FieldBuilder<TGraphType, TSourceType, TGraphType> Field<TGraphType, TSourceType>()
            where TGraphType : GraphType
        {
            var builder = FieldBuilder.Create<TGraphType, TSourceType>();
            _fields.Add(builder.FieldType);
            return builder;
        }

        public ConnectionBuilder<TGraphType, object> Connection<TGraphType>()
            where TGraphType : ObjectGraphType
        {
            return Connection<TGraphType, object>();
        }

        public ConnectionBuilder<TGraphType, TSourceType> Connection<TGraphType, TSourceType>()
            where TGraphType : ObjectGraphType
        {
            var builder = ConnectionBuilder.Create<TGraphType, TSourceType>();
            _fields.Add(builder.FieldType);
            return builder;
        }

        public FieldType Field(
            Type type,
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null,
            string deprecationReason = null)
        {
            if (_fields.Exists(x => x.Name == name))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"A field with the name '{name}' is already registered.");
            }

            if (!type.IsSubclassOf(typeof(GraphType)))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Field type must derive from GraphType.");
            }

            var fieldType = new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
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
            Func<ResolveFieldContext, object> resolve = null,
            string deprecationReason = null)
            where TType : GraphType
        {
            return Field(typeof(TType), name, description, arguments, resolve, deprecationReason);
        }

        public FieldType Field<TSource, TType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSource>, object> resolve = null,
            string deprecationReason = null)
            where TType : GraphType
        {
            Func<ResolveFieldContext, object> resolver = 
                context => resolve(new ResolveFieldContext<TSource>(context));

            return Field(typeof(TType), name, description, arguments, resolver, deprecationReason);
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
            return Name?.GetHashCode() ?? 0;
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
