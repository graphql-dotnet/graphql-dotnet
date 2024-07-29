using System.Numerics;
using GraphQL.Tests.StarWars;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Serialization;

/// <summary>
/// Tests for <see cref="IGraphQLTextSerializer"/> implementations and the custom converters
/// that are used in the process of serializing an <see cref="ExecutionResult"/> to JSON.
/// </summary>
public class ExecutionResultTests
{
    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Can_Write_Execution_Result(IGraphQLTextSerializer serializer)
    {
        var executionResult = new ExecutionResult
        {
            Executed = true,
            Data = """{ "someType": { "someProperty": "someValue" } }""".ToDictionary().ToExecutionTree(),
            Errors = new ExecutionErrors
            {
                new ExecutionError("some error 1"),
                new ExecutionError("some error 2"),
            },
            Extensions = new Dictionary<string, object?>
            {
                { "someExtension", new { someProperty = "someValue", someOtherProperty = 1 } }
            }
        };

        const string expected = """
            {
              "errors": [
              {
                "message": "some error 1"
              },
              {
                "message": "some error 2"
              }
              ],
              "data": {
                "someType": {
                  "someProperty": "someValue"
                }
              },
              "extensions": {
                "someExtension": {
                  "someProperty": "someValue",
                  "someOtherProperty": 1
                }
              }
            }
            """;

        string actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_Correct_Execution_Result_With_Null_Data_And_Null_Errors(IGraphQLTextSerializer serializer)
    {
        var executionResult = new ExecutionResult { Executed = true };

        const string expected = """{"data": null}""";

        string actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_Correct_Execution_Result_With_Null_Data_And_Some_Errors(IGraphQLTextSerializer serializer)
    {
        // "If an error was encountered before execution begins, the data entry should not be present in the result."
        // Source: https://github.com/graphql/graphql-spec/blob/master/spec/Section%207%20--%20Response.md#data

        var executionResult = new ExecutionResult
        {
            Errors = new ExecutionErrors
            {
                new ExecutionError("some error 1"),
                new ExecutionError("some error 2"),
            }
        };

        const string expected = """
            {
              "errors": [{"message":"some error 1"},{"message":"some error 2"}]
            }
            """;

        string actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_Correct_Execution_Result_With_Empty_Data_Errors_And_Extensions_When_Executed(IGraphQLTextSerializer serializer)
    {
        var executionResult = new ExecutionResult
        {
            Data = new Dictionary<string, object?>().ToExecutionTree(),
            Errors = new ExecutionErrors(),
            Extensions = new Dictionary<string, object?>(),
            Executed = true
        };

        const string expected = """{ "data": {} }""";

        string actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_Correct_Execution_Result_With_Empty_Data_Errors_And_Extensions_When_Not_Executed(IGraphQLTextSerializer writer)
    {
        var executionResult = new ExecutionResult
        {
            Data = new Dictionary<string, object?>().ToExecutionTree(),
            Errors = new ExecutionErrors(),
            Extensions = new Dictionary<string, object?>(),
            Executed = false
        };

        const string expected = "{ }";

        string actual = writer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public void Writes_Correct_Execution_Result_With_Null_Data_Errors_And_Extensions_When_Executed(IGraphQLTextSerializer serializer)
    {
        var executionResult = new ExecutionResult
        {
            Data = null,
            Errors = new ExecutionErrors(),
            Extensions = new Dictionary<string, object?>(),
            Executed = true
        };

        const string expected = """{ "data": null }""";

        string actual = serializer.Serialize(executionResult);

        actual.ShouldBeCrossPlatJson(expected);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public async Task Synchronous_and_Async_Works_Same(IGraphQLTextSerializer serializer)
    {
        //ISSUE: manually created test instance with ServiceProvider
        var builder = new MicrosoftDI.GraphQLBuilder(new ServiceCollection(), b => new StarWarsTestBase().RegisterServices(b.Services));
        var schema = new GraphQL.StarWars.StarWarsSchema(builder.ServiceCollection.BuildServiceProvider());
        var result = await new DocumentExecuter().ExecuteAsync(new ExecutionOptions
        {
            Schema = schema,
            Query = "IntrospectionQuery".ReadGraphQLRequest()
        });
        string syncResult = serializer.Serialize(result);
        var stream = new System.IO.MemoryStream();
        await serializer.WriteAsync(stream, result);
        string asyncResult = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        syncResult.ShouldBe(asyncResult);
    }

    [Theory]
    [ClassData(typeof(GraphQLSerializersTestData))]
    public async Task IntegralListTypesSerializeCorrectly(IGraphQLTextSerializer serializer)
    {
        var schema = new Schema
        {
            Query = new AutoRegisteringObjectGraphType<ArraySampleQuery>(),
        };
        schema.Initialize();
        var result = await new DocumentExecuter().ExecuteAsync(new ExecutionOptions
        {
            Schema = schema,
            Query = """
                {
                    bools bools2 sBytes sBytes2 bytes bytes2 bytes3 shorts shorts2 uShorts uShorts2
                    ints ints2 ints3 uInts uInts2 longs longs2 uLongs uLongs2 singles singles2
                    doubles doubles2 decimals decimals2 bigIntegers bigIntegers2 strings strings2
                    guids guids2 ids1 ids2 ids3 ids4 ids5 ids6
                }
                """
        });
        string actual = serializer.Serialize(result);
        actual.ShouldBeCrossPlatJson("""
            {
              "data": {
                "bools": [true, false],
                "bools2": [true, null],
                "sBytes": [-1, 0, 1],
                "sBytes2": [-1, null, 1],
                "bytes": [0, 1, 2],
                "bytes2": [null, 1, 2],
                "bytes3": [0, 1, 2],
                "shorts": [-1, 0, 1],
                "shorts2": [-1, null, 1],
                "uShorts": [0, 1, 2],
                "uShorts2": [null, 1, 2],
                "ints": [-1, 0, 1],
                "ints2": [-1, null, 1],
                "ints3": [-1, 0, 1],
                "uInts": [0, 1, 2],
                "uInts2": [null, 1, 2],
                "longs": [-1, 0, 1],
                "longs2": [-1, null, 1],
                "uLongs": [0, 1, 2],
                "uLongs2": [null, 1, 2],
                "singles": [-1.5, 0, 1.5],
                "singles2": [-1.5, null, 1.5],
                "doubles": [-1.5, 0, 1.5],
                "doubles2": [-1.5, null, 1.5],
                "decimals": [-1.5, 0, 1.5],
                "decimals2": [-1.5, null, 1.5],
                "bigIntegers": [-1, 0, 1],
                "bigIntegers2": [-1, null, 1],
                "strings": ["abc", "def", "ghi"],
                "strings2": ["abc", null, "ghi"],
                "guids": ["00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000"],
                "guids2": ["00000000-0000-0000-0000-000000000000", null, "00000000-0000-0000-0000-000000000000"],
                "ids1": ["1", "2", "3"],
                "ids2": ["1", null, "3"],
                "ids3": ["1", "2", "3"],
                "ids4": ["1", null, "3"],
                "ids5": ["00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000"],
                "ids6": ["00000000-0000-0000-0000-000000000000", null, "00000000-0000-0000-0000-000000000000"]
              }
            }
            """);
    }

    private class ArraySampleQuery()
    {
        public static bool[] Bools => [true, false];
        public static bool?[] Bools2 => [true, null];

        public static sbyte[] SBytes => [-1, 0, 1];
        public static sbyte?[] SBytes2 => [-1, null, 1];
        public static byte[] Bytes => [0, 1, 2];
        public static byte?[] Bytes2 => [null, 1, 2];
        public static List<byte> Bytes3 => [0, 1, 2];
        public static short[] Shorts => [-1, 0, 1];
        public static short?[] Shorts2 => [-1, null, 1];
        public static ushort[] UShorts => [0, 1, 2];
        public static ushort?[] UShorts2 => [null, 1, 2];
        public static int[] Ints => [-1, 0, 1];
        public static int?[] Ints2 => [-1, null, 1];
        [OutputType(typeof(NonNullGraphType<ListGraphType<NonNullGraphType<IntGraphType>>>))]
        public static long[] Ints3 => [-1, 0, 1];
        public static uint[] UInts => [0, 1, 2];
        public static uint?[] UInts2 => [null, 1, 2];
        public static long[] Longs => [-1, 0, 1];
        public static long?[] Longs2 => [-1, null, 1];
        public static ulong[] ULongs => [0, 1, 2];
        public static ulong?[] ULongs2 => [null, 1, 2];
        public static float[] Singles => [-1.5f, 0, 1.5f];
        public static float?[] Singles2 => [-1.5f, null, 1.5f];
        public static double[] Doubles => [-1.5, 0, 1.5];
        public static double?[] Doubles2 => [-1.5, null, 1.5];
        public static decimal[] Decimals => [-1.5m, 0, 1.5m];
        public static decimal?[] Decimals2 => [-1.5m, null, 1.5m];
        public static BigInteger[] BigIntegers => [-1, 0, 1];
        public static BigInteger?[] BigIntegers2 => [-1, null, 1];
        public static string[] Strings => ["abc", "def", "ghi"];
        public static string?[] Strings2 => ["abc", null, "ghi"];
        public static Guid[] Guids => [Guid.Empty, Guid.Empty, Guid.Empty];
        public static Guid?[] Guids2 => [Guid.Empty, null, Guid.Empty];
        [Id]
        public static int[] Ids1 => [1, 2, 3];
        [Id]
        public static int?[] Ids2 => [1, null, 3];
        [Id]
        public static long[] Ids3 => [1, 2, 3];
        [Id]
        public static long?[] Ids4 => [1, null, 3];
        [Id]
        public static Guid[] Ids5 => [Guid.Empty, Guid.Empty, Guid.Empty];
        [Id]
        public static Guid?[] Ids6 => [Guid.Empty, null, Guid.Empty];
    }
}
