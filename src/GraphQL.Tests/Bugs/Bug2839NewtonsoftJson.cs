using GraphQL.Execution;
using GraphQL.NewtonsoftJson;
using GraphQL.Types;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace GraphQL.Tests.Bugs;

public class Bug2839NewtonsoftJson
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
            options.Converters.Add(new IsoDateTimeConverter()
            {
                DateTimeFormat = "yyyy-MMM-dd",
                Culture = System.Globalization.CultureInfo.InvariantCulture,
            });

            options.ContractResolver = new GraphQLContractResolver(new ErrorInfoProvider())
            {
                NamingStrategy = new KebabCaseNamingStrategy(true, false, false)
            };
        });

        var str = writer.Serialize(result);
        str.ShouldBeCrossPlatJson("{\"data\":{\"test\":{\"this-is-a-string\":\"String Value\",\"this-is-a-date-time\":\"2022-Jan-04\"}}}");
    }

    public class TestQuery : ObjectGraphType
    {
        public TestQuery()
        {
            Name = "testQuery";
            Description = "Test description";

            Field<TestResponseType>("test").Resolve(_ => new TestResponse());
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
}
