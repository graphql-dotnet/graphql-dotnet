using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQl.SchemaGenerator.Wrappers;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator
{
    public class GraphTypeResolver : IGraphTypeResolver
    {
        /// <summary>
        ///     Resolve a type into a graph type. This implementation will dynamically resolve types needed in object graph types.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public GraphType ResolveType(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                return null;
            }

            if (type.GetGenericTypeDefinition() == typeof(ObjectGraphTypeWrapper<>))
            {
               return Activator.CreateInstance(type) as GraphType;
            }

            if (type.IsAssignableFrom(typeof(GraphType)))
            {
                return Activator.CreateInstance(type) as GraphType;
            }

            var graphType = GraphTypeConverter.ConvertTypeToGraphType(type);
            var generic = typeof(ObjectGraphTypeWrapper<>).MakeGenericType(graphType);

            return Activator.CreateInstance(generic) as GraphType;
        }
    }
}

