using GraphQL.Builders;
using GraphQL.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GraphQL.Types
{
    public class InputObjectGraphType : ComplexGraphType<object>
    {
    }



    public class InputObjectGraphType<TSourceType> : InputObjectGraphType
    {

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

        public FieldBuilder<TSourceType, TProperty> Field<TProperty>(
            Expression<Func<TSourceType, TProperty>> expression,
            bool nullable = false,
            Type type = null)
        {
            string name;
            try
            {
                name = expression.NameOf();
            }
            catch
            {
                throw new ArgumentException(
                    $"Cannot infer a Field name from the expression: '{expression.Body.ToString()}' " +
                    $"on parent GraphQL type: '{Name ?? GetType().Name}'.");
            }
            return Field(name, expression, nullable, type);
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

    }
}

