using System;
using GraphQL.SchemaGenerator.Wrappers;
using GraphQL.Types;

namespace GraphQL.SchemaGenerator
{
    public class GraphTypeResolver : IGraphTypeResolver
    {
        /// <summary>
        ///     Resolve a type into a graph type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public GraphType ResolveType(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                return null;
            }

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(InterfaceGraphTypeWrapper<>))
                {
                    return null;
                }

                if (type.GetGenericTypeDefinition() == typeof(InputObjectGraphTypeWrapper<>))
                {
                    return Activator.CreateInstance(type) as GraphType;
                }

                if (type.GetGenericTypeDefinition() == typeof(EnumerationGraphTypeWrapper<>))
                {
                    return Activator.CreateInstance(type) as GraphType;
                }

                if (type.GetGenericTypeDefinition() == typeof(ObjectGraphTypeWrapper<>))
                {
                    return Activator.CreateInstance(type) as GraphType;
                }
            }

            if (type.IsAssignableFrom(typeof(GraphType)))
            {
                return Activator.CreateInstance(type) as GraphType;
            }

            var graphType = GraphTypeConverter.ConvertTypeToGraphType(type);

            if (graphType == null)
            {
                return null;
            }

            var generic = typeof(ObjectGraphTypeWrapper<>).MakeGenericType(graphType);

            return Activator.CreateInstance(generic) as GraphType;
        }
    }
}

