using System.Globalization;
using Newtonsoft.Json;

namespace GraphQL.NewtonsoftJson;

// https://github.com/JamesNK/Newtonsoft.Json/issues/1726
// https://stackoverflow.com/questions/21153381/json-net-serializing-float-double-with-minimal-decimal-places-i-e-no-redundant
/// <summary>
/// JSON converter for writing floating-point values without loss of precision.
/// </summary>
public class FixPrecisionConverter : JsonConverter
{
    private readonly bool _decimal;
    private readonly bool _double;
    private readonly bool _float;

    /// <summary>
    /// Initializes the converter and enables it for the specified floating-point types.
    /// </summary>
    public FixPrecisionConverter(bool forDecimal, bool forDouble, bool forFloat)
    {
        _decimal = forDecimal;
        _double = forDouble;
        _float = forFloat;
    }

    /// <inheritdoc/>
    public override bool CanRead => false;

    /// <inheritdoc/>
    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override bool CanConvert(Type objType) =>
        objType == typeof(decimal) && _decimal ||
        objType == typeof(float) && _float ||
        objType == typeof(double) && _double;

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter jWriter, object? value, JsonSerializer jSerializer)
    {
        if (IsWholeValue(value, out string? result))
        {
            jWriter.WriteRawValue(result);
        }
        else
        {
            jWriter.WriteRawValue(JsonConvert.ToString(value)); // allocations
        }
    }

    private static bool IsWholeValue(object? value, out string? result)
    {
        if (value is decimal dm)
        {
            var decBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref dm);
            int precision = ((int)decBits.Flags >> 16) & 0x000000FF;
            if (precision == 0)
            {
                result = dm.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            {
                result = null;
                return false;
            }
        }
        else if (value is float f)
        {
            double doubleValue = f;
            if (doubleValue == Math.Truncate(doubleValue))
            {
                result = doubleValue.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
        else if (value is double d)
        {
            if (d == Math.Truncate(d))
            {
                result = d.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        throw new NotSupportedException();
    }
}
