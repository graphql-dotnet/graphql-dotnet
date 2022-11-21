using GraphQL.Types;

namespace GraphQL.Tests.Execution;

public class AbstractTypeErrorTests : QueryTestBase<AbstractSchema>
{
    [Fact]
    public void throws_when_unable_to_determine_object_type()
    {
        var result = AssertQueryWithErrors("{ pets { name } }", """{ "pets": null }""", expectedErrorCount: 1);
        var error = result.Errors.First();
        error.Message.ShouldBe("Error trying to resolve field 'pets'.");
        error.InnerException.ShouldNotBeNull();
        error.InnerException.Message.ShouldBe("Abstract type Pet must resolve to an Object type at runtime for field Query.pets with value '{ name = Eli }', received 'null'.");
    }

    [Fact]
    public void throws_when_unable_to_determine_object_type_nullable()
    {
        var result = AssertQueryWithErrors("{ pets2 { name } }", """{ "pets2": null }""", expectedErrorCount: 1);
        var error = result.Errors.First();
        error.Message.ShouldBe("Error trying to resolve field 'pets2'.");
        error.InnerException.ShouldNotBeNull();
        error.InnerException.Message.ShouldBe("Abstract type Pet must resolve to an Object type at runtime for field Query.pets2 with value '{ name = Eli }', received 'null'.");
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
    }
}

public class AbstractSchema : Schema
{
    public AbstractSchema()
    {
        Query = new AbstractQueryType();
    }
}
