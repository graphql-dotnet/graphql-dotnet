using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
        ///   <see langword="true"/> if the specified type is neither abstract nor an interface; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsConcrete(this Type type)
        {
            if (type == null)
                return false;

            return !type.IsAbstract && !type.IsInterface; // && !type.IsGenericTypeDefinition; ??
        }

        /// <summary>
        /// Determines whether the indicated type implements IGraphType.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <see langword="true"/> if the indicated type implements IGraphType; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsGraphType(this Type type)
            => typeof(IGraphType).IsAssignableFrom(type);

        /// <summary>
        /// Determines if the specified type represents a named graph type (not a wrapper type such as <see cref="ListGraphType"/>).
        /// </summary>
        internal static bool IsNamedType(this Type type)
        {
            if (!IsGraphType(type))
                return false;
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(NonNullGraphType<>) ||
                    genericType == typeof(ListGraphType<>))
                {
                    return false;
                }
            }
            else if (type == typeof(NonNullGraphType) || type == typeof(ListGraphType))
            {
                return false;
            }

            return true;
        }

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
                return attr!.Name!;
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
        /// <param name="isNullable">if set to <see langword="false"/> if the type explicitly non-nullable.</param>
        /// <param name="mode">Mode to use when mapping CLR type to GraphType.</param>
        /// <returns>A Type object representing a GraphType that matches the indicated type.</returns>
        /// <remarks>This can handle arrays, lists and other collections implementing IEnumerable.</remarks>
        public static Type GetGraphTypeFromType(this Type type, bool isNullable = false, TypeMappingMode mode = TypeMappingMode.UseBuiltInScalarMappings)
        {
            while (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDataLoaderResult<>))
            {
                type = type.GetGenericArguments()[0];
            }

            if (type == typeof(IDataLoaderResult))
            {
                type = typeof(object);
            }

            if (typeof(Task).IsAssignableFrom(type))
                throw new ArgumentOutOfRangeException(nameof(type), "Task types cannot be coerced to a graph type; please unwrap the task type before calling this method.");

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
                if (!isNullable)
                {
                    throw new ArgumentOutOfRangeException(nameof(isNullable),
                        $"Explicitly nullable type: Nullable<{type.Name}> cannot be coerced to a non nullable GraphQL type.");
                }
            }

            Type? graphType = null;

            if (type.IsArray)
            {
                var clrElementType = type.GetElementType()!;
                var elementType = GetGraphTypeFromType(clrElementType, IsNullableType(clrElementType), mode); // isNullable from elementType, not from parent array
                graphType = typeof(ListGraphType<>).MakeGenericType(elementType);
            }
            else if (TryGetEnumerableElementType(type, out var clrElementType))
            {
                var elementType = GetGraphTypeFromType(clrElementType, IsNullableType(clrElementType), mode); // isNullable from elementType, not from parent container
                graphType = typeof(ListGraphType<>).MakeGenericType(elementType);
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
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
#pragma warning restore CS0618 // Type or member is obsolete

                if (mode == TypeMappingMode.InputType)
                {
                    var inputAttr = type.GetCustomAttribute<InputTypeAttribute>();
                    if (inputAttr != null)
                        graphType = inputAttr.InputType;
                }
                else if (mode == TypeMappingMode.OutputType)
                {
                    var outputAttr = type.GetCustomAttribute<OutputTypeAttribute>();
                    if (outputAttr != null)
                        graphType = outputAttr.OutputType;
                }

                if (graphType == null)
                {
                    if (mode == TypeMappingMode.UseBuiltInScalarMappings)
                    {
                        if (!SchemaTypes.BuiltInScalarMappings.TryGetValue(type, out graphType))
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
                    }
                    else
                    {
                        graphType = (mode == TypeMappingMode.OutputType ? typeof(GraphQLClrOutputTypeReference<>) : typeof(GraphQLClrInputTypeReference<>)).MakeGenericType(type);
                    }
                }
            }

            if (!isNullable)
            {
                graphType = typeof(NonNullGraphType<>).MakeGenericType(graphType);
            }

            return graphType;

            //TODO: rewrite nullability condition in v5
            static bool IsNullableType(Type type) => !type.IsValueType || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
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

        /// <summary>
        /// Returns the type of element for a one-dimensional container type.
        /// </summary>
        private static bool TryGetEnumerableElementType(Type type, [NotNullWhen(true)] out Type? elementType)
        {
            if (type == typeof(IEnumerable))
            {
                elementType = typeof(object);
                return true;
            }

            if (!type.IsGenericType || !TypeInformation.EnumerableListTypes.Contains(type.GetGenericTypeDefinition()))
            {
                elementType = null;
                return false;
            }

            elementType = type.GetGenericArguments()[0];
            return true;
        }

        /// <summary>
        /// Returns whether or not the given <paramref name="type"/> implements <paramref name="genericType"/>
        /// by testing itself, and then recursively up it's base types hierarchy.
        /// </summary>
        /// <param name="type">Type to test.</param>
        /// <param name="genericType">Type to test for.</param>
        /// <returns>
        ///   <see langword="true"/> if the indicated type implements <paramref name="genericType"/>; otherwise, <see langword="false"/>.
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
        public static string? Description(this MemberInfo memberInfo)
        {
            string? description = null;

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
        /// Looks for a <see cref="DescriptionAttribute"/> on the specified parameter and returns
        /// the <see cref="DescriptionAttribute.Description">description</see>, if any. Otherwise
        /// returns XML documentation on the specified member, if any. Note that behavior of this
        /// method depends from <see cref="GlobalSwitches.EnableReadDescriptionFromAttributes"/>
        /// and <see cref="GlobalSwitches.EnableReadDescriptionFromXmlDocumentation"/> settings.
        /// </summary>
        public static string? Description(this ParameterInfo parameterInfo)
        {
            string? description = null;

            if (GlobalSwitches.EnableReadDescriptionFromAttributes)
            {
                description = (parameterInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute)?.Description;
                if (description != null)
                    return description;
            }

            if (GlobalSwitches.EnableReadDescriptionFromXmlDocumentation)
            {
                description = parameterInfo.GetXmlDocumentation();
            }

            return description;
        }

        /// <summary>
        /// Looks for a <see cref="ObsoleteAttribute"/> on the specified member and returns
        /// the <see cref="ObsoleteAttribute.Message">message</see>, if any. Note that behavior of this
        /// method depends from <see cref="GlobalSwitches.EnableReadDeprecationReasonFromAttributes"/> setting.
        /// </summary>
        public static string? ObsoleteMessage(this MemberInfo memberInfo)
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
        public static object? DefaultValue(this MemberInfo memberInfo)
        {
            return GlobalSwitches.EnableReadDefaultValueFromAttributes
                ? (memberInfo.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute)?.Value
                : null;
        }

        /// <summary>
        /// Returns the set of <see cref="GraphQLAttribute"/>s applied to the specified member or its
        /// owning module or assembly, or are listed within <see cref="GlobalSwitches.GlobalAttributes"/>.
        /// Attributes are sorted by <see cref="GraphQLAttribute.Priority"/>, lowest first.
        /// </summary>
        public static IEnumerable<GraphQLAttribute> GetGraphQLAttributes(this MemberInfo memberInfo)
        {
            var module = memberInfo.Module;
            var assembly = module.Assembly;
            return memberInfo.GetCustomAttributes<GraphQLAttribute>()
                .Concat(module.GetCustomAttributes<GraphQLAttribute>())
                .Concat(assembly.GetCustomAttributes<GraphQLAttribute>())
                .Concat(GlobalSwitches.GlobalAttributes)
                .OrderBy(x => x.Priority);
        }

        /// <summary>
        /// Returns the set of <see cref="GraphQLAttribute"/>s applied to the specified parameter or its
        /// owning module or assembly, or are listed within <see cref="GlobalSwitches.GlobalAttributes"/>.
        /// Attributes are sorted by <see cref="GraphQLAttribute.Priority"/>, lowest first.
        /// </summary>
        public static IEnumerable<GraphQLAttribute> GetGraphQLAttributes(this ParameterInfo parameterInfo)
        {
            var module = parameterInfo.Member.Module;
            var assembly = module.Assembly;
            return parameterInfo.GetCustomAttributes<GraphQLAttribute>()
                .Concat(module.GetCustomAttributes<GraphQLAttribute>())
                .Concat(assembly.GetCustomAttributes<GraphQLAttribute>())
                .Concat(GlobalSwitches.GlobalAttributes)
                .OrderBy(x => x.Priority);
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
