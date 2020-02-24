using System.Linq;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class NonNullGraphTypeTests : QueryTestBase<NullableSchema>
    {
        [Fact]
        public void nullable_fields_with_values_never_complain()
        {
            AssertQuerySuccess(
                "{ nullable { a b c } }",
                @"{ ""nullable"": { ""a"": 99, ""b"": true, ""c"": ""Hello world"" } }",
                root: new ExampleContext(99, true, "Hello world"));
        }

        [Fact]
        public void nullable_fields_without_values_never_complain()
        {
            AssertQuerySuccess(
                @"{ nullable { a b c } }",
                @"{ ""nullable"": { ""a"": null, ""b"": null, ""c"": null } }",
                root: new ExampleContext(null, null, null));
        }

        [Fact]
        public void nonnullable_fields_with_values_never_complain()
        {
            AssertQuerySuccess(
                "{ nonNullable { a b c } }",
                @"{ ""nonNullable"": { ""a"": 99, ""b"": true, ""c"": ""Hello world"" } }",
                root: new ExampleContext(99, true, "Hello world"));
        }

        [Fact]
        public void nonnullable_fields_without_values_do_complain()
        {
            var result = AssertQueryWithErrors(
                "{ nonNullable { a b c } }",
                @"{ ""nonNullable"": null }",
                root: new ExampleContext(null, null, null),
                expectedErrorCount: 3);

            var errors = result.Errors.ToArray();
            errors[0].Message.ShouldBe("Cannot return null for non-null type. Field: a, Type: Int!.");
            errors[1].Message.ShouldBe("Cannot return null for non-null type. Field: b, Type: Boolean!.");
            errors[2].Message.ShouldBe("Cannot return null for non-null type. Field: c, Type: String!.");
        }
    }

    public class ExampleContext
    {
        public ExampleContext(int? a, bool? b, string c)
        {
            A = a;
            B = b;
            C = c;
        }

        public int? A { get; set; }

        public bool? B { get; set; }

        public string C { get; set; }
    }

    public class NullableSchema : Schema
    {
        public NullableSchema()
        {   var query = new ObjectGraphType();

            query.Field<NullableSchemaType>("nullable",
                resolve: c => new NullableSchemaType { Data = c.Source as ExampleContext });
            query.Field<NonNullableSchemaType>("nonNullable",
                resolve: c => new NonNullableSchemaType { Data = c.Source as ExampleContext });

            Query = query;
        }
    }

    public class NullableSchemaType : ObjectGraphType<NullableSchemaType>
    {
        public NullableSchemaType()
        {
            Field<IntGraphType>("a", resolve: _ => _.Source.Data.A);
            Field<BooleanGraphType>("b", resolve: _ => _.Source.Data.B);
            Field<StringGraphType>("c", resolve: _ => _.Source.Data.C);
        }

        public ExampleContext Data { get; set; }
    }

    public class NonNullableSchemaType : ObjectGraphType<NonNullableSchemaType>
    {
        public NonNullableSchemaType()
        {

            Field<NonNullGraphType<IntGraphType>>("a", resolve: _ => _.Source.Data.A);
            Field<NonNullGraphType<BooleanGraphType>>("b", resolve: _ => _.Source.Data.B);
            Field<NonNullGraphType<StringGraphType>>("c", resolve: _ => _.Source.Data.C);
        }

        public ExampleContext Data { get; set; }
    }
}
