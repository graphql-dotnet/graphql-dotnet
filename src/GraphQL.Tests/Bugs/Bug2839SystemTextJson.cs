using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.SystemTextJson;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug2839SystemTextJson
{
    [Fact]
    public void Bug2839Test()
    {
        var schema = new Schema { Query = new TestQuery() };
        schema.ReplaceScalar(new MyDateTimeGraphType());

        var exec = new DocumentExecuter();

        var result = exec.ExecuteAsync(options =>
        {
            options.Schema = schema;
            options.Query = "{ test { thisIsAString, thisIsADateTime } }";
        }
        ).Result;

        var writer = new GraphQLSerializer(options =>
        {
            options.PropertyNamingPolicy = new MyNamingPolicy();
            options.Converters.Add(new MyDateTimeConverter());
        });

        var str = writer.Serialize(result);
        str.ShouldBeCrossPlatJson("{\"data\":{\"TEST\":{\"THISISASTRING\":\"String Value\",\"THISISADATETIME\":\"2022-Jan-04\"}}}");
    }

    public class TestQuery : ObjectGraphType
    {
        public TestQuery()
        {
            Name = "testQuery";
            Description = "Test description";

            Field<TestResponseType>(
                name: "test",
                resolve: context => new TestResponse());
        }
    }

    public class TestResponseType : ObjectGraphType<TestResponse>
    {
        public TestResponseType()
        {
            Name = "TestReponse";
            Field<StringGraphType>("ThisIsAString");
            Field<DateTimeGraphType>("ThisIsADateTime");
        }
    }

    public class TestResponse
    {
        public string ThisIsAString { get; set; }
        public DateTime ThisIsADateTime { get; set; }
        public TestResponse()
        {
            ThisIsAString = "String Value";
            ThisIsADateTime = new DateTime(2022, 1, 4, 10, 20, 30);
        }
    }

    public class MyDateTimeGraphType : DateTimeGraphType
    {
        public MyDateTimeGraphType()
        {
            Name = "DateTime";
        }

        public override object Serialize(object value) => value switch
        {
            DateTime _ => value,
            DateTimeOffset _ => value,
            null => null,
            _ => ThrowSerializationError(value)
        };
    }

    public class MyDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MMM-dd", CultureInfo.InvariantCulture));
        }
    }

    public class MyNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name.ToUpper(CultureInfo.InvariantCulture);
    }
}
