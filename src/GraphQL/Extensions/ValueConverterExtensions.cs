using System.Collections;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Provides extension methods for <see cref="IValueConverter"/> to simplify type conversion operations.
/// </summary>
public static class ValueConverterExtensions
{
    /// <summary>
    /// <para>Returns an object of the specified type and whose value is equivalent to the specified object.</para>
    /// <para>Throws a <see cref="InvalidOperationException"/> if there is no conversion registered; conversion functions may throw other exceptions</para>
    /// </summary>
    public static T? ConvertTo<T>(this IValueConverter valueConverter, object? value)
    {
        var ret = valueConverter.ConvertTo(value, typeof(T));
        if (ret is null)
            return default;
        return (T)ret;
    }

    /// <summary>
    /// <para>Returns an object of the specified type and whose value is equivalent to the specified object.</para>
    /// <para>Throws a <see cref="InvalidOperationException"/> if there is no conversion registered; conversion functions may throw other exceptions</para>
    /// </summary>
    public static object? ConvertTo(this IValueConverter valueConverter, object? value, Type targetType)
    {
        if (value == null)
            return null;

        if (!valueConverter.TryConvertTo(value, targetType, out object? result))
            throw new InvalidOperationException($"Could not find conversion from '{value.GetType().FullName}' to '{targetType.FullName}'");

        return result;
    }

    /// <summary>
    /// <para>
    /// If a conversion delegate was registered, converts an object to the specified type and
    /// returns <see langword="true"/>; returns <see langword="false"/> if no conversion delegate is registered.
    /// </para>
    /// <para>Conversion delegates may throw exceptions if the conversion was unsuccessful</para>
    /// </summary>
    public static bool TryConvertTo(this IValueConverter valueConverter, object? value, Type targetType, out object? result, Type? sourceType = null)
    {
        if (value == null || targetType.IsInstanceOfType(value))
        {
            result = value;
            return true;
        }

        sourceType ??= value.GetType();
        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var conversion = valueConverter.GetConversion(sourceType, targetType);
        if (conversion == null)
        {
            result = null;
            return false;
        }
        else
        {
            result = conversion(value);
            return true;
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
    /// <param name="valueConverter">The value converter instance to use for type conversions.</param>
    /// <remarks>There is special handling for strings, IEnumerable&lt;T&gt;, Nullable&lt;T&gt;, and Enum.</remarks>
    public static object? GetPropertyValue(this IValueConverter valueConverter, object? propertyValue, Type fieldType, IGraphType mappedType)
    {
        if (mappedType == null)
            throw new ArgumentNullException(nameof(mappedType));

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

        if (valueConverter.TryConvertTo(propertyValue, fieldType, out object? result))
            return result;

        if (mappedType is ListGraphType listGraphType)
        {
            var itemGraphType = listGraphType.ResolvedType
                ?? throw new InvalidOperationException("Graph type is not a list graph type or ResolvedType not set.");

            var listConverter = valueConverter.GetListConverter(fieldType);

            var underlyingType = Nullable.GetUnderlyingType(listConverter.ElementType) ?? listConverter.ElementType;

            // typically, propertyValue is an object[], and no allocations occur
            var objectArray = (propertyValue as IEnumerable
                ?? throw new InvalidOperationException($"Cannot coerce collection of type '{propertyValue.GetType().GetFriendlyName()}' to IEnumerable."))
                .ToObjectArray();

            for (int i = 0; i < objectArray.Length; ++i)
            {
                var listItem = objectArray[i];
                objectArray[i] = listItem == null ? null : valueConverter.GetPropertyValue(listItem, underlyingType, itemGraphType);
            }

            return listConverter.Convert(objectArray);
        }

        fieldType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;

        if (mappedType is IInputObjectGraphType inputObjectGraphType)
        {
            if (propertyValue is not IDictionary<string, object?> dictionary)
                throw new InvalidOperationException($"Cannot coerce map of type '{propertyValue.GetType().GetFriendlyName()}' to IDictionary<string, object?>.");

            return valueConverter.ToObject(dictionary, fieldType, inputObjectGraphType);
        }

        if (fieldType.IsEnum)
        {
#if NETSTANDARD2_0
            try
            {
                return Enum.Parse(fieldType, propertyValue?.ToString() ?? "", true);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException($"Unknown value '{propertyValue.ToSafeString()}' for enum '{fieldType.Name}'.");
            }
#else
            if (Enum.TryParse(fieldType, propertyValue?.ToString() ?? "", true, out object? enumResult))
            {
                return enumResult;
            }
            else
            {
                throw new InvalidOperationException($"Unknown value '{propertyValue.ToSafeString()}' for enum '{fieldType.Name}'.");
            }
#endif
        }

        // this always throws, since TryConvertTo already failed
        return valueConverter.ConvertTo(propertyValue, fieldType);
    }

    /// <inheritdoc cref="ValueConverterBase.Register{TSource, TTarget}(Func{TSource, TTarget}?)"/>
    public static void Register<TObjectType>(this ValueConverterBase valueConverter, Func<IDictionary<string, object?>, TObjectType>? converter)
        => valueConverter.Register<IDictionary<string, object?>, TObjectType>(converter);
}
