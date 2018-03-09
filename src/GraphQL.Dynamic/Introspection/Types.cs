using System;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GraphQL.Dynamic.Types.Introspection
{
    public partial class Root
    {
        [JsonProperty("data")]
        public Data Data { get; set; }
    }

    public partial class Data
    {
        [JsonProperty("__schema")]
        public Schema Schema { get; set; }
    }

    public partial class Schema
    {
        [JsonProperty("queryType")]
        public SchemaMutationType QueryType { get; set; }

        [JsonProperty("mutationType")]
        public SchemaMutationType MutationType { get; set; }

        [JsonProperty("subscriptionType")]
        public object SubscriptionType { get; set; }

        [JsonProperty("types")]
        public TypeElement[] Types { get; set; }

        [JsonProperty("directives")]
        public Directive[] Directives { get; set; }
    }

    public partial class Directive
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("args")]
        public DirectiveArg[] Args { get; set; }

        [JsonProperty("onOperation")]
        public bool OnOperation { get; set; }

        [JsonProperty("onFragment")]
        public bool OnFragment { get; set; }

        [JsonProperty("onField")]
        public bool OnField { get; set; }
    }

    public partial class DirectiveArg
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("type")]
        public TypeElementType Type { get; set; }

        [JsonProperty("defaultValue")]
        public object DefaultValue { get; set; }
    }

    public partial class TypeElementType
    {
        [JsonProperty("kind")]
        public TypeElementTypeKind Kind { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ofType")]
        public TypeElementType OfType { get; set; }
    }

    public partial class SchemaMutationType
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public partial class TypeElement
    {
        [JsonProperty("kind")]
        public TypeElementTypeKind Kind { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("fields")]
        public Field[] Fields { get; set; }

        [JsonProperty("inputFields")]
        public DirectiveArg[] InputFields { get; set; }

        [JsonProperty("interfaces")]
        public TypeElementType[] Interfaces { get; set; }

        [JsonProperty("enumValues")]
        public EnumValue[] EnumValues { get; set; }

        [JsonProperty("possibleTypes")]
        public TypeElementType[] PossibleTypes { get; set; }
    }

    public partial class EnumValue
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("isDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonProperty("deprecationReason")]
        public object DeprecationReason { get; set; }
    }

    public partial class Field
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("args")]
        public FieldArg[] Args { get; set; }

        [JsonProperty("type")]
        public TypeElementType Type { get; set; }

        [JsonProperty("isDeprecated")]
        public bool IsDeprecated { get; set; }

        [JsonProperty("deprecationReason")]
        public object DeprecationReason { get; set; }
    }

    public partial class FieldArg
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("type")]
        public TypeElementType Type { get; set; }

        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }
    }

    public enum TypeElementTypeKind { Enum, InputObject, Interface, List, NonNull, Object, Scalar, Union };

    public partial class Root
    {
        public static Root FromJson(string json) => JsonConvert.DeserializeObject<Root>(json, GraphQL.Dynamic.Types.Introspection.Converter.Settings);
    }

    static class InterfaceKindExtensions
    {
        public static TypeElementTypeKind? ValueForString(string str)
        {
            switch (str)
            {
                case "ENUM": return TypeElementTypeKind.Enum;
                case "INPUT_OBJECT": return TypeElementTypeKind.InputObject;
                case "INTERFACE": return TypeElementTypeKind.Interface;
                case "LIST": return TypeElementTypeKind.List;
                case "NON_NULL": return TypeElementTypeKind.NonNull;
                case "OBJECT": return TypeElementTypeKind.Object;
                case "SCALAR": return TypeElementTypeKind.Scalar;
                case "UNION": return TypeElementTypeKind.Union;
                default: return null;
            }
        }

        public static TypeElementTypeKind ReadJson(JsonReader reader, JsonSerializer serializer)
        {
            var str = serializer.Deserialize<string>(reader);
            var maybeValue = ValueForString(str);
            if (maybeValue.HasValue) return maybeValue.Value;
            throw new Exception("Unknown enum case " + str);
        }

        public static void WriteJson(this TypeElementTypeKind value, JsonWriter writer, JsonSerializer serializer)
        {
            switch (value)
            {
                case TypeElementTypeKind.Enum: serializer.Serialize(writer, "ENUM"); break;
                case TypeElementTypeKind.InputObject: serializer.Serialize(writer, "INPUT_OBJECT"); break;
                case TypeElementTypeKind.Interface: serializer.Serialize(writer, "INTERFACE"); break;
                case TypeElementTypeKind.List: serializer.Serialize(writer, "LIST"); break;
                case TypeElementTypeKind.NonNull: serializer.Serialize(writer, "NON_NULL"); break;
                case TypeElementTypeKind.Object: serializer.Serialize(writer, "OBJECT"); break;
                case TypeElementTypeKind.Scalar: serializer.Serialize(writer, "SCALAR"); break;
                case TypeElementTypeKind.Union: serializer.Serialize(writer, "UNION"); break;
            }
        }
    }

    public static class Serialize
    {
        public static string ToJson(this Root self) => JsonConvert.SerializeObject(self, GraphQL.Dynamic.Types.Introspection.Converter.Settings);
    }

    internal class Converter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(TypeElementTypeKind) || t == typeof(TypeElementTypeKind) || t == typeof(TypeElementTypeKind) || t == typeof(TypeElementTypeKind) || t == typeof(TypeElementTypeKind?) || t == typeof(TypeElementTypeKind?) || t == typeof(TypeElementTypeKind?) || t == typeof(TypeElementTypeKind?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (t == typeof(TypeElementTypeKind))
                return InterfaceKindExtensions.ReadJson(reader, serializer);
            if (t == typeof(TypeElementTypeKind?))
            {
                if (reader.TokenType == JsonToken.Null) return null;
                return InterfaceKindExtensions.ReadJson(reader, serializer);
            }
            throw new Exception("Unknown type");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var t = value.GetType();
            if (t == typeof(TypeElementTypeKind))
            {
                ((TypeElementTypeKind)value).WriteJson(writer, serializer);
                return;
            }
            if (t == typeof(TypeElementTypeKind))
            {
                ((TypeElementTypeKind)value).WriteJson(writer, serializer);
                return;
            }
            if (t == typeof(TypeElementTypeKind))
            {
                ((TypeElementTypeKind)value).WriteJson(writer, serializer);
                return;
            }
            if (t == typeof(TypeElementTypeKind))
            {
                ((TypeElementTypeKind)value).WriteJson(writer, serializer);
                return;
            }
            throw new Exception("Unknown type");
        }

        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new Converter(),
                new IsoDateTimeConverter()
                {
                    DateTimeStyles = DateTimeStyles.AssumeUniversal,
                },
            },
        };
    }
}
