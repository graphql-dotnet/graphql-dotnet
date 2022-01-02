using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.SystemTextJson
{
    /// <summary>
    /// Provides extension methods to deserialize json strings into object dictionaries.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new InputsConverter(),
                new JsonConverterBigInteger(),
            }
        };

        private static readonly JsonSerializerOptions _jsonOptions2 = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new InputsConverter(),
                new JsonConverterBigInteger(),
                new GraphQLRequestConverter(),
                new GraphQLRequestListConverter(),
            }
        };

        /// <summary>
        /// Converts a JSON-formatted string into a dictionary.
        /// </summary>
        /// <param name="json">A JSON formatted string.</param>
        /// <returns>Inputs.</returns>
        public static Inputs ToInputs(this string json)
            => json != null ? JsonSerializer.Deserialize<Inputs>(json, _jsonOptions2) : Inputs.Empty;

        /// <summary>
        /// Deserializes a JSON-formatted string of data into the specified type.
        /// <br/><br/>
        /// Any <see cref="Inputs"/> objects will be deserialized into the proper format.
        /// <br/><br/>
        /// Property names are converted to camel case and matched based on a case sensitive comparison (the default for System.Text.Json).
        /// </summary>
        public static T FromJson<T>(this string json)
            => JsonSerializer.Deserialize<T>(json, _jsonOptions2);

        /// <summary>
        /// Deserializes a JSON-formatted stream of data into the specified type.
        /// <br/><br/>
        /// Any <see cref="Inputs"/> objects will be deserialized into the proper format.
        /// <br/><br/>
        /// Property names are converted to camel case and matched based on a case sensitive comparison (the default for System.Text.Json).
        /// </summary>
        public static ValueTask<T> FromJsonAsync<T>(this System.IO.Stream stream, CancellationToken cancellationToken = default)
            => JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions2, cancellationToken);

        /// <summary>
        /// Converts a JSON element into an <see cref="Inputs"/> object.
        /// </summary>
        public static Inputs ToInputs(this JsonElement obj)
        {
            if (obj.ValueKind == JsonValueKind.Null || obj.ValueKind == JsonValueKind.Undefined)
                return Inputs.Empty;

            if (obj.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("This element is not an object element");

            var dic = (Dictionary<string, object>)GetValue(obj);
            return dic.ToInputs();

            object GetValue(JsonElement node)
            {
                switch (node.ValueKind)
                {
                    case JsonValueKind.Null:
                        return null;
                    case JsonValueKind.True:
                        return BoolBox.True;
                    case JsonValueKind.False:
                        return BoolBox.False;
                    case JsonValueKind.String:
                        return node.GetString();
                    case JsonValueKind.Number:
                        if (node.TryGetInt32(out var val))
                            return val;
                        if (node.TryGetInt64(out var val2))
                            return val2;
                        if (node.TryGetUInt64(out var val3))
                            return val3;
                        if (BigInteger.TryParse(node.GetRawText(), out var val4))
                            return val4;
                        if (node.TryGetDouble(out var val5))
                            return val5;
                        if (node.TryGetDecimal(out var val6))
                            return val6;
                        throw new NotImplementedException($"Unexpected Number value. Raw text was: {node.GetRawText()}");
                    case JsonValueKind.Object:
                        var dic = new Dictionary<string, object>();
                        foreach (var prop in node.EnumerateObject())
                        {
                            dic.Add(prop.Name, GetValue(prop.Value));
                        }
                        return dic;
                    case JsonValueKind.Array:
                        var array = new List<object>();
                        foreach (var prop in node.EnumerateArray())
                        {
                            array.Add(GetValue(prop));
                        }
                        return array;
                    default:
                        throw new NotImplementedException($"Unexpected element type '{node.ValueKind}'.");
                }
            }
        }
    }
}
