using GraphQL.Builders;
using GraphQL.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace GraphQL.Types
{
    public interface IComplexGraphType : IGraphType
    {
        IEnumerable<FieldType> Fields { get; }

        FieldType AddField(FieldType fieldType);

        bool HasField(string name);
    }

    public abstract class ComplexGraphType<TSourceType> : GraphType, IComplexGraphType
    {
        private readonly List<FieldType> _fields = new List<FieldType>();

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

        public FieldType AddField(FieldType fieldType)
        {
            if (HasField(fieldType.Name))
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType.Name), 
                    $"A field with the name: {fieldType.Name} is already registered for GraphType: {Name ?? this.GetType().Name}");
            }

            if (fieldType.ResolvedType == null && !fieldType.Type.IsGraphType())
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType.Type), 
                    $"The declared Field type: {fieldType.Type.Name} should derive from GraphType, but doesn't.");
            }

            _fields.Add(fieldType);

            return fieldType;
        }

        public FieldType Field(
            Type type,
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, object> resolve = null,
            string deprecationReason = null)
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = type,
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(resolve)
                    : null,
            });
        }

        public FieldType Field<TGraphType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, object> resolve = null,
            string deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, object>(resolve)
                    : null,
            });
        }

        public FieldType FieldAsync<TGraphType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, Task<object>> resolve = null,
            string deprecationReason = null)
            where TGraphType : IGraphType
        {
            return AddField(new FieldType
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = typeof(TGraphType),
                Arguments = arguments,
                Resolver = resolve != null
                    ? new FuncFieldResolver<TSourceType, Task<object>>(resolve)
                    : null,
            });
        }

        public FieldBuilder<TSourceType, TReturnType> Field<TGraphType, TReturnType>()
        {
            var builder = FieldBuilder.Create<TSourceType, TReturnType>(typeof(TGraphType));
            AddField(builder.FieldType);
            return builder;
        }

        public FieldBuilder<TSourceType, object> Field<TGraphType>()
        {
            var builder = FieldBuilder.Create<TSourceType, object>(typeof(TGraphType));
            AddField(builder.FieldType);
            return builder;
        }

        public FieldBuilder<TSourceType, TProperty> Field<TProperty>(
           string name,
           Expression<Func<TSourceType, TProperty>> expression,
           bool nullable = false,
           Type type = null)
        {
            try
            {
                if (type == null)
                    type = typeof(TProperty).GetGraphTypeFromType(nullable);
            }
            catch (ArgumentOutOfRangeException exp)
            {
                throw new ArgumentException(
                    $"The GraphQL type for Field: '{name}' on parent type: '{Name ?? GetType().Name}' could not be derived implicitly. \n",
                    exp
                 );
            }

            var builder = FieldBuilder.Create<TSourceType, TProperty>(type)
                .Resolve(new ExpressionFieldResolver<TSourceType, TProperty>(expression))
                .Name(name);

            AddField(builder.FieldType);
            return builder;
        }

        public FieldBuilder<TSourceType, TProperty> Field<TProperty>(
            Expression<Func<TSourceType, TProperty>> expression,
            bool nullable = false,
            Type type = null)
        {
            string name;
            try {
                name = expression.NameOf().ToCamelCase();
            }
            catch {
                throw new ArgumentException(
                    $"Cannot infer a Field name from the expression: '{expression.Body.ToString()}' " +
                    $"on parent GraphQL type: '{Name ?? GetType().Name}'.");
            }
            return Field(name, expression, nullable, type);
        }


        public ConnectionBuilder<TNodeType, TSourceType> Connection<TNodeType>()
            where TNodeType : IGraphType
        {
            var builder = ConnectionBuilder.Create<TNodeType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }
    }
}
