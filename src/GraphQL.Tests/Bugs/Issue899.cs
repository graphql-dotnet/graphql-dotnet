using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue899 : QueryTestBase<Issue899Schema>
{
    [Fact]
    public void Issue899_Should_Work()
    {
        var query = @"
query {
  level1(arg1: ""1"") {
    level2(arg2: ""2"") {
      level3(arg3: ""3"") {
        level4(arg4: ""4"")
      }
    }
  }
}
";
        var expected = @"{
  ""level1"": {
    ""level2"": [[{
      ""level3"": [{
        ""level4"": ""X""
      }]
    }]]
  }
}";
        AssertQuerySuccess(query, expected, null);
    }
}

public class Issue899Schema : Schema
{
    public Issue899Schema()
    {
        Query = new Issue899Query();
    }
}

public class Issue899Query : ObjectGraphType
{
    public Issue899Query()
    {
        Field<Issue899Level1>("level1", resolve: context =>
        {
            context.GetArgument<string>("arg1").ShouldBe("1");
            context.Parent.ShouldBeNull();

            return new object();
        },
        arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "arg1" }));
    }
}

public class Issue899Level1 : ObjectGraphType
{
    public Issue899Level1()
    {
        Field<ListGraphType<ListGraphType<Issue899Level2>>>("level2", resolve: context =>
        {
            context.GetArgument<string>("arg2").ShouldBe("2");
            context.Parent.GetArgument<string>("arg1").ShouldBe("1");
            context.Parent.Parent.ShouldBeNull();

            return new[] { new[] { new object() } };
        },
        arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "arg2" }));
    }
}

public class Issue899Level2 : ObjectGraphType
{
    public Issue899Level2()
    {
        Field<ListGraphType<Issue899Level3>>("level3", resolve: context =>
        {
            context.GetArgument<string>("arg3").ShouldBe("3");
            context.Parent.GetArgument<string>("arg2").ShouldBe("2");
            context.Parent.Parent.GetArgument<string>("arg1").ShouldBe("1");
            context.Parent.Parent.Parent.ShouldBeNull();

            return new[] { new object() };
        },
        arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "arg3" }));
    }
}

public class Issue899Level3 : ObjectGraphType
{
    public Issue899Level3()
    {
        Field<StringGraphType>("level4", resolve: context =>
        {
            context.GetArgument<string>("arg4").ShouldBe("4");
            context.Parent.GetArgument<string>("arg3").ShouldBe("3");
            context.Parent.Parent.GetArgument<string>("arg2").ShouldBe("2");
            context.Parent.Parent.Parent.GetArgument<string>("arg1").ShouldBe("1");
            context.Parent.Parent.Parent.Parent.ShouldBeNull();

            return "X";
        },
        arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "arg4" }));
    }
}
