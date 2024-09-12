using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug4059 : QueryTestBase<Bug4059Schema>
{
    [Fact]
    public void TestRead()
    {
        Schema.Features.DeprecationOfInputValues = false;
        AssertQuerySuccess("""
        {
           __type(name: "Bug4059Type"){
            fields {
              args {
                name
              }
            }
          }
        }
        """,
        """{ "__type": { "fields": [ { "args": [ { "name": "arg1" }, { "name": "arg2" } ] } ] } }""");
    }

    [Fact]
    public void TestReadWithDeprecatedInputValues()
    {
        Schema.Features.DeprecationOfInputValues = true;
        AssertQuerySuccess("""
        {
           __type(name: "Bug4059Type"){
            fields {
              args(includeDeprecated: true) {
                name
              }
            }
          }
        }
        """,
        """{ "__type": { "fields": [ { "args": [ { "name": "arg1" }, { "name": "arg2" } ] } ] } }""");
    }

    [Fact]
    public void TestReadWithDeprecatedInputValues2()
    {
        Schema.Features.DeprecationOfInputValues = true;
        AssertQuerySuccess("""
        {
           __type(name: "Bug4059Type"){
            fields {
              args {
                name
              }
            }
          }
        }
        """,
        """{ "__type": { "fields": [ { "args": [ { "name": "arg2" } ] } ] } }""");
    }
}

public class Bug4059Schema : Schema
{
    public Bug4059Schema()
    {
        Query = new Bug4059Query();
    }
}

public class Bug4059Query : ObjectGraphType
{
    public Bug4059Query()
    {
        Field<Bug4059GraphType>("test")
            .Resolve(_ => null);
    }
}

public class Bug4059GraphType : ObjectGraphType
{
    public Bug4059GraphType()
    {
        Name = "Bug4059Type";
        Field<StringGraphType>("test1")
            .Argument<StringGraphType>("arg1", arg => arg.DeprecationReason = "because")
            .Argument<StringGraphType>("arg2")
            .Resolve(_ => null);
    }
}
