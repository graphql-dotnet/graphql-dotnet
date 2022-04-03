using GraphQL.Execution;

namespace GraphQL.Tests.Bugs;

public class Bug1626
{
    [Fact]
    public void GetArgument_Should_Not_Throw_AmbiguousMatchException()
    {
        var context = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>
            {
                ["root"] = new ArgumentValue(new Dictionary<string, object>
                {
                    ["MyChildProperty"] = new Dictionary<string, object>
                    {
                        ["MyProperty"] = "graphql"
                    }
                }, ArgumentSource.Literal)
            }
        };

        var arg = context.GetArgument<MyDerivedType>("root");

        arg.MyChildProperty.ShouldNotBeNull();
        (arg as MyBaseType).MyChildProperty.ShouldBeNull();

        arg.MyChildProperty.MyProperty.ShouldBe("graphql");
        (arg.MyChildProperty as MyChildBaseType).MyProperty.ShouldBeNull();
    }

    private class MyBaseType
    {
        public MyChildBaseType MyChildProperty { get; set; }
    }

    private class MyDerivedType : MyBaseType
    {
        public new MyChildDerivedType MyChildProperty { get; set; }
    }

    private class MyChildBaseType
    {
        public string MyProperty { get; set; }
    }

    private class MyChildDerivedType : MyChildBaseType
    {
        public new string MyProperty { get; set; }
    }
}
