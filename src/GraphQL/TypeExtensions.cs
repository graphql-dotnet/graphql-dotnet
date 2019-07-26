using GraphQL.Types;
using GraphQL.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphQL
{
    using System.Collections;
    using System.ComponentModel;

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

            return !type.IsAbstract && !type.IsInterface;
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
            return type == typeof(string) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
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
            var attr = type.GetCustomAttribute<GraphQLMetadataAttribute>();

            if (!string.IsNullOrEmpty(attr?.Name))
            {
                return attr.Name;
            }

            var typeName = type.Name;

            if (type.IsGenericType)
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
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
                if (isNullable == false)
                {
                    throw new ArgumentOutOfRangeException(nameof(isNullable),
                        $"Explicitly nullable type: Nullable<{type.Name}> cannot be coerced to a non nullable GraphQL type. \n");
                }
            }

            Type graphType;

            if (type.IsArray)
            {
                var clrElementType = type.GetElementType();
                var elementType = GetGraphTypeFromType(clrElementType, clrElementType.IsNullable()); // isNullable from elementType, not from parent array
                graphType = typeof(ListGraphType<>).MakeGenericType(elementType);
            }
            else if (IsAnIEnumerable(type))
            {
                var clrElementType = GetEnumerableElementType(type);
                var elementType = GetGraphTypeFromType(clrElementType, clrElementType.IsNullable()); // isNullable from elementType, not from parent container
                graphType = typeof(ListGraphType<>).MakeGenericType(elementType);
            }
            else
            {
                graphType = GraphTypeTypeRegistry.Get(type);
            }

            if (graphType == null)
            {
                if (type.IsEnum)
                {
                    graphType = typeof(EnumerationGraphType<>).MakeGenericType(type);
                }
                else
                    throw new ArgumentOutOfRangeException(nameof(type), $"The type: {type.Name} cannot be coerced effectively to a GraphQL type");
            }

            if (!isNullable)
            {
                graphType = typeof(NonNullGraphType<>).MakeGenericType(graphType);
            }

            return graphType;
        }

        /// <summary>
        /// Returns the friendly name of a type, using C# angle-bracket syntax for generics.
        /// </summary>
        /// <param name="type">The type of which you are inquiring.</param>
        /// <returns>A string representing the friendly name.</returns>
        internal static string GetFriendlyName(this Type type)
        {
            string friendlyName = type.Name;

            var genericArgs = type.GetGenericArguments();

            if (genericArgs.Any())
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = genericArgs;
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName;
        }

        private static bool IsAnIEnumerable(Type type) =>
            type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && !type.IsArray;

        public static Type GetEnumerableElementType(this Type type)
        {
            if (_untypedContainers.Contains(type)) return typeof(object);

            if (type.IsConstructedGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (_typedContainers.Contains(definition))
                {
                    return type.GenericTypeArguments[0];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(type), $"The element type for {type.Name} cannot be coerced effectively");
        }

        private static readonly Type[] _untypedContainers = { typeof(IEnumerable), typeof(IList), typeof(ICollection) };

        private static readonly Type[] _typedContainers = { typeof(IEnumerable<>), typeof(List<>), typeof(IList<>), typeof(ICollection<>), typeof(IReadOnlyCollection<>) };

        /// <summary>
        /// Returns whether or not the given <paramref name="type"/> implements <paramref name="genericType"/>
        /// by testing itself, and then recursively up it's base types hierarchy.
        /// </summary>
        /// <param name="type">Type to test.</param>
        /// <param name="genericType">Type to test for.</param>
        /// <returns>
        ///   <c>true</c> if the indicated type implements <paramref name="genericType"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool ImplementsGenericType(this Type type, Type genericType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
            {
                return true;
            }

            var interfaceTypes = type.GetInterfaces();
            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
            }

            var baseType = type.BaseType;
            return baseType == null ? false : ImplementsGenericType(baseType, genericType);
        }

        public static string Description(this MemberInfo memberInfo) => (memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute)?.Description;

        public static string ObsoleteMessage(this MemberInfo memberInfo) => (memberInfo.GetCustomAttributes(typeof(ObsoleteAttribute), false).FirstOrDefault() as ObsoleteAttribute)?.Message;
    }
}
