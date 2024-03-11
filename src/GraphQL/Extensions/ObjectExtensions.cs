using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Provides extension methods for objects and a method for converting a dictionary into a strongly typed object.
/// </summary>
public static partial class ObjectExtensions
{
    private static readonly ConcurrentDictionary<Type, (ConstructorInfo Constructor, ParameterInfo[] ConstructorParameters)> _types = new();
    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), (MemberInfo MemberInfo, bool IsInitOnly, bool IsRequired)> _members = new();

    /// <summary>
    /// Creates a new instance of the indicated type, populating it with the dictionary.
    /// Can use any constructor of the indicated type, provided that there are keys in the
    /// dictionary that correspond (case insensitive) to the names of the constructor parameters.
    /// </summary>
    /// <param name="source">The source of values.</param>
    /// <param name="type">The type to create.</param>
    /// <param name="mappedType">
    /// GraphType for matching dictionary keys with <paramref name="type"/> property names.
    /// GraphType contains information about this matching in Metadata property.
    /// In case of configuring field as Field("FirstName", x => x.FName) source dictionary
    /// will have 'FirstName' key but its value should be set to 'FName' property of created object.
    /// </param>
    public static object ToObject(
        this IDictionary<string, object?> source,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type,
        IGraphType mappedType)
    {
        var inputGraphType = (mappedType is NonNullGraphType nonNullGraphType
            ? nonNullGraphType.ResolvedType as IInputObjectGraphType
            : mappedType as IInputObjectGraphType)
            ?? throw new InvalidOperationException($"Graph type supplied is not an input object graph type.");

        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // if conversion from IDictionary<string, object> to desired type is registered then use it
        if (ValueConverter.TryConvertTo(source, type, out object? result, typeof(IDictionary<string, object>)))
            return result!;

        var reflectionInfo = GetReflectionInformation(type, inputGraphType);
        return ToObject(source, reflectionInfo);
    }

    /// <summary>
    /// Creates a new instance of the indicated type, populating it with the dictionary.
    /// Uses the constructor and properties specified by the supplied <see cref="ReflectionInfo"/>.
    /// </summary>
    private static object ToObject(this IDictionary<string, object?> source, ReflectionInfo reflectionInfo)
    {
        // build the constructor arguments
        object?[] ctorArguments = reflectionInfo.CtorFields.Length == 0
            ? Array.Empty<object>()
            : new object[reflectionInfo.CtorFields.Length];

        for (int i = 0; i < reflectionInfo.CtorFields.Length; ++i)
        {
            var ctorField = reflectionInfo.CtorFields[i];
            ctorArguments[i] = ctorField.Key != null
                ? GetPropertyValue(source.TryGetValue(ctorField.Key, out var value) ? value : null, ctorField.ParameterInfo.ParameterType, ctorField.GraphType!)
                : ctorField.ParameterInfo.DefaultValue;
        }

        // construct the object
        object obj;
        try
        {
            obj = reflectionInfo.CtorFields.Length == 0
                ? Activator.CreateInstance(reflectionInfo.Type)!
                : reflectionInfo.Constructor.Invoke(ctorArguments);
        }
        catch (TargetInvocationException ex)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
            return ""; // never executed, necessary only for intellisense
        }

        // populate the remaining fields
        foreach (var field in reflectionInfo.MemberFields)
        {
            if (source.TryGetValue(field.Key, out var value))
            {
                if (field.Member is PropertyInfo propertyInfo)
                {
                    var coercedValue = GetPropertyValue(value, propertyInfo.PropertyType, field.GraphType);
                    propertyInfo.SetValue(obj, coercedValue); //issue: this works even if propertyInfo is ValueType and value is null
                }
                else if (field.Member is FieldInfo fieldInfo)
                {
                    var coercedValue = GetPropertyValue(value, fieldInfo.FieldType, field.GraphType);
                    fieldInfo.SetValue(obj, coercedValue);
                }
            }
            else if (field.IsInitOnly || field.IsRequired)
            {
                // initialize all unspecified init-only or required properties
                if (field.Member is PropertyInfo propertyInfo)
                {
                    propertyInfo.SetValue(obj, null);
                }
                else
                {
                    ((FieldInfo)field.Member).SetValue(obj, null);
                }
            }
        }

        return obj;
    }

    private readonly ref struct ReflectionInfo
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public readonly Type Type;
        public readonly ConstructorInfo Constructor;
        public readonly CtorParameterInfo[] CtorFields;
        public readonly MemberFieldInfo[] MemberFields;

        public ReflectionInfo(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
            Type type,
            ConstructorInfo constructor,
            CtorParameterInfo[] ctorFields,
            MemberFieldInfo[] memberFields)
        {
            Type = type;
            Constructor = constructor;
            CtorFields = ctorFields;
            MemberFields = memberFields;
        }

        public readonly struct CtorParameterInfo
        {
            public readonly string? Key;
            public readonly ParameterInfo ParameterInfo;
            public readonly IGraphType? GraphType;

            public CtorParameterInfo(string? key, ParameterInfo parameterInfo, IGraphType? graphType)
            {
                Key = key;
                ParameterInfo = parameterInfo;
                GraphType = graphType;
            }
        }

        public readonly struct MemberFieldInfo
        {
            public readonly string Key;
            public readonly MemberInfo Member;
            public readonly bool IsInitOnly;
            public readonly bool IsRequired;
            public readonly IGraphType GraphType;

            public MemberFieldInfo(string key, MemberInfo member, bool isInitOnly, bool isRequired, IGraphType graphType)
            {
                Key = key;
                Member = member;
                IsInitOnly = isInitOnly;
                IsRequired = isRequired;
                GraphType = graphType;
            }
        }
    }

    /// <summary>
    /// Gets reflection information based on the specified CLR type and graph type.
    /// </summary>
    private static ReflectionInfo GetReflectionInformation(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type clrType,
        IInputObjectGraphType graphType)
    {
        // gather for each field: dictionary key, clr property name, and graph type
        var fields = new (string Key, string? MemberName, IGraphType ResolvedType)[graphType.Fields.Count];
        for (var i = 0; i < graphType.Fields.Count; i++)
        {
            var fieldType = graphType.Fields.List[i];
            // get clr property name (also used for matching on field name or constructor parameter name)
            var fieldName = fieldType.GetMetadata<string>(InputObjectGraphType.ORIGINAL_EXPRESSION_PROPERTY_NAME) ?? fieldType.Name;
            // get graph type
            var resolvedType = fieldType.ResolvedType
                ?? throw new InvalidOperationException($"Field '{fieldType.Name}' of graph type '{graphType.Name}' does not have the ResolvedType property set.");
            // verify no other fields have the same name
            for (var j = 0; j < i; j++)
            {
                if (fields[j].MemberName == fieldName)
                    throw new InvalidOperationException($"Two fields within graph type '{graphType.Name}' were mapped to the same member '{fieldName}'.");
            }
            // add to list
            fields[i] = (fieldType.Name, fieldName, resolvedType);
        }

        // find best constructor to use
        var (bestConstructor, ctorParameters) = _types.GetOrAdd(
            clrType,
            static clrType =>
            {
#pragma warning disable IL2067 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
                var constructor = AutoRegisteringHelper.GetConstructor(clrType);
#pragma warning restore IL2067 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
                var parameters = constructor.GetParameters();
                return (constructor, parameters);
            });

        // pull out parameters that are applicable for that constructor
        var memberCount = fields.Length;
        var ctorFields = ctorParameters.Length > 0
            ? new ReflectionInfo.CtorParameterInfo[ctorParameters.Length]
            : Array.Empty<ReflectionInfo.CtorParameterInfo>();
        for (var i = 0; i < ctorParameters.Length; i++)
        {
            var ctorParam = ctorParameters[i];
            // look for a field that matches the constructor parameter name
            var index = Array.FindIndex<(string, string? MemberName, IGraphType)>(fields, 0, fields.Length, x => string.Equals(x.MemberName, ctorParam.Name, StringComparison.OrdinalIgnoreCase));
            if (index == -1)
            {
                if (ctorParam.IsOptional)
                    ctorFields[i] = new(key: null, ctorParam, graphType: null);
                else
                    throw new InvalidOperationException($"Cannot find field named '{ctorParam.Name}' on graph type '{graphType.Name}' to fulfill constructor parameter for CLR type '{clrType.GetFriendlyName()}'.");
            }
            else
            {
                // add to list, and mark to be removed from fields
                var value = fields[index];
                ctorFields[i] = new(value.Key, ctorParam, value.ResolvedType);
                value.MemberName = null;
                fields[index] = value;
                memberCount--;
            }
        }

        // find other members
        var members = memberCount > 0
            ? new ReflectionInfo.MemberFieldInfo[memberCount]
            : Array.Empty<ReflectionInfo.MemberFieldInfo>();
        var memberIndex = 0;
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            // skip fields handled by constructor
            if (field.MemberName == null)
                continue;
            // look for match on type
#pragma warning disable IL2077 // 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicFields', 'DynamicallyAccessedMemberTypes.PublicProperties' in call to 'FindWritableMember(Type, String)'. The field '(System.Type, System.String).Item1' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
            var (member, initOnly, isRequired) = _members.GetOrAdd((clrType, field.MemberName), static info => info.Type.FindWritableMember(info.PropertyName));
#pragma warning restore IL2077 // 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicFields', 'DynamicallyAccessedMemberTypes.PublicProperties' in call to 'FindWritableMember(Type, String)'. The field '(System.Type, System.String).Item1' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
            members[memberIndex++] = new(field.Key, member, initOnly, isRequired, field.ResolvedType);
        }

        return new ReflectionInfo(clrType, bestConstructor, ctorFields, members);
    }

    /// <summary>
    /// Converts the indicated value into a type that is compatible with fieldType.
    /// </summary>
    /// <param name="propertyValue">The value to be converted.</param>
    /// <param name="fieldType">The desired type.</param>
    /// <param name="mappedType">
    /// GraphType for matching dictionary keys with <paramref name="fieldType"/> property names.
    /// GraphType contains information about this matching in Metadata property.
    /// In case of configuring field as Field("FirstName", x => x.FName) source dictionary
    /// will have 'FirstName' key but its value should be set to 'FName' property of created object.
    /// </param>
    /// <remarks>There is special handling for strings, IEnumerable&lt;T&gt;, Nullable&lt;T&gt;, and Enum.</remarks>
    [RequiresUnreferencedCode("Recursively converts propertyValue to fieldType.")]
    public static object? GetPropertyValue(this object? propertyValue, Type fieldType, IGraphType mappedType)
    {
        if (mappedType == null)
        {
            throw new ArgumentNullException(nameof(mappedType));
        }

        if (mappedType is NonNullGraphType nonNullGraphType)
        {
            mappedType = nonNullGraphType.ResolvedType
                ?? throw new InvalidOperationException("ResolvedType not set for non-null graph type.");
        }

        // Short-circuit conversion if the property value already of the right type
        if (propertyValue == null || fieldType == typeof(object) || fieldType.IsInstanceOfType(propertyValue))
        {
            return propertyValue;
        }

        if (ValueConverter.TryConvertTo(propertyValue, fieldType, out object? result))
            return result;

        if (mappedType is ListGraphType listGraphType)
        {
            var itemGraphType = listGraphType.ResolvedType
                ?? throw new InvalidOperationException("Graph type is not a list graph type or ResolvedType not set.");

            var listConverter = ValueConverter.GetListConverter(fieldType);

            var underlyingType = Nullable.GetUnderlyingType(listConverter.ElementType) ?? listConverter.ElementType;

            // typically, propertyValue is an object[], and no allocations occur
            var objectArray = (propertyValue as IEnumerable
                ?? throw new InvalidOperationException($"Cannot coerce collection of type '{propertyValue.GetType().GetFriendlyName()}' to IEnumerable."))
                .ToObjectArray();

            for (int i = 0; i < objectArray.Length; ++i)
            {
                var listItem = objectArray[i];
                objectArray[i] = listItem == null ? null : GetPropertyValue(listItem, underlyingType, itemGraphType);
            }

            return listConverter.Convert(objectArray);
        }

        var value = propertyValue;

        var nullableFieldType = Nullable.GetUnderlyingType(fieldType);

        // if this is a nullable type and the value is null, return null
        if (nullableFieldType != null && value == null)
        {
            return null;
        }

        if (nullableFieldType != null)
        {
            fieldType = nullableFieldType;
        }

        if (propertyValue is IDictionary<string, object?> objects)
        {
            return ToObject(objects, fieldType, mappedType);
        }

        if (fieldType.IsEnum)
        {
            if (value == null)
            {
                var enumNames = Enum.GetNames(fieldType);
                value = enumNames[0];
            }

            if (!IsDefinedEnumValue(fieldType, value))
            {
                throw new InvalidOperationException($"Unknown value '{value}' for enum '{fieldType.Name}'.");
            }

            string str = value.ToString()!;
            value = Enum.Parse(fieldType, str, true);
        }

        return ValueConverter.ConvertTo(value, fieldType);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the value is <see langword="null"/>, value.ToString equals an empty string, or the value can be converted into a named enum value.
    /// </summary>
    /// <param name="type">An enum type.</param>
    /// <param name="value">The value being tested.</param>
    public static bool IsDefinedEnumValue(Type type, object? value) //TODO: rewrite, comment above seems wrong
    {
        try
        {
            var names = Enum.GetNames(type);
            if (names.Contains(value?.ToString() ?? "", StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            var underlyingType = Enum.GetUnderlyingType(type);
            var converted = Convert.ChangeType(value, underlyingType);

            var values = Enum.GetValues(type);

            foreach (var val in values)
            {
                var convertedVal = Convert.ChangeType(val, underlyingType);
                if (convertedVal.Equals(converted))
                {
                    return true;
                }
            }
        }
        catch
        {
            // TODO: refactor IsDefinedEnumValue
        }

        return false;
    }

    /// <summary>
    /// Returns a string representation of the object, as follows:
    /// <list type="bullet">
    /// <item>Null values are returned as &quot;(null)&quot;</item>
    /// <item>Arrays are returned as &quot;(array)&quot;</item>
    /// <item>Strings are returned unchanged</item>
    /// <item>Intrinsic numbers are formatted and returned</item>
    /// <item>Date and time formats are formatted and returned</item>
    /// <item>Dictionaries and any other types are returned as &quot;(object)&quot;</item>
    /// </list>
    /// </summary>
    internal static string ToSafeString(this object? value) => value switch
    {
        null => "(null)",
        string str => str, // must appear before IEnumerable
        IDictionary => "(object)", // must appear before IEnumerable
        IEnumerable => "(array)",
        bool b => b.ToString(CultureInfo.InvariantCulture),
        char c => c.ToString(CultureInfo.InvariantCulture),
        sbyte sb => sb.ToString(CultureInfo.InvariantCulture),
        byte b => b.ToString(CultureInfo.InvariantCulture),
        short s => s.ToString(CultureInfo.InvariantCulture),
        ushort us => us.ToString(CultureInfo.InvariantCulture),
        int i => i.ToString(CultureInfo.InvariantCulture),
        uint i => i.ToString(CultureInfo.InvariantCulture),
        long l => l.ToString(CultureInfo.InvariantCulture),
        ulong l => l.ToString(CultureInfo.InvariantCulture),
        float f => f.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString(CultureInfo.InvariantCulture),
        decimal d => d.ToString(CultureInfo.InvariantCulture),
        BigInteger bi => bi.ToString(CultureInfo.InvariantCulture),
        Guid g => g.ToString(),
        DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
        TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
#if NET6_0_OR_GREATER
        DateOnly d => d.ToString("o", CultureInfo.InvariantCulture),
        TimeOnly t => t.ToString("o", CultureInfo.InvariantCulture),
#endif
        _ => "(object)"
    };
}
