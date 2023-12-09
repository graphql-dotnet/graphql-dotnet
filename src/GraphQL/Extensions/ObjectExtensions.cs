using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for objects and a method for converting a dictionary into a strongly typed object.
    /// </summary>
    public static class ObjectExtensions
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
                obj = reflectionInfo.Constructor.Invoke(ctorArguments);
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

        private struct ReflectionInfo
        {
            public ConstructorInfo Constructor;
            public (string? Key, ParameterInfo ParameterInfo, IGraphType? GraphType)[] CtorFields;
            public (string Key, MemberInfo Member, bool IsInitOnly, bool IsRequired, IGraphType GraphType)[] MemberFields;
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

            // find best constructor to use, with preference to the constructor with the most parameters
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
                ? new (string? Key, ParameterInfo Parameter, IGraphType? GraphType)[ctorParameters.Length]
                : Array.Empty<(string? Key, ParameterInfo Parameter, IGraphType? GraphType)>();
            for (var i = 0; i < ctorParameters.Length; i++)
            {
                var ctorParam = ctorParameters[i];
                // look for a field that matches the constructor parameter name
                var index = Array.FindIndex<(string, string? MemberName, IGraphType)>(fields, 0, fields.Length, x => string.Equals(x.MemberName, ctorParam.Name, StringComparison.OrdinalIgnoreCase));
                if (index == -1)
                {
                    if (ctorParam.IsOptional)
                        ctorFields[i] = (null, ctorParam, null);
                    else
                        throw new InvalidOperationException($"Cannot find field named '{ctorParam.Name}' on graph type '{graphType.Name}' to fulfill constructor parameter for type '{clrType.GetFriendlyName()}'.");
                }
                else
                {
                    // add to list, and mark to be removed from fields
                    var value = fields[index];
                    ctorFields[i] = (value.Key, ctorParam, value.ResolvedType);
                    value.MemberName = null;
                    fields[index] = value;
                    memberCount--;
                }
            }

            // find other members
            var members = memberCount > 0
                ? new (string Key, MemberInfo Member, bool IsInitOnly, bool IsRequired, IGraphType ResolvedType)[memberCount]
                : Array.Empty<(string Key, MemberInfo Member, bool IsInitOnly, bool IsRequired, IGraphType ResolvedType)>();
            var memberIndex = 0;
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                // skip fields handled by constructor
                if (field.MemberName == null)
                    continue;
                // look for match on type
#pragma warning disable IL2077 // 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicFields', 'DynamicallyAccessedMemberTypes.PublicProperties' in call to 'FindMatchingMember(Type, String)'. The field '(System.Type, System.String).Item1' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
                var (member, initOnly, isRequired) = _members.GetOrAdd((clrType, field.MemberName), static info => FindMatchingMember(info.Type, info.PropertyName));
#pragma warning restore IL2077 // 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicFields', 'DynamicallyAccessedMemberTypes.PublicProperties' in call to 'FindMatchingMember(Type, String)'. The field '(System.Type, System.String).Item1' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
                members[memberIndex++] = (field.Key, member, initOnly, isRequired, field.ResolvedType);
            }

            return new ReflectionInfo
            {
                Constructor = bestConstructor,
                CtorFields = ctorFields,
                MemberFields = members,
            };

            static (MemberInfo MemberInfo, bool IsInitOnly, bool IsRequired) FindMatchingMember(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
                Type type,
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
                    var isRequired = fieldInfo.CustomAttributes.Any(x => x.AttributeType.FullName == typeof(RequiredMemberAttribute).FullName);

                    return (fieldInfo, false, isRequired);
                }

                throw new InvalidOperationException($"Cannot find member named '{propertyName}' on CLR type '{type.GetFriendlyName()}'.");
            }
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

            var enumerableInterface = fieldType.Name == "IEnumerable`1"
              ? fieldType
              : fieldType.GetInterface("IEnumerable`1");

            if (mappedType is ListGraphType listGraphType && fieldType != typeof(string) && enumerableInterface != null)
            {
                var itemGraphType = listGraphType.ResolvedType
                    ?? throw new InvalidOperationException("Graph type is not a list graph type or ResolvedType not set.");
                IList newCollection;
                var elementType = enumerableInterface.GetGenericArguments()[0];
                var underlyingType = Nullable.GetUnderlyingType(elementType) ?? elementType;
                var fieldTypeImplementsIList = fieldType.GetInterface("IList") != null;

                var propertyValueAsIList = propertyValue as IList;

                // Custom container
                if (fieldTypeImplementsIList && !fieldType.IsArray)
                {
                    newCollection = (IList)Activator.CreateInstance(fieldType)!;
                }
                // Array of known size is created immediately
                else if (fieldType.IsArray && propertyValueAsIList != null)
                {
                    newCollection = Array.CreateInstance(elementType, propertyValueAsIList.Count);
                }
                // List<T>
                else
                {
                    var genericListType = typeof(List<>).MakeGenericType(elementType);
                    newCollection = (IList)Activator.CreateInstance(genericListType)!;
                }

                if (propertyValue is not IEnumerable valueList)
                    return newCollection;

                // Array of known size is populated in-place
                if (fieldType.IsArray && propertyValueAsIList != null)
                {
                    for (int i = 0; i < propertyValueAsIList.Count; ++i)
                    {
                        var listItem = propertyValueAsIList[i];
                        newCollection[i] = listItem == null ? null : GetPropertyValue(listItem, underlyingType, itemGraphType);
                    }
                }
                // Array of unknown size is created only after populating list
                else
                {
                    foreach (var listItem in valueList)
                    {
                        newCollection.Add(listItem == null ? null : GetPropertyValue(listItem, underlyingType, itemGraphType));
                    }

                    if (fieldType.IsArray)
                        newCollection = ((dynamic)newCollection!).ToArray();
                }

                return newCollection;
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
    }
}
