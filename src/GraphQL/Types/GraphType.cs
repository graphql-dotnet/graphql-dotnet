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

        public ConnectionBuilder<TParentType, TGraphType, object> Connection<TParentType, TGraphType>()
            where TParentType : GraphType
            where TGraphType : ObjectGraphType, new()
        {
            var builder = ConnectionBuilder.Create<TParentType, TGraphType>();
            _fields.Add(builder.FieldType);
            return builder;
        }

        public FieldType Field<TType>(
            string name, 
            string description = null, 
            QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
            where TType : GraphType
        {
            if (_fields.Exists(x => x.Name == name))
            {
                throw new ArgumentOutOfRangeException("name", "A field with that name is already registered.");
            }

            var fieldType = new FieldType
            {
                Name = name,
                Description = description,
                Type = typeof(TType),
                Arguments = arguments,
                Resolve = resolve,
            };

            _fields.Add(fieldType);

            return fieldType;
        }

        public virtual string CollectTypes(TypeCollectionContext context)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = GetType().Name;
            }

            return Name;
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
