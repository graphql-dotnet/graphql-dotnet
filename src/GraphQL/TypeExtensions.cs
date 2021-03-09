using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for types.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Determines whether this instance is a concrete type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        ///   <c>true</c> if the specified type is neither abstract nor an interface; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsConcrete(this Type type)
        {
            if (type == null)
                return false;

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
            => type == typeof(string) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        /// <summary>
        /// Determines whether the indicated type implements IGraphType.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the indicated type implements IGraphType; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsGraphType(this Type type)
            => typeof(IGraphType).IsAssignableFrom(type);

        /// <summary>
        /// Gets the GraphQL name of the type. This is derived from the type name and can be overridden by the GraphQLMetadata Attribute.
        /// </summary>
        /// <param name="type">The indicated type.</param>
        /// <returns>A string containing a GraphQL compatible type name.</returns>
        public static string GraphQLName(this Type type)
        {
            type = type.GetNamedType();

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

            return typeName.EndsWith(nameof(Type), StringComparison.InvariantCulture)
                ? typeName.Remove(typeName.Length - nameof(Type).Length)
                : typeName;
        }

        /// <summary>
        /// Gets the graph type for the indicated type.
        /// </summary>
        /// <param name="type">The type for which a graph type is desired.</param>
        /// <param name="isNullable">if set to <c>false</c> if the type explicitly non-nullable.</param>
        /// <param name="mode">Mode to use when mapping CLR type to GraphType.</param>
        /// <returns>A Type object representing a GraphType that matches the indicated type.</returns>
        /// <remarks>This can handle arrays, lists and other collections implementing IEnumerable.</remarks>
        public static Type GetGraphTypeFromType(this Type type, bool isNullable = false, TypeMappingMode mode = TypeMappingMode.UseBuiltInScalarMappings)
        {
            while (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDataLoaderResult<>))
            {
                type = type.GetGenericArguments()[0];
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
                if (isNullable == false)
                {
                    throw new ArgumentOutOfRangeException(nameof(isNullable),
                        $"Explicitly nullable type: Nullable<{type.Name}> cannot be coerced to a non nullable GraphQL type.");
                }
            }

            Type graphType = null;

            if (type.IsArray)
            {
                var clrElementType = type.GetElementType();
                var elementType = GetGraphTypeFromType(clrElementType, clrElementType.IsNullable(), mode); // isNullable from elementType, not from parent array
                graphType = typeof(ListGraphType<>).MakeGenericType(elementType);
            }
            else if (IsAnIEnumerable(type))
            {
                var clrElementType = GetEnumerableElementType(type);
                var elementType = GetGraphTypeFromType(clrElementType, clrElementType.IsNullable(), mode); // isNullable from elementType, not from parent container
                graphType = typeof(ListGraphType<>).MakeGenericType(elementType);
            }
            else
            {
                var attr = type.GetCustomAttribute<GraphQLMetadataAttribute>();
                if (attr != null)
                {
                    if (mode == TypeMappingMode.InputType)
                        graphType = attr.InputType;
                    else if (mode == TypeMappingMode.OutputType)
                        graphType = attr.OutputType;
                    else if (attr.InputType == attr.OutputType) // scalar
                        graphType = attr.InputType;
                }

                if (graphType == null)
                {
                    if (mode == TypeMappingMode.UseBuiltInScalarMappings || !GlobalSwitches.UseRuntimeTypeMappings)
                    {
                        SchemaTypes.BuiltInScalarMappings.TryGetValue(type, out graphType);
                    }
                    else if (!type.IsEnum)
                    {
                        graphType = (mode == TypeMappingMode.OutputType ? typeof(GraphQLClrOutputTypeReference<>) : typeof(GraphQLClrInputTypeReference<>)).MakeGenericType(type);
                    }
                }
            }

            if (graphType == null)
            {
                if (type.IsEnum)
                {
                    graphType = typeof(EnumerationGraphType<>).MakeGenericType(type);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(type), $"The CLR type '{type.FullName}' cannot be coerced effectively to a GraphQL type.");
                }
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

            if (genericArgs.Length > 0)
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
                    friendlyName += i == 0 ? typeParamName : "," + typeParamName;
                }
                friendlyName += ">";
            }

            return friendlyName;
        }

        private static bool IsAnIEnumerable(Type type) =>
            type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && !type.IsArray;

        /// <summary>
        /// Returns the type of element for a one-dimensional container type.
        /// Throws <see cref="ArgumentOutOfRangeException"/> if the type cannot be identified
        /// as a one-dimensional container type.
        /// </summary>
        public static Type GetEnumerableElementType(this Type type)
        {
            if (_untypedContainers.Contains(type))
                return typeof(object);

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
            return baseType != null && ImplementsGenericType(baseType, genericType);
        }

        /// <summary>
        /// Looks for a <see cref="DescriptionAttribute"/> on the specified member and returns
        /// the <see cref="DescriptionAttribute.Description">description</see>, if any. Otherwise
        /// returns XML documentation on the specified member, if any. Note that behavior of this
        /// method depends from <see cref="GlobalSwitches.EnableReadDescriptionFromAttributes"/>
        /// and <see cref="GlobalSwitches.EnableReadDescriptionFromXmlDocumentation"/> settings.
        /// </summary>
        public static string Description(this MemberInfo memberInfo)
        {
            string description = null;

            if (GlobalSwitches.EnableReadDescriptionFromAttributes)
            {
                description = (memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute)?.Description;
                if (description != null)
                    return description;
            }

            if (GlobalSwitches.EnableReadDescriptionFromXmlDocumentation)
            {
                description = memberInfo.GetXmlDocumentation();
            }

            return description;
        }

        /// <summary>
        /// Looks for a <see cref="ObsoleteAttribute"/> on the specified member and returns
        /// the <see cref="ObsoleteAttribute.Message">message</see>, if any. Note that behavior of this
        /// method depends from <see cref="GlobalSwitches.EnableReadDeprecationReasonFromAttributes"/> setting.
        /// </summary>
        public static string ObsoleteMessage(this MemberInfo memberInfo)
        {
            return GlobalSwitches.EnableReadDeprecationReasonFromAttributes
                ? (memberInfo.GetCustomAttributes(typeof(ObsoleteAttribute), false).FirstOrDefault() as ObsoleteAttribute)?.Message
                : null;
        }

        /// <summary>
        /// Looks for a <see cref="DefaultValueAttribute"/> on the specified member and returns
        /// the <see cref="DefaultValueAttribute.Value">value</see>, if any. Note that behavior of this
        /// method depends from <see cref="GlobalSwitches.EnableReadDefaultValueFromAttributes"/> setting.
        /// </summary>
        public static object DefaultValue(this MemberInfo memberInfo)
        {
            return GlobalSwitches.EnableReadDefaultValueFromAttributes
                ? (memberInfo.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute)?.Value
                : null;
        }
    }

    /// <summary>
    /// Mode used when mapping CLR type to GraphType in <see cref="TypeExtensions.GetGraphTypeFromType"/>.
    /// </summary>
    public enum TypeMappingMode
    {
        /// <summary>
        /// This mode is left for backward compatibility in cases where you call <see cref="TypeExtensions.GetGraphTypeFromType"/> directly.
        /// </summary>
        UseBuiltInScalarMappings,

        /// <summary>
        /// Map CLR type as input type.
        /// </summary>
        InputType,

        /// <summary>
        /// Map CLR type as output type.
        /// </summary>
        OutputType,
    }
}
