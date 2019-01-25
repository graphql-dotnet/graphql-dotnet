namespace GraphQL.Tests.Bugs
{
    using GraphQL.Types;
    using Shouldly;
    using System.Collections.Generic;
    using Xunit;

    public class BubbleUpTheNullToNextNullable : QueryTestBase<BubbleNullSchema>
    {
        [Theory]
        [MemberData(nameof(TestData))]
        public void Test(string query, string expected, Data data, ExecutionError[] errors)
        {
            ExecutionResult result =
                AssertQueryWithErrors(
                    query,
                    expected,
                    root: data,
                    expectedErrorCount: errors.Length);

            ExecutionErrors actualErrors = result.Errors;

            if (errors.Length == 0)
            {
                actualErrors.ShouldBeNull();
            }
            else
            {
                actualErrors.Count.ShouldBe(errors.Length);

                for (var i = 0; i < errors.Length; i++)
                {
                    ExecutionError actualError = actualErrors[i];
                    ExecutionError expectedError = errors[i];

                    actualError.Message.ShouldBe(expectedError.Message);
                    actualError.Path.ShouldBe(expectedError.Path);
                }
            }
        }

        public static IEnumerable<object[]> TestData =>
            new List<object[]>()
            {
                new object[]
                {
                    "{ nullableDataGraph { nullable } }",
                    "{ nullableDataGraph: { nullable: null } }",
                    new Data { Nullable = null },
                    new string[] { }
                },
                new object[]
                {
                    "{ nullableDataGraph { nonNullable } }",
                    "{ nullableDataGraph: null }",
                    new Data { NonNullable = null },
                    new[]
                    {
                        new ExecutionError("Cannot return null for non-null type. Field: nonNullable, Type: String!.")
                        {
                            Path = new [] { "nullableDataGraph", "nonNullable" }
                        }
                    }
                },
                new object[]
                {
                    "{ nullableDataGraph { nullableNest { nonNullable } } }",
                    "{ nullableDataGraph: { nullableNest: null } }",
                    new Data { NullableNest = new Data { Nullable = null } },
                    new[]
                    {
                        new ExecutionError("Cannot return null for non-null type. Field: nonNullable, Type: String!.")
                        {
                            Path = new [] { "nullableDataGraph", "nullableNest", "nonNullable" }
                        }
                    }
                },
                new object[]
                {
                    "{ nullableDataGraph { nonNullableNest { nonNullable } } }",
                    "{ nullableDataGraph: null }",
                    new Data { NonNullableNest = new Data { Nullable = null } },
                    new[]
                    {
                        new ExecutionError("Cannot return null for non-null type. Field: nonNullable, Type: String!.")
                        {
                            Path = new [] { "nullableDataGraph", "nonNullableNest", "nonNullable" }
                        }
                    }
                },
                new object[]
                {
                    "{ nonNullableDataGraph { nonNullableNest { nonNullable } } }",
                    null,
                    new Data { NonNullableNest = new Data { Nullable = null } },
                    new[]
                    {
                        new ExecutionError("Cannot return null for non-null type. Field: nonNullable, Type: String!.")
                        {
                            Path = new [] { "nonNullableDataGraph", "nonNullableNest", "nonNullable" }
                        }
                    }
                },
                new object[]
                {
                    "{ nonNullableDataGraph { listOfNonNullable } }",
                    "{ nonNullableDataGraph: { listOfNonNullable: null } }",
                    new Data { ListOfNonNullable = new List<string> { "text", null, null } },
                    new[] {
                        new ExecutionError("Cannot return null for non-null type. Field: listOfNonNullable, Type: [String!].")
                        {
                            Path = new [] { "nonNullableDataGraph", "listOfNonNullable", "1" }
                        }
                    }
                },
                new object[]
                {
                    "{ nullableDataGraph { nonNullableList } }",
                    "{ nullableDataGraph: null }",
                    new Data { ListOfNonNullable = null },
                    new[]
                    {
                        new ExecutionError("Cannot return null for non-null type. Field: nonNullableList, Type: [String]!.")
                        {
                            Path = new [] { "nullableDataGraph", "nonNullableList" }
                        }
                    }
                },
                new object[]
                {
                    "{ nonNullableDataGraph { nonNullableList } }",
                    null,
                    new Data { ListOfNonNullable = null },
                    new[]
                    {
                        new ExecutionError("Cannot return null for non-null type. Field: nonNullableList, Type: [String]!.")
                        {
                            Path = new [] { "nonNullableDataGraph", "nonNullableList" }
                        }
                    }
                }
            };
    }

    public class BubbleNullSchema : Schema
    {
        public BubbleNullSchema()
        {
            var query = new ObjectGraphType();

            query.Field<NonNullGraphType<DataGraphType>>(
                "nonNullableDataGraph",
                resolve: c => new DataGraphType() { Data = c.Source as Data }
            );

            query.Field<DataGraphType>(
                "nullableDataGraph",
                resolve: c => new DataGraphType() { Data = c.Source as Data }
            );

            Query = query;
        }
    }

    public class DataGraphType : ObjectGraphType<DataGraphType>
    {
        public DataGraphType()
        {
            Name = "dataType";

            Field<StringGraphType>(
                "nullable",
                resolve: c => c.Source.Data.Nullable);

            Field<NonNullGraphType<StringGraphType>>(
                "nonNullable",
                resolve: c => c.Source.Data.NonNullable);

            Field<ListGraphType<NonNullGraphType<StringGraphType>>>(
                "listOfNonNullable",
                resolve: c => c.Source.Data.ListOfNonNullable);

            Field<NonNullGraphType<ListGraphType<StringGraphType>>>(
                "nonNullableList",
                resolve: c => c.Source.Data.ListOfNonNullable);

            Field<NonNullGraphType<DataGraphType>>(
                "nonNullableNest",
                resolve: c => new DataGraphType() { Data = c.Source.Data.NonNullableNest });

            Field<DataGraphType>(
                 "nullableNest",
                resolve: c => new DataGraphType() { Data = c.Source.Data.NullableNest });
        }

        public Data Data { get; set; }
    }

    public class Data
    {
        public string Nullable { get; set; }
        public Data NullableNest { get; set; }
        public string NonNullable { get; set; }
        public Data NonNullableNest { get; set; }
        public List<string> ListOfNonNullable { get; set; }
    }
}
