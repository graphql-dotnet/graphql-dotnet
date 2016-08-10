using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQl.SchemaGenerator.Attributes;
using GraphQl.SchemaGenerator.Helpers;
using GraphQl.SchemaGenerator.Models;
using GraphQl.SchemaGenerator.Schema;
using GraphQl.SchemaGenerator.Wrappers;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator
{
    public class SchemaGenerator
    {
        private IGraphFieldResolver FieldResolver { get; }
        private IGraphTypeResolver TypeResolver { get; }

        public SchemaGenerator(IGraphFieldResolver fieldResolver, IGraphTypeResolver typeResolver)
        {
            FieldResolver = fieldResolver;
            TypeResolver = typeResolver;
        }

        /// <summary>
        ///     Create field definitions based off a type.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public IEnumerable<FieldDefinition> CreateDefinitions(params Type[] types)
        {
            var definitions = new List<FieldDefinition>();

            foreach (var type in types)
            {
                foreach (var method in type.GetMethods())
                {
                    var graphRoute = method.GetCustomAttributes(typeof(GraphRouteAttribute), true)
                        .OfType<GraphRouteAttribute>()
                        .FirstOrDefault();

                    if (graphRoute == null)
                    {
                        continue;
                    }
                    
                    var parameters = method.GetParameters();
                    var arguments = CreateArguments(parameters);
                    var field = new FieldInformation
                    {
                        IsMutation = graphRoute.IsMutation,
                        Arguments =  arguments,
                        Name = !String.IsNullOrWhiteSpace(graphRoute.Name) ? graphRoute.Name : method.Name,
                        Response = graphRoute.ResponseType,
                        Method = method
                    };

                    definitions.Add(new FieldDefinition(field, (context) => FieldResolver.ResolveField(context, field)));
                }
            }

            return definitions;
        }


        /// <summary>
        ///     Helper method to create schema from types.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public GraphQL.Types.Schema CreateSchema(params Type[] types)
        {
            return CreateSchema(CreateDefinitions(types));
        }

        public GraphQL.Types.Schema CreateSchema(
            IEnumerable<FieldDefinition> definitions)
        {
            var mutation = new ObjectGraphType();
            var query = new ObjectGraphType();

            foreach (var definition in definitions)
            {
                if (definition.Field == null)
                {
                    continue;
                }

                var type = EnsureGraphType(definition.Field.Response);

                if (definition.Field.IsMutation){
                    mutation.Field(
                        type,
                        definition.Field.Name,
                        arguments: definition.Field.Arguments,
                        resolve: definition.Resolve);
                }
                else
                {
                    query.Field(
                        type,
                        definition.Field.Name,
                        arguments: definition.Field.Arguments,
                        resolve: definition.Resolve);
                }
            }

            return new GraphQL.Types.Schema(CreateGraphType)
            {                
                Mutation = mutation.Fields.Any() ? mutation : null,
                Query = query.Fields.Any() ? query : null
            };
        }

        public GraphType CreateGraphType(Type type)
        {
            return TypeResolver.ResolveType(type);
        }

        public static Type EnsureGraphType(Type parameterType)
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

        public static QueryArguments CreateArguments(IEnumerable<ParameterInfo> parameters)
        {
            var arguments = new List<QueryArgument>();

            foreach (var parameter in parameters)
            {
                var argument = CreateArgument(parameter);
                arguments.Add(argument);
            }

            return new QueryArguments(arguments);
        }

        private static QueryArgument CreateArgument(ParameterInfo parameter)
        {
            var requestArgumentType = GetRequestArgumentType(parameter.ParameterType);

            var argument = (QueryArgument)Activator.CreateInstance(requestArgumentType);
            argument.Name = parameter.Name;

            return argument;
        }

        private static Type GetRequestArgumentType(Type parameterType)
        {
            var requestType = GraphTypeConverter.ConvertTypeToGraphType(parameterType);
            if (typeof(ObjectGraphType).IsAssignableFrom(requestType))
            {
                // rewrap as InputObjectGraphTypeWrapper for use as inputs - all QueryArguments used as mutations should be 
                // InputGraphObjectType instead of ObjectGraphType
                requestType = typeof(InputObjectGraphTypeWrapper<>).MakeGenericType(requestType.GetGenericArguments()[0]);
            }

            var requestArgumentType = typeof(QueryArgument<>).MakeGenericType(EnsureNonNull(requestType));

            return requestArgumentType;
        }

        private static Type EnsureNonNull(Type requestType)
        {
            if (!typeof(NonNullGraphType).IsAssignableFrom(requestType))
            {
                return typeof(NonNullGraphType<>).MakeGenericType(requestType);
            }

            return requestType;
        }
    }
}
