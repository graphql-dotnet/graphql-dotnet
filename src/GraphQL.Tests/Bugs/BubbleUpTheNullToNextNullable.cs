namespace GraphQL.Tests.Bugs
{
    using GraphQL.Types;
    using Shouldly;
    using System.Collections.Generic;
    using Xunit;

    public class BubbleUpTheNullToNextNullable : QueryTestBase<BubbleNullSchema>
    {
        [Fact]
        public void Nullable_field_resolve_to_null_should_not_bubble_up_the_null()
        {
            const string QUERY = "{ nullableDataGraph { nullable } }";
            const string EXPECTED = "{ nullableDataGraph: { nullable: null } }";
            var data = new Data {Nullable = null};
            var errors = new ExecutionError[] { };

            AssertResult(QUERY, EXPECTED, data, errors);
        }

        [Fact]
        public void NonNull_field_resolve_with_non_null_value_should_not_throw_error()
        {
            const string QUERY = "{ nullableDataGraph { nonNullable } }";
            const string EXPECTED = "{ nullableDataGraph: { nonNullable: 'data' } }";
            var data = new Data { NonNullable = "data" };
            var errors = new ExecutionError[] { };

            AssertResult(QUERY, EXPECTED, data, errors);
        }

        [Fact]
        public void NonNull_field_resolve_to_null_should_bubble_up_null_to_parent()
        {
            const string QUERY = "{ nullableDataGraph { nonNullable } }";
            const string EXPECTED = "{ nullableDataGraph: null }";
            var data = new Data {NonNullable = null};
            var errors = new[]
            {
                new ExecutionError("Cannot return null for non-null type. Field: nonNullable, Type: String!.")
                {
                    Path = new[] {"nullableDataGraph", "nonNullable"}
                }
            };

            AssertResult(QUERY,EXPECTED, data, errors);
        }

        [Fact]
        public void NonNull_field_resolve_to_null_should_bubble_up_the_null_to_first_nullable_parent_in_chain_of_nullables()
        {
            const string QUERY = "{ nullableDataGraph { nullableNest { nonNullable } } }";
            const string EXPECTED = "{ nullableDataGraph: { nullableNest: null } }";
            var data = new Data {NullableNest = new Data {NonNullable = null}};
            var errors = new[]
            {
                new ExecutionError("Cannot return null for non-null type. Field: nonNullable, Type: String!.")
                {
                    Path = new[] {"nullableDataGraph", "nullableNest", "nonNullable"}
                }
            };

            AssertResult(QUERY, EXPECTED, data, errors);
        }

        [Fact]
        public void NonNull_field_resolve_to_null_should_bubble_up_the_null_to_first_nullable_parent_in_chain_of_non_null_fields()
        {
            const string QUERY = "{ nullableDataGraph { nonNullableNest { nonNullable } } }";
            const string EXPECTED = "{ nullableDataGraph: null }";
            var data = new Data {NonNullableNest = new Data {NonNullable = null}};
            var errors = new[]
            {
                new ExecutionError("Cannot return null for non-null type. Field: nonNullable, Type: String!.")
                {
                    Path = new[] {"nullableDataGraph", "nonNullableNest", "nonNullable"}
                }
            };

            AssertResult(QUERY, EXPECTED, data, errors);
        }

        [Fact]
        public void NonNull_field_resolve_to_null_should_null_the_top_level_if_no_nullable_parent_present()
        {
            const string QUERY = "{ nonNullableDataGraph { nonNullableNest { nonNullable } } }";
            const string EXPECTED = null;
            var data = new Data {NonNullableNest = new Data {NonNullable = null}};
            var errors = new[]
            {
                new ExecutionError("Cannot return null for non-null type. Field: nonNullable, Type: String!.")
                {
                    Path = new[] {"nonNullableDataGraph", "nonNullableNest", "nonNullable"}
                }
            };

            AssertResult(QUERY, EXPECTED, data, errors);
        }

        [Theory]
        [MemberData(nameof(ListTestData))]
        public void NullIsNotBubbledInListGraphType(string query, string expected, Data data, ExecutionError[] errors)
        {
            AssertResult(query, expected, data, errors);
        }

        private void AssertResult(string query, string expected, Data data, ExecutionError[] errors)
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

        public static IEnumerable<object[]> ListTestData =>
            new List<object[]>()
            {
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
                },
                new object[]
                {
                    "{ nullableDataGraph { nonNullableListOfNonNullable } }",
                    "{ nullableDataGraph: null }",
                    new Data { ListOfNonNullable = new List<string> { "text", null, null } },
                    new[]
                    {
                        new ExecutionError("Cannot return null for non-null type. Field: nonNullableListOfNonNullable, Type: [String!]!.")
                        {
                            Path = new [] { "nullableDataGraph", "nonNullableListOfNonNullable", "1" }
                        }
                    }
                },
                new object[]
                {
                    "{ nullableDataGraph { nonNullableListOfNonNullable } }",
                    "{ nullableDataGraph: null }",
                    new Data { ListOfNonNullable = null },
                    new[]
                    {
                        new ExecutionError("Cannot return null for non-null type. Field: nonNullableListOfNonNullable, Type: [String!]!.")
                        {
                            Path = new [] { "nullableDataGraph", "nonNullableListOfNonNullable" }
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
