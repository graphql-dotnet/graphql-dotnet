using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphQL
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Conditionally casts the item into the indicated type using an "as" cast.
        /// </summary>
        /// <typeparam name="T">The desired type</typeparam>
        /// <param name="item">The item.</param>
        /// <returns><c>null</c> if the cast failed, otherwise item as T</returns>
        public static T As<T>(this object item)
            where T : class
        {
            return item as T;
        }

        /// <summary>
        /// Determines whether this instance is a concrete type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is neither abstract nor an interface; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsConcrete(this Type type)
        {
            if (type == null) return false;

            var typeInfo = type.GetTypeInfo();

            return !typeInfo.IsAbstract && !typeInfo.IsInterface;
        }

        /// <summary>
        /// Determines whether this instance is a subclass of Nullable&lt;T&gt;.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is a subclass of Nullable&lt;T&gt;; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullable(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Returns the first non-null value from executing the func against the enumerable
        /// </summary>
        /// <returns><c>null</c> is all values were null.</returns>
        public static TReturn FirstValue<TItem, TReturn>(this IEnumerable<TItem> enumerable, Func<TItem, TReturn> func)
            where TReturn : class
        {
            foreach (TItem item in enumerable)
            {
                TReturn @object = func(item);
                if (@object != null) return @object;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the indicated type implements IGraphType.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the indicated type implements IGraphType; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsGraphType(this Type type)
        {
            return type.GetInterfaces().Contains(typeof(IGraphType));
        }

        /// <summary>
        /// Gets the GraphQL name of the type. This is derived from the type name and can be overridden by the GraphQLMetadata Attribute.
        /// </summary>
        /// <param name="type">The indicated type.</param>
        /// <returns>A string containing a GraphQL compatible type name.</returns>
        public static string GraphQLName(this Type type)
        {
            var attr = type.GetTypeInfo().GetCustomAttribute<GraphQLMetadataAttribute>();

            if (attr != null)
            {
                return attr.Name;
            }

            var typeName = type.Name;

            if (type.GetTypeInfo().IsGenericType)
            {
                typeName = typeName.Substring(0, typeName.IndexOf('`'));
            }

            typeName = typeName.Replace(nameof(GraphType), nameof(Type));

            return typeName.EndsWith(nameof(Type))
                ? typeName.Remove(typeName.Length - nameof(Type).Length)
                : typeName;
        }

        /// <summary>
        /// Gets the graph type for the indicated type.
        /// </summary>
        /// <param name="type">The type for which a graph type is desired.</param>
        /// <param name="isNullable">if set to <c>false</c> if the type explicitly non-nullable.</param>
        /// <returns>A Type object representing a GraphType that matches the indicated type.</returns>
        /// <remarks>This can handle arrays and lists, but not other collection types.</remarks>
        public static Type GetGraphTypeFromType(this Type type, bool isNullable = false)
        {
            TypeInfo info = type.GetTypeInfo();
            Type graphType = null;

            if (info.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
                if (isNullable == false)
                {
                    throw new ArgumentOutOfRangeException(nameof(isNullable),
                        $"Explicitly nullable type: Nullable<{type.Name}> cannot be coerced to a non nullable GraphQL type. \n");
                }
            }

            graphType = GraphQL.GraphTypeRegistry.Get(type);

            if (type.IsArray)
            {
                var elementType = GetGraphTypeFromType(type.GetElementType(), isNullable);
                var listType = typeof(ListGraphType<>);
                graphType = listType.MakeGenericType(elementType);
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = GetGraphTypeFromType(type.GenericTypeArguments.First(), isNullable);
                var listType = typeof(ListGraphType<>);
                graphType = listType.MakeGenericType(elementType);
            }

            if (graphType == null)
            {
                throw new ArgumentOutOfRangeException(nameof(type),
                    $"The type: {type.Name} cannot be coerced effectively to a GraphQL type");
            }

            if (!isNullable)
            {
                var nullType = typeof(NonNullGraphType<>);
                graphType = nullType.MakeGenericType(graphType);
            }

            return graphType;
        }
    }
}
