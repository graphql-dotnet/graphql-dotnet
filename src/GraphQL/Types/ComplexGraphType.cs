using GraphQL;
using GraphQL.Builders;
using GraphQL.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

        public ComplexGraphType() { }

        public ComplexGraphType(Action<ComplexGraphType<TSourceType>> configure) {
            configure(this);
        }

        public bool HasField(string name)
        {
            return _fields.Any(x => string.Equals(x.Name, name));
        }

        public FieldType AddField(FieldType fieldType)
        {
            if (HasField(fieldType.Name))
            {
                throw new ArgumentOutOfRangeException(nameof(fieldType.Name), "A field with that name is already registered.");
            }

            _fields.Add(fieldType);

            return fieldType;
        }

        public FieldType Field(
            IGraphType type,
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


        //public FieldBuilder<TSourceType, TReturnType> Field<TReturnType>()
        //{
        //    var builder = FieldBuilder.Create<TSourceType, TReturnType>(typeof(TGraphType));
        //    AddField(builder.FieldType);
        //    return builder;
        //}

        //public FieldBuilder<TSourceType, object> Field<TGraphType>()
        //{
        //    var builder = FieldBuilder.Create<TSourceType, object>(typeof(TGraphType));
        //    AddField(builder.FieldType);
        //    return builder;
        //}

        public FieldBuilder<TSourceType, TProperty> Field<TProperty>(
           string name,
           Expression<Func<TSourceType, TProperty>> expression,
           bool nullable = false,
           IGraphType type = null)
        {
            type = type ?? typeof(TProperty).GetGraphTypeFromType(nullable);

            var builder = FieldBuilder.Create<TSourceType, TProperty>(type)
                .Resolve(new ExpressionFieldResolver<TSourceType, TProperty>(expression))
                .Name(name);

            AddField(builder.FieldType);
            return builder;
        }

        public FieldBuilder<TSourceType, TProperty> Field<TProperty>(
            Expression<Func<TSourceType, TProperty>> expression,
            bool nullable = false,
            IGraphType type = null)
        {
            string name;
            try
            {
                name = expression.NameOf().ToCamelCase();
            }
            catch
            {
                throw new ArgumentException("Cannot infer a Field name from the provided expression");
            }
            return Field(name, expression, nullable, type);
        }

    }
}
