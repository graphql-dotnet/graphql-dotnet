using System;
using System.Linq;
using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class NonNullGraphTypeTests : QueryTestBase<NullableSchema>
    {
        [Fact]
        public void nullable_fields_with_values_never_complain()
        {
            AssertQuerySuccess(
                "{ nullable { a b c } }",
                "{ nullable: { a: 99, b: true, c: 'Hello world' } }",
                root: new ExampleContext(99, true, "Hello world"));
        }

        [Fact]
        public void nullable_fields_without_values_never_complain()
        {
            AssertQuerySuccess(
                "{ nullable { a b c } }",
                "{ nullable: { a: null, b: null, c: null } }",
                root: new ExampleContext(null, null, null));
        }

        [Fact]
        public void nonnullable_fields_with_values_never_complain()
        {
            AssertQuerySuccess(
                "{ nonNullable { a b c } }",
                "{ nonNullable: { a: 99, b: true, c: 'Hello world' } }",
                root: new ExampleContext(99, true, "Hello world"));
        }

        [Fact]
        public void nonnullable_fields_without_values_do_complain()
        {
            var result = AssertQueryWithErrors(
                "{ nonNullable { a b c } }",
                "{ nonNullable: { a: null, b: null, c: null } }",
                root: new ExampleContext(null, null, null),
                expectedErrorCount: 3);

            var errors = result.Errors.ToArray();
            errors[0].InnerException.Message.ShouldEqual("Cannot return null for non-null type. Field: a, Type: Int!.");
            errors[1].InnerException.Message.ShouldEqual("Cannot return null for non-null type. Field: b, Type: Boolean!.");
            errors[2].InnerException.Message.ShouldEqual("Cannot return null for non-null type. Field: c, Type: String!.");
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
        {
            Query = new ObjectGraphType();
            Query.Field<NullableSchemaType>("nullable",
                resolve: c => new NullableSchemaType { Data = c.Source as ExampleContext });
            Query.Field<NonNullableSchemaType>("nonNullable",
                resolve: c => new NonNullableSchemaType { Data = c.Source as ExampleContext });
        }
    }

    public class NullableSchemaType : ObjectGraphType
    {
        public NullableSchemaType()
        {
            Field<IntGraphType>("a", resolve: _ => ((NullableSchemaType)_.Source).Data.A);
            Field<BooleanGraphType>("b", resolve: _ => ((NullableSchemaType)_.Source).Data.B);
            Field<StringGraphType>("c", resolve: _ => ((NullableSchemaType)_.Source).Data.C);
        }

        public ExampleContext Data { get; set; }
    }

    public class NonNullableSchemaType : ObjectGraphType
    {
        public NonNullableSchemaType()
        {

            Field<NonNullGraphType<IntGraphType>>("a", resolve: _ => ((NonNullableSchemaType)_.Source).Data.A);
            Field<NonNullGraphType<BooleanGraphType>>("b", resolve: _ => ((NonNullableSchemaType)_.Source).Data.B);
            Field<NonNullGraphType<StringGraphType>>("c", resolve: _ => ((NonNullableSchemaType)_.Source).Data.C);
        }

        public ExampleContext Data { get; set; }
    }
}
