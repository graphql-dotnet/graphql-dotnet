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
        public void Test(string query, string expected, Data data, string[] errors)
        {
            ExecutionResult result = AssertQueryWithErrors(query,
                expected,
                root: data,
                expectedErrorCount: errors.Length);

            ExecutionErrors actualErrors = result.Errors;

            for (var i = 0; i < errors.Length; i++)
            {
                actualErrors[i].Message.ShouldBe(errors[i]);
            }
        }

        [Theory]
        [MemberData(nameof(ListTestData))]
        public void NullIsNotBubbledInListGraphType(string query, string expected, Data data, string[] errors)
        {
            ExecutionResult result = AssertQueryWithErrors(query,
                expected,
                root: data,
                expectedErrorCount: errors.Length);

            ExecutionErrors actualErrors = result.Errors;

            for (var i = 0; i < errors.Length; i++)
            {
                actualErrors[i].Message.ShouldBe(errors[i]);
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
                    new [] { "Cannot return null for non-null type. Field: nonNullable, Type: String!." }
                },
                new object[]
                {
                    "{ nullableDataGraph { nullableNest { nonNullable } } }",
                    "{ nullableDataGraph: { nullableNest: null } }",
                    new Data { NullableNest = new Data { Nullable = null } },
                    new [] { "Cannot return null for non-null type. Field: nonNullable, Type: String!." }
                },
                new object[]
                {
                    "{ nullableDataGraph { nonNullableNest { nonNullable } } }",
                    "{ nullableDataGraph: null }",
                    new Data { NonNullableNest = new Data { Nullable = null } },
                    new [] { "Cannot return null for non-null type. Field: nonNullable, Type: String!." }
                },
                new object[]
                {
                    "{ nonNullableDataGraph { nonNullableNest { nonNullable } } }",
                    null,
                    new Data { NonNullableNest = new Data { Nullable = null } },
                    new [] { "Cannot return null for non-null type. Field: nonNullable, Type: String!." }
                },
                new object[]
                {
                    "{ nonNullableDataGraph { listOfNonNullable } }",
                    "{ nonNullableDataGraph: { listOfNonNullable: null } }",
                    new Data { ListOfNonNullable = new List<string> { "text", null, null } },
                    new [] { "Cannot return null for non-null type. Field: listOfNonNullable, Type: String!." }
                },
                new object[]
                {
                    "{ nullableDataGraph { nonNullableList } }",
                    "{ nullableDataGraph: null }",
                    new Data { ListOfNonNullable = null },
                    new [] { "Cannot return null for non-null type. Field: nonNullableList, Type: ListGraphType!." }
                }
            };

        public static IEnumerable<object[]> ListTestData =>
            new List<object[]>()
            {
                new object[]
                {
                    "{ nonNullableDataGraph { listOfNonNullable } }",
                    "{ nonNullableDataGraph: { listOfNonNullable: null } }",
                    new Data { ListOfNonNullable = new List<string> { "text", null, null } },
                    new [] { "Cannot return null for non-null type. Field: listOfNonNullable, Type: String!." }
                },
                new object[]
                {
                    "{ nullableDataGraph { nonNullableList } }",
                    "{ nullableDataGraph: null }",
                    new Data { ListOfNonNullable = null },
                    // Empty is returned as Type.Name for ListGraphType
                    new [] { "Cannot return null for non-null type. Field: nonNullableList, Type: !." }
                },
                new object[]
                {
                    "{ nullableDataGraph { nonNullableListOfNonNullable } }",
                    "{ nullableDataGraph: null }",
                    new Data { ListOfNonNullable = new List<string> { "text", null, null } },
                    new [] { "Cannot return null for non-null type. Field: nonNullableListOfNonNullable, Type: String!." }
                },
                new object[]
                {
                    "{ nullableDataGraph { nonNullableListOfNonNullable } }",
                    "{ nullableDataGraph: null }",
                    new Data { ListOfNonNullable = null },
                    // Empty is returned as Type.Name for ListGraphType
                    new [] { "Cannot return null for non-null type. Field: nonNullableListOfNonNullable, Type: !." }
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

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>(
                "nonNullableListOfNonNullable",
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
