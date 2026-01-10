using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL;

/// <summary>
/// Provides extension methods for types.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Returns the element type of the specified list type.
    /// For arrays, this is the element type of the array.
    /// For generic types, this is the type of generic argument.
    /// Otherwise, this is <see cref="object"/>.
    /// </summary>
    internal static Type GetListElementType(this Type listType)
    {
        if (listType is null)
            throw new ArgumentNullException(nameof(listType));
        if (listType.IsGenericTypeDefinition)
            throw new InvalidOperationException($"Type '{listType.GetFriendlyName()}' is a generic type definition and the element type cannot be determined.");
        if (listType.IsGenericType)
        {
            var genericArguments = listType.GetGenericArguments();
            if (genericArguments.Length != 1)
                throw new InvalidOperationException($"Type '{listType.GetFriendlyName()}' is a generic type with {genericArguments.Length} generic arguments so the element type cannot be determined.");
            return genericArguments[0];
        }
        return listType.IsArray
            ? listType.GetElementType()!
            : typeof(object);
    }

    /// <summary>
    /// Converts the specified <see cref="IEnumerable"/> to an <see cref="Array"/> of type <see cref="object"/>.
    /// </summary>
    /// <remarks>
    /// Optimized over <paramref name="values"/><see cref="Enumerable.Cast{TResult}(IEnumerable)">.Cast&lt;object&gt;()</see><see cref="Enumerable.ToArray{TSource}(IEnumerable{TSource})">.ToArray()</see>.
    /// </remarks>
    internal static object?[] ToObjectArray(this IEnumerable values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (values is object?[] objectArray)
            return objectArray;
        if (values is List<object?> objectList)
            return objectList.ToArray();
        if (values is IEnumerable<object?> enumerable)
            return enumerable.ToArray();
        if (values is ICollection collection) // note: TryGetNonEnumeratedCount is not available for IEnumerable; only IEnumerable<T>
        {
            var count = collection.Count;
            object?[] array = new object?[count];
            int i = 0;
            foreach (var value in values)
                array[i++] = value;
            if (i != count)
                throw new InvalidOperationException("The number of items in the collection changed during enumeration.");
            return array;
        }
        var list = new List<object?>();
        foreach (object? value in values)
            list.Add(value);
        return list.ToArray();
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
    /// Gets the GraphQL name of the type. This is derived from the type name and can
    /// be overridden by the <see cref="GraphQLMetadataAttribute"/>.
    /// </summary>
    /// <param name="type">The indicated type.</param>
    /// <returns>A string containing a GraphQL compatible type name.</returns>
    public static string GraphQLName(this Type type)
    {
        return NameOf(type)
            .Replace('@', '_'); // F# anonymous class support

        static string NameOf(Type type)
        {
            type = type.GetNamedType();

            if (!typeof(IGraphType).IsAssignableFrom(type))
            {
                var attr = type.GetCustomAttribute<GraphQLMetadataAttribute>();
                if (!string.IsNullOrEmpty(attr?.Name))
                {
                    return attr!.Name!;
                }
            }

            var name = type.Name;

            if (type.IsGenericType)
            {
                var i = name.IndexOf('`');
                if (i >= 0)
                    name = name.Substring(0, name.IndexOf('`'));
            }

            if (name != "GraphType" && name != "Type")
            {
                if (name.EndsWith("GraphType", StringComparison.Ordinal))
                {
                    name = name.Substring(0, name.Length - "GraphType".Length);
                }
                else if (name.EndsWith("Type", StringComparison.Ordinal))
                {
                    name = name.Substring(0, name.Length - "Type".Length);
                }
            }

            if (GlobalSwitches.UseDeclaringTypeNames)
            {
                var parent = type.DeclaringType;
                while (parent != null)
                {
                    var parentName = parent.Name;
                    var i = parentName.IndexOf('`');
                    if (i >= 0)
                        parentName = parentName.Substring(0, parentName.IndexOf('`'));
                    name = $"{parentName}_{name}";
                    parent = parent.DeclaringType;
                }
            }

            if (!type.IsGenericType)
                return name;
            var sb = new StringBuilder();
            foreach (var arg in type.GetGenericArguments())
            {
                sb.Append(NameOf(arg));
            }
            sb.Append(name);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Converts a CLR type to its corresponding GraphQL type representation.
    /// </summary>
    /// <param name="type">The CLR type to convert (e.g., <see cref="int"/>, <see cref="string"/>, custom classes, enums, or collections).</param>
    /// <param name="isNullable">
    /// Indicates whether the GraphQL type should allow null values. When <see langword="false"/>, the returned type is wrapped in <see cref="NonNullGraphType{T}"/>.
    /// </param>
    /// <param name="mode">The mapping mode: <see cref="TypeMappingMode.InputType"/>, <see cref="TypeMappingMode.OutputType"/>, or <see cref="TypeMappingMode.UseBuiltInScalarMappings"/> (legacy).</param>
    /// <returns>
    /// A GraphQL type such as <see cref="EnumerationGraphType{T}"/>, <see cref="ListGraphType{T}"/>, or <see cref="NonNullGraphType{T}"/>.
    /// </returns>
    /// <remarks>
    /// Handles arrays, collections, nullable value types, and <see cref="IDataLoaderResult{T}"/>.
    /// Respects <see cref="InputTypeAttribute"/> and <see cref="OutputTypeAttribute"/> for custom type mappings.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="type"/> is already a GraphQL type, a <see cref="Task"/>, or cannot be mapped.
    /// </remarks>
    [Obsolete("Use TypeExtensions.GetGraphTypeFromType(Type, bool, bool) overload instead. This method will be removed in v10.")]
    [RequiresDynamicCode("This method uses reflection to create types at runtime which is not compatible with trimming and AOT.")]
    public static Type GetGraphTypeFromType(this Type type, bool isNullable, TypeMappingMode mode)
    {
        if (mode == TypeMappingMode.InputType || mode == TypeMappingMode.OutputType)
            return GetGraphTypeFromType(type, isNullable, mode == TypeMappingMode.InputType);

        // legacy path for UseBuiltInScalarMappings
        if (typeof(IGraphType).IsAssignableFrom(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), $"The graph type '{type.GetFriendlyName()}' cannot be used as a CLR type.");
        }

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
            graphType = MakeListType(elementType);
        }
        else if (TryGetEnumerableElementType(type, out var clrElementType))
        {
            var elementType = GetGraphTypeFromType(clrElementType, IsNullableType(clrElementType), mode); // isNullable from elementType, not from parent container
            graphType = MakeListType(elementType);
        }
        else
        {
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
                if (!SchemaTypesBase.BuiltInScalarMappings.TryGetValue(type, out graphType))
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
        }

        if (!isNullable)
        {
            graphType = MakeNonNullType(graphType);
        }

        return graphType;

        static bool IsNullableType(Type type) => !type.IsValueType || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    /// <summary>
    /// Converts a CLR type to its corresponding GraphQL type representation.
    /// </summary>
    /// <param name="type">The CLR type to convert (e.g., <see cref="int"/>, <see cref="string"/>, custom classes, enums, or collections).</param>
    /// <param name="isNullable">
    /// Indicates whether the GraphQL type should allow null values. When <see langword="false"/>, the returned type is wrapped in <see cref="NonNullGraphType{T}"/>.
    /// </param>
    /// <param name="isInputType">
    /// <see langword="true"/> for input types (arguments, input objects); <see langword="false"/> for output types (fields, query results).
    /// </param>
    /// <returns>
    /// A GraphQL type such as <see cref="GraphQLClrOutputTypeReference{T}"/>, <see cref="GraphQLClrInputTypeReference{T}"/>,
    /// <see cref="ListGraphType{T}"/>, or <see cref="NonNullGraphType{T}"/>.
    /// </returns>
    /// <remarks>
    /// Handles arrays, collections, nullable value types, and <see cref="IDataLoaderResult{T}"/>.
    /// Respects <see cref="InputTypeAttribute"/> and <see cref="OutputTypeAttribute"/> for custom type mappings.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="type"/> is already a GraphQL type or a <see cref="Task"/>.
    /// </remarks>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public static Type GetGraphTypeFromType(this Type type, bool isNullable, bool isInputType)
    {
        if (typeof(IGraphType).IsAssignableFrom(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), $"The graph type '{type.GetFriendlyName()}' cannot be used as a CLR type.");
        }

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
            var elementType = GetGraphTypeFromType(clrElementType, IsNullableType(clrElementType), isInputType); // isNullable from elementType, not from parent array
            graphType = MakeListType(elementType);
        }
        else if (TryGetEnumerableElementType(type, out var clrElementType))
        {
            var elementType = GetGraphTypeFromType(clrElementType, IsNullableType(clrElementType), isInputType); // isNullable from elementType, not from parent container
            graphType = MakeListType(elementType);
        }
        else
        {
            if (isInputType)
            {
                var inputAttr = type.GetCustomAttribute<InputTypeAttribute>();
                if (inputAttr != null)
                    graphType = inputAttr.InputType;
            }
            else
            {
                var outputAttr = type.GetCustomAttribute<OutputTypeAttribute>();
                if (outputAttr != null)
                    graphType = outputAttr.OutputType;
            }

            if (graphType == null)
            {
                graphType = MakeClrTypeReference(type, isInputType);
            }
        }

        if (!isNullable)
        {
            graphType = MakeNonNullType(graphType);
        }

        return graphType;

        static bool IsNullableType(Type type) => !type.IsValueType || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    // typeof(NonNullGraphType<>).MakeGenericType(type) will always succeed with .NET 10, but it will always
    // be missing native code; which doesn't matter because these types are never instantiated
    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.",
        Justification = "The supplied type is expected to be a reference type. NonNullGraphType<T> constrains T to IGraphType, and all supported implementations are classes, so MakeGenericType(type) should be valid.")]
    [UnconditionalSuppressMessage("Trimming", "IL2071:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.",
        Justification = "The supplied type is expected to be a reference type. NonNullGraphType<T> constrains T to IGraphType, and all supported implementations are classes, so MakeGenericType(type) should be valid.")]
    [UnconditionalSuppressMessage("Trimming", "IL2073: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to",
        Justification = "The return type is a marker type and will never have constructors.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050:Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT",
        Justification = "T in NonNullGraphType<T> is constrained to IGraphType (a reference type), so MakeGenericType always works.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    internal static Type MakeNonNullType(this Type type)
        => typeof(NonNullGraphType<>).MakeGenericType(type);

    // typeof(ListGraphType<>).MakeGenericType(type) will always succeed with .NET 10, but it will always
    // be missing native code; which doesn't matter because these types are never instantiated
    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.",
        Justification = "The supplied type is expected to be a reference type. ListGraphType<T> constrains T to IGraphType, and all supported implementations are classes, so MakeGenericType(type) should be valid.")]
    [UnconditionalSuppressMessage("Trimming", "IL2071:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.",
        Justification = "The supplied type is expected to be a reference type. ListGraphType<T> constrains T to IGraphType, and all supported implementations are classes, so MakeGenericType(type) should be valid.")]
    [UnconditionalSuppressMessage("Trimming", "IL2073: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to",
        Justification = "The return type is a marker type and will never have constructors.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050:Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT",
        Justification = "T in ListGraphType<T> is constrained to IGraphType (a reference type), so MakeGenericType always works.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    internal static Type MakeListType(this Type type)
        => typeof(ListGraphType<>).MakeGenericType(type);

    // typeof(GraphQLClrOutputTypeReference<>).MakeGenericType(type) will always succeed with .NET 10, but it will always
    // be missing native code; which doesn't matter because these types are never instantiated
    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.",
        Justification = "MakeGenericType always works in .NET 10 even if it does not generate any code, which doesn't matter because the type is never instantiated.")]
    [UnconditionalSuppressMessage("Trimming", "IL2071:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.",
        Justification = "MakeGenericType always works in .NET 10 even if it does not generate any code, which doesn't matter because the type is never instantiated.")]
    [UnconditionalSuppressMessage("Trimming", "IL2073: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements. The return value of method 'source method' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to",
        Justification = "The return type is a marker type and will never have constructors.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050:Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT",
        Justification = "MakeGenericType always works in .NET 10 even if it does not generate any code, which doesn't matter because the type is never instantiated.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    internal static Type MakeClrTypeReference(this Type clrType, bool isInputType)
        => (isInputType ? typeof(GraphQLClrInputTypeReference<>) : typeof(GraphQLClrOutputTypeReference<>)).MakeGenericType(clrType);

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

    /// <summary>
    /// Identifies a property or field on the specified type that matches the specified name.
    /// Search is performed case-insensitively. The property or field must be public and writable/settable.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    internal static (MemberInfo MemberInfo, bool IsInitOnly, bool IsRequired) FindWritableMember(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
        this Type type,
        string propertyName)
    {
        PropertyInfo? propertyInfo = null;

        // note: analzyer raises false IL2070 warning due to BindingFlags.IgnoreCase being present

        try
        {
#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
            propertyInfo = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        }
        catch (AmbiguousMatchException)
        {
#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
            propertyInfo = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        }

        if (propertyInfo?.SetMethod?.IsPublic ?? false)
        {
            var isExternalInit = propertyInfo.SetMethod.ReturnParameter.GetRequiredCustomModifiers()
                .Any(type => type.FullName == typeof(IsExternalInit).FullName);

            var isRequired = propertyInfo.CustomAttributes.Any(x => x.AttributeType.FullName == typeof(RequiredMemberAttribute).FullName);

            return (propertyInfo, isExternalInit, isRequired);
        }

        FieldInfo? fieldInfo;

        try
        {
#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
            fieldInfo = type.GetField(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        }
        catch (AmbiguousMatchException)
        {
#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
            fieldInfo = type.GetField(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        }

        if (fieldInfo != null)
        {
            if (fieldInfo.IsInitOnly)
                throw new InvalidOperationException($"Field named '{propertyName}' on CLR type '{type.GetFriendlyName()}' is defined as a read-only field.");

            var isRequired = fieldInfo.CustomAttributes.Any(x => x.AttributeType.FullName == typeof(RequiredMemberAttribute).FullName);

            return (fieldInfo, false, isRequired);
        }

        throw new InvalidOperationException($"Cannot find member named '{propertyName}' on CLR type '{type.GetFriendlyName()}'.");
    }
}

/// <summary>
/// Mode used when mapping CLR type to GraphType in <see cref="TypeExtensions.GetGraphTypeFromType(Type, bool, TypeMappingMode)"/>.
/// </summary>
[Obsolete("Use TypeExtensions.GetGraphTypeFromType(Type, bool, bool) overload instead. This enum will be removed in v10.")]
public enum TypeMappingMode
{
    /// <summary>
    /// This mode is left for backward compatibility in cases where you call <see cref="TypeExtensions.GetGraphTypeFromType(Type, bool, TypeMappingMode)"/> directly.
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
