using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug1156 : QueryTestBase<Bug1156Schema>
{
    [Fact]
    public void duplicated_type_names_should_throw_error()
    {
        var query = @"
{
    type1 {
        field1A
        field1B
    }

    type2 {
        field2A
        field2B
    }
}";
        var result = AssertQueryWithErrors(query, null, expectedErrorCount: 1, executed: false);
        result.Errors[0].Message.ShouldBe("Error executing document.");
        result.Errors[0].InnerException.Message.ShouldBe(@"Unable to register GraphType 'Type2' with the name 'MyType'. The name 'MyType' is already registered to 'Type1'. Check your schema configuration.");
    }
}

public sealed class Type1 : ObjectGraphType
{
    public Type1()
    {
        Name = "MyType";

        Field<StringGraphType>("Field1A").Resolve(_ => "Field1A Value");
        Field<StringGraphType>("Field1B").Resolve(_ => "Field1B Value");
    }
}

public sealed class Type2 : ObjectGraphType
{
    public Type2()
    {
        Name = "MyType";

        Field<StringGraphType>("Field2A").Resolve(_ => "Field2A Value");
        Field<StringGraphType>("Field2B").Resolve(_ => "Field1B Value");
    }
}

public sealed class QueryType : ObjectGraphType
{
    public QueryType()
    {
        Field<Type1>("type1").Resolve(x => new { });
        Field<Type2>("type2").Resolve(x => new { });
    }
}

public class Bug1156Schema : Schema
{
    public Bug1156Schema()
    {
        Query = new QueryType();
    }
}
