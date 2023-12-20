using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/3825
public class Bug3825 : QueryTestBase<Bug3825Schema>
{
    [Fact]
    public void TestRead()
    {
        Schema.Features.DeprecationOfInputValues = false;
        AssertQuerySuccess("""
        {
            __type(name: "Bug3825Input") {
                name
                inputFields {
                    name
                }
            }
        }
        """,
        """{ "__type": { "name": "Bug3825Input", "inputFields": [ { "name": "test1" }, { "name": "test2" } ] } }""");
    }

    [Fact]
    public void TestReadWithDeprecatedInputValues()
    {
        Schema.Features.DeprecationOfInputValues = true;
        AssertQuerySuccess("""
        {
            __type(name: "Bug3825Input") {
                name
                inputFields(includeDeprecated: true) {
                    name
                }
            }
        }
        """,
        """{ "__type": { "name": "Bug3825Input", "inputFields": [ { "name": "test1" }, { "name": "test2" } ] } }""");
    }

    [Fact]
    public void TestReadWithDeprecatedInputValues2()
    {
        Schema.Features.DeprecationOfInputValues = true;
        AssertQuerySuccess("""
        {
            __type(name: "Bug3825Input") {
                name
                inputFields {
                    name
                }
            }
        }
        """,
        """{ "__type": { "name": "Bug3825Input", "inputFields": [ { "name": "test2" } ] } }""");
    }
}

public class Bug3825Schema : Schema
{
    public Bug3825Schema()
    {
        Query = new Bug3825Query();
    }
}

public class Bug3825Query : ObjectGraphType
{
    public Bug3825Query()
    {
        Field<StringGraphType>("test")
            .Argument(typeof(Bug3825InputGraphType), "arg")
            .Resolve(_ => null);
    }
}

public class Bug3825InputGraphType : InputObjectGraphType
{
    public Bug3825InputGraphType()
    {
        Name = "Bug3825Input";
        Field<StringGraphType>("test1").DeprecationReason("because");
        Field<StringGraphType>("test2");
    }
}
