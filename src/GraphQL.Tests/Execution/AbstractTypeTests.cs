using GraphQL.Types;

namespace GraphQL.Tests.Execution;

public class AbstractTypeErrorTests : QueryTestBase<AbstractSchema>
{
    [Fact]
    public void throws_when_unable_to_determine_object_type()
    {
        var result = AssertQueryWithErrors("{ pets { name } }", """{ "pets": null }""", expectedErrorCount: 1);
        var error = result.Errors!.First();
        error.Message.ShouldBe("Error trying to resolve field 'pets'.");
        error.InnerException.ShouldNotBeNull();
        error.InnerException.Message.ShouldBe("Abstract type Pet must resolve to an Object type at runtime for field Query.pets with value '{ name = Eli }', received 'null'.");
    }

    [Fact]
    public void throws_when_unable_to_determine_object_type_nonnull()
    {
        var result = AssertQueryWithErrors("{ pets2 { name } }", null, expectedErrorCount: 1);
        var error = result.Errors!.First();
        error.Message.ShouldBe("Error trying to resolve field 'pets2'.");
        error.InnerException.ShouldNotBeNull();
        error.InnerException.Message.ShouldBe("Abstract type Pet must resolve to an Object type at runtime for field Query.pets2 with value '{ name = Eli }', received 'null'.");
    }

    [Fact]
    public void throws_when_unable_to_determine_object_type_list()
    {
        var result = AssertQueryWithErrors("{ pets3 { name } }", """{ "pets3": [ null ] }""", expectedErrorCount: 1);
        var error = result.Errors!.First();
        error.Message.ShouldBe("Error trying to resolve field 'pets3'.");
        error.InnerException.ShouldNotBeNull();
        error.InnerException.Message.ShouldBe("Abstract type Pet must resolve to an Object type at runtime for field Query.pets3 with value '{ name = Eli }', received 'null'.");
    }

    [Fact]
    public void throws_when_unable_to_determine_object_type_list_nonnull()
    {
        var result = AssertQueryWithErrors("{ pets4 { name } }", """{ "pets4": null }""", expectedErrorCount: 1);
        var error = result.Errors!.First();
        error.Message.ShouldBe("Error trying to resolve field 'pets4'.");
        error.InnerException.ShouldNotBeNull();
        error.InnerException.Message.ShouldBe("Abstract type Pet must resolve to an Object type at runtime for field Query.pets4 with value '{ name = Eli }', received 'null'.");
    }

    [Fact]
    public void throws_when_unable_to_determine_object_type_nonnull_list()
    {
        var result = AssertQueryWithErrors("{ pets5 { name } }", """{ "pets5": [ null ] }""", expectedErrorCount: 1);
        var error = result.Errors!.First();
        error.Message.ShouldBe("Error trying to resolve field 'pets5'.");
        error.InnerException.ShouldNotBeNull();
        error.InnerException.Message.ShouldBe("Abstract type Pet must resolve to an Object type at runtime for field Query.pets5 with value '{ name = Eli }', received 'null'.");
    }

    [Fact]
    public void throws_when_unable_to_determine_object_type_nonnull_list_nonnull()
    {
        var result = AssertQueryWithErrors("{ pets6 { name } }", null, expectedErrorCount: 1);
        var error = result.Errors!.First();
        error.Message.ShouldBe("Error trying to resolve field 'pets6'.");
        error.InnerException.ShouldNotBeNull();
        error.InnerException.Message.ShouldBe("Abstract type Pet must resolve to an Object type at runtime for field Query.pets6 with value '{ name = Eli }', received 'null'.");
    }
}

public class PetInterfaceType : InterfaceGraphType
{
    public PetInterfaceType()
    {
        Name = "Pet";
        Field<StringGraphType>("name");
    }
}

public class AbstractQueryType : ObjectGraphType
{
    public AbstractQueryType()
    {
        Name = "Query";
        Field<PetInterfaceType>("pets").Resolve(_ => new { name = "Eli" });
        Field<NonNullGraphType<PetInterfaceType>>("pets2").Resolve(_ => new { name = "Eli" });
        Field<ListGraphType<PetInterfaceType>>("pets3").Resolve(_ => new[] { new { name = "Eli" } });
        Field<ListGraphType<NonNullGraphType<PetInterfaceType>>>("pets4").Resolve(_ => new[] { new { name = "Eli" } });
        Field<NonNullGraphType<ListGraphType<PetInterfaceType>>>("pets5").Resolve(_ => new[] { new { name = "Eli" } });
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<PetInterfaceType>>>>("pets6").Resolve(_ => new[] { new { name = "Eli" } });
    }
}

public class AbstractSchema : Schema
{
    public AbstractSchema()
    {
        Query = new AbstractQueryType();
    }
}
