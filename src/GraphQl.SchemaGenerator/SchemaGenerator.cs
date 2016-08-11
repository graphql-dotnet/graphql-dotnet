using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.SchemaGenerator.Attributes;
using GraphQL.SchemaGenerator.Extensions;
using GraphQL.SchemaGenerator.Helpers;
using GraphQL.SchemaGenerator.Models;
using GraphQL.SchemaGenerator.Wrappers;
using GraphQL.Types;

namespace GraphQL.SchemaGenerator
{
    public class SchemaGenerator
    {
        private IServiceProvider ServiceProvider { get; }
        private IGraphTypeResolver TypeResolver { get; }

        public SchemaGenerator(IServiceProvider serviceProvider, IGraphTypeResolver typeResolver)
        {
            ServiceProvider = serviceProvider;
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
                    var response = method.ReturnType;

                    if (response.IsGenericType && response.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        response = response.GenericTypeArguments.First();
                    }

                    var field = new FieldInformation
                    {
                        IsMutation = graphRoute.IsMutation,
                        Arguments =  arguments,
                        Name = !String.IsNullOrWhiteSpace(graphRoute.Name) ? graphRoute.Name : StringHelper.ConvertToCamelCase(method.Name),
                        Response = response,
                        Method = method
                    };

                    definitions.Add(new FieldDefinition(field, (context) => ResolveField(context, field)));
                }
            }

            return definitions;
        }

        public object ResolveField(ResolveFieldContext context, FieldInformation field)
        {
            var classObject = ServiceProvider.GetService(field.Method.DeclaringType);
            var result = field.Method.Invoke(classObject, context.Parameters(field));

            //todo async support.

            return result;
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

                if (type == null)
                {
                    continue;
                }

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

            //dont force everything to be required.
            //var requestArgumentType = typeof(QueryArgument<>).MakeGenericType(EnsureNonNull(requestType));

            var requestArgumentType = typeof(QueryArgument<>).MakeGenericType(requestType);

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
