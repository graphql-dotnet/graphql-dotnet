using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Bugs;

public class Bug4064
{
    private ISchema Schema { get; }

    public Bug4064()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<Query>());
        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        Schema = schema;
    }

    [Fact]
    public async Task ListOfObjectsCoercesToListOfObjects()
    {
        var result = await Schema.ExecuteAsync(_ =>
        {
            _.Query = "query ($input: [InputObj!]!) { testObjects(input: $input) }";
            _.Variables = """
                {
                    "input": [{
                        "field1": "value1"
                    }]
                }
                """.ToInputs();
        });

        result.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "testObjects": "value1"
                }
            }
            """);
    }

    [Fact]
    public async Task SingleObjectCoercesToListOfObjects()
    {
        var result = await Schema.ExecuteAsync(_ =>
        {
            _.Query = "query ($input: [InputObj!]!) { testObjects(input: $input) }";
            _.Variables = """
                {
                    "input": {
                        "field1": "value1"
                    }
                }
                """.ToInputs();
        });

        result.ShouldBeCrossPlatJson("""
            {
                "data": {
                    "testObjects": "value1"
                }
            }
            """);
    }
    [Fact]
    public async Task ListOfStringsCoercesToListOfStrings()
    {
        var result = await Schema.ExecuteAsync(_ =>
        {
            _.Query = "query ($input: [String!]!) { testStrings(input: $input) }";
            _.Variables = """
            {
                "input": ["value1"]
            }
            """.ToInputs();
        });

        result.ShouldBeCrossPlatJson("""
        {
            "data": {
                "testStrings": "value1"
            }
        }
        """);
    }

    [Fact]
    public async Task SingleStringCoercesToListOfStrings()
    {
        var result = await Schema.ExecuteAsync(_ =>
        {
            _.Query = "query ($input: [String!]!) { testStrings(input: $input) }";
            _.Variables = """
            {
                "input": "value1"
            }
            """.ToInputs();
        });

        result.ShouldBeCrossPlatJson("""
        {
            "data": {
                "testStrings": "value1"
            }
        }
        """);
    }

    [Fact]
    public async Task ListOfIntsCoercesToListOfInts()
    {
        var result = await Schema.ExecuteAsync(_ =>
        {
            _.Query = "query ($input: [Int!]!) { testInts(input: $input) }";
            _.Variables = """
            {
                "input": [1]
            }
            """.ToInputs();
        });

        result.ShouldBeCrossPlatJson("""
        {
            "data": {
                "testInts": 1
            }
        }
        """);
    }

    [Fact]
    public async Task SingleIntCoercesToListOfInts()
    {
        var result = await Schema.ExecuteAsync(_ =>
        {
            _.Query = "query ($input: [Int!]!) { testInts(input: $input) }";
            _.Variables = """
            {
                "input": 1
            }
            """.ToInputs();
        });

        result.ShouldBeCrossPlatJson("""
        {
            "data": {
                "testInts": 1
            }
        }
        """);
    }

    public class Query
    {
        public static string? TestObjects(IEnumerable<InputObj> input) => input.FirstOrDefault()?.Field1;
        public static string? TestStrings(IEnumerable<string> input) => input.FirstOrDefault();
        public static int? TestInts(IEnumerable<int> input) => input.FirstOrDefault();
    }

    public class InputObj
    {
        public required string Field1 { get; set; }
    }
}
