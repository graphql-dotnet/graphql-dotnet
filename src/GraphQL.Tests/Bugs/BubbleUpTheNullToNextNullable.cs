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
        public void NonNull_field_resolve_to_null_should_bubble_up_the_null_to_first_nullable_parent_in_chain_of_nullable()
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

        [Fact]
        public void ListOfNonNull_containing_null_should_bubble_up_the_null()
        {
            const string QUERY = "{ nonNullableDataGraph { listOfNonNullable } }";
            const string EXPECTED = "{ nonNullableDataGraph: { listOfNonNullable: null } }";
            var data = new Data {ListOfStrings = new List<string> {"text", null, null}};
            var errors = new[]
            {
                new ExecutionError("Cannot return null for non-null type. Field: listOfNonNullable, Type: [String!].")
                {
                    Path = new[] {"nonNullableDataGraph", "listOfNonNullable", "1"}
                }
            };

            AssertResult(QUERY, EXPECTED, data, errors);
        }

        [Fact]
        public void NonNullList_resolve_to_null_should_bubble_up_the_null()
        {
            const string QUERY = "{ nullableDataGraph { nonNullableList } }";
            const string EXPECTED = "{ nullableDataGraph: null }";
            var data = new Data {ListOfStrings = null};
            var errors = new[]
            {
                new ExecutionError("Cannot return null for non-null type. Field: nonNullableList, Type: [String]!.")
                {
                    Path = new[] {"nullableDataGraph", "nonNullableList"}
                }
            };

            AssertResult(QUERY, EXPECTED, data, errors);
        }

        [Fact]
        public void NonNullList_resolve_to_null_should_null_top_level_if_no_nullable_parent_found()
        {
            const string QUERY = "{ nonNullableDataGraph { nonNullableList } }";
            const string EXPECTED = null;
            var data = new Data {ListOfStrings = null};
            var errors = new[]
            {
                new ExecutionError("Cannot return null for non-null type. Field: nonNullableList, Type: [String]!.")
                {
                    Path = new[] {"nonNullableDataGraph", "nonNullableList"}
                }
            };

            AssertResult(QUERY, EXPECTED, data, errors);
        }

        [Fact]
        public void NoNullListOfNonNull_contains_null_should_bubble_up_the_null()
        {
            const string QUERY = "{ nullableDataGraph { nonNullableListOfNonNullable } }";
            const string EXPECTED = "{ nullableDataGraph: null }";
            var data = new Data {ListOfStrings = new List<string> {"text", null, null}};
            var errors = new[]
            {
                new ExecutionError(
                    "Cannot return null for non-null type. Field: nonNullableListOfNonNullable, Type: [String!]!.")
                {
                    Path = new[] {"nullableDataGraph", "nonNullableListOfNonNullable", "1"}
                }
            };

            AssertResult(QUERY, EXPECTED, data, errors);
        }

        [Fact]
        public void NonNullListOfNonNull_resolve_to_null_should_bubble_up_the_null()
        {
            const string QUERY = "{ nullableDataGraph { nonNullableListOfNonNullable } }";
            const string EXPECTED = "{ nullableDataGraph: null }";
            var data = new Data {ListOfStrings = null};
            var errors = new[]
            {
                new ExecutionError(
                    "Cannot return null for non-null type. Field: nonNullableListOfNonNullable, Type: [String!]!.")
                {
                    Path = new[] {"nullableDataGraph", "nonNullableListOfNonNullable"}
                }
            };

            AssertResult(QUERY, EXPECTED, data, errors);
        }

        private void AssertResult(string query, string expected, Data data, IReadOnlyList<ExecutionError> errors)
        {
            ExecutionResult result =
                AssertQueryWithErrors(
                    query,
                    expected,
                    root: data,
                    expectedErrorCount: errors.Count);

            ExecutionErrors actualErrors = result.Errors;

            if (errors.Count == 0)
            {
                actualErrors.ShouldBeNull();
            }
            else
            {
                actualErrors.Count.ShouldBe(errors.Count);

                for (var i = 0; i < errors.Count; i++)
                {
                    ExecutionError actualError = actualErrors[i];
                    ExecutionError expectedError = errors[i];

                    actualError.Message.ShouldBe(expectedError.Message);
                    actualError.Path.ShouldBe(expectedError.Path);
                }
            }
        }

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
                resolve: c => c.Source.Data.ListOfStrings);

            Field<NonNullGraphType<ListGraphType<StringGraphType>>>(
                "nonNullableList",
                resolve: c => c.Source.Data.ListOfStrings);

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>(
                "nonNullableListOfNonNullable",
                resolve: c => c.Source.Data.ListOfStrings);

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
        public List<string> ListOfStrings { get; set; }
    }
}
