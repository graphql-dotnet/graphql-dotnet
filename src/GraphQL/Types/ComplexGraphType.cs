using GraphQL.Builders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public abstract class ComplexGraphType : GraphType {
        private readonly List<FieldType> _fields = new List<FieldType>();

        public List<FieldType> Fields
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

        public FieldType Field(FieldType fieldType)
        {
            if (HasField(fieldType.Name))
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType.Name), "A field with that name is already registered.");
            }

            if (!fieldType.Type.IsSubclassOf(typeof(GraphType)))
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType.Type), "Field type must derive from GraphType.");
            }

            _fields.Add(fieldType);

            return fieldType;
        }

        public FieldType Field(
            Type type,
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null,
            string deprecationReason = null)
        {

            return Field(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = type,
                Arguments = arguments,
                Resolve = resolve,
            });
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

        public FieldBuilder<TGraphType, object, object> Field<TGraphType>()
            where TGraphType : GraphType
        {
            var builder = FieldBuilder.Create<TGraphType, object>();
            Field(builder.FieldType);
            return builder;
        }
    }
}
