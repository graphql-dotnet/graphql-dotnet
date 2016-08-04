using System;
using System.Collections.Generic;
using System.Linq;
using GraphQl.SchemaGenerator.Definitions;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator
{
    public static class SchemaGenerator
    {
        public static GraphQL.Types.Schema CreateSchema(
            IEnumerable<FieldDefinition> fields)
        {
            var mutation = new ObjectGraphType();
            var query = new ObjectGraphType();

            foreach (var field in fields)
            {
                var type = EnsureGraphType(field.Response);

                if (field.IsMutation)
                {
                    mutation.Field(
                        type,
                        field.Name,
                        arguments: field.Arguments,
                        resolve: field.Resolve);
                }
                else
                {
                    query.Field(
                        type,
                        field.Name,
                        arguments: field.Arguments,
                        resolve: field.Resolve);
                }
            }

            return new GraphQL.Types.Schema
            {                
                Mutation = mutation.Fields.Any() ? mutation : null,
                Query = query.Fields.Any() ? query : null
            };
        }

        private static Type EnsureGraphType(Type parameterType)
        {
            if (parameterType == null)
            {
                return typeof(StringGraphType);
            }

            if (typeof(GraphType).IsAssignableFrom(parameterType))
            {
                return parameterType;
            }

            return GraphTypeConverter.ConvertTypeToGraphType(parameterType);
        }

    }
}
