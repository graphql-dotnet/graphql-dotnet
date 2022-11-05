using GraphQL.Types;

namespace GraphQL.Tests.Types;

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
        errors[0].Message.ShouldBe("Error trying to resolve field 'a'.");
        errors[0].InnerException.Message.ShouldBe("Cannot return null for a non-null type. Field: a, Type: Int!.");
        errors[1].Message.ShouldBe("Error trying to resolve field 'b'.");
        errors[1].InnerException.Message.ShouldBe("Cannot return null for a non-null type. Field: b, Type: Boolean!.");
        errors[2].Message.ShouldBe("Error trying to resolve field 'c'.");
        errors[2].InnerException.Message.ShouldBe("Cannot return null for a non-null type. Field: c, Type: String!.");
    }

    [Fact]
    public void NonNull_Wrapped_With_NonNull_Should_Throw()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new NonNullGraphType<NonNullGraphType<StringGraphType>>()).ParamName.ShouldBe("type");
        Should.Throw<ArgumentOutOfRangeException>(() => new NonNullGraphType(new NonNullGraphType(new StringGraphType()))).ParamName.ShouldBe("ResolvedType");
    }

    [Fact]
    public void NonNull_ResolvedType_And_Type_Should_Match()
    {
        var type = new NonNullGraphType<StringGraphType>();
        Should.Throw<ArgumentOutOfRangeException>(() => type.ResolvedType = new IntGraphType()).Message.ShouldStartWith("Type 'StringGraphType' should be assignable from ResolvedType 'IntGraphType'.");
    }

    [Fact]
    public void NonNull_Name_Should_Be_Null()
    {
        new NonNullGraphType<StringGraphType>().Name.ShouldBeNull();
        new NonNullGraphType(new StringGraphType()).Name.ShouldBeNull();
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
        var query = new ObjectGraphType();

        query.Field<NullableSchemaType>("nullable")
            .Resolve(c => new DataModel { Data = c.Source as ExampleContext });
        query.Field<NonNullableSchemaType>("nonNullable")
            .Resolve(c => new DataModel { Data = c.Source as ExampleContext });

        Query = query;
    }
}

public class DataModel
{
    public ExampleContext Data { get; set; }
}

public class NullableSchemaType : ObjectGraphType<DataModel>
{
    public NullableSchemaType()
    {
        Field<IntGraphType>("a").Resolve(_ => _.Source.Data.A);
        Field<BooleanGraphType>("b").Resolve(_ => _.Source.Data.B);
        Field<StringGraphType>("c").Resolve(_ => _.Source.Data.C);
    }
}

public class NonNullableSchemaType : ObjectGraphType<DataModel>
{
    public NonNullableSchemaType()
    {
        Field<NonNullGraphType<IntGraphType>>("a").Resolve(_ => _.Source.Data.A);
        Field<NonNullGraphType<BooleanGraphType>>("b").Resolve(_ => _.Source.Data.B);
        Field<NonNullGraphType<StringGraphType>>("c").Resolve(_ => _.Source.Data.C);
    }
}
