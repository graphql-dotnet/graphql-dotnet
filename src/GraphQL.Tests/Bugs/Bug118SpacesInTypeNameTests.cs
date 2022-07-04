using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug118SpacesInTypeNameTests : QueryTestBase<MutationSchema>
{
    [Fact]
    public void supports_partially_nullable_inputs_when_parent_non_null()
    {
        var inputs = @"{ ""input_0"": { ""id"": ""123"", ""foo"": null, ""bar"": null } }".ToInputs();
        var query = @"
mutation M($input_0: MyInput!) {
  run(input: $input_0)
}
";
        var expected = @"{ ""run"": ""123"" }";
        AssertQuerySuccess(query, expected, inputs);
    }
}

public class MutationSchema : Schema
{
    public MutationSchema()
    {
        Mutation = new MyMutation();
    }
}

public class MyMutation : ObjectGraphType
{
    public MyMutation()
    {
        Field<StringGraphType>(
            "run",
            arguments: new QueryArguments(new QueryArgument<MyInput> { Name = "input" }),
            resolve: ctx => ctx.GetArgument<MyInputClass>("input").Id);
    }
}

public class MyInputClass
{
    public string Id { get; set; }
    public string Foo { get; set; }
    public string Bar { get; set; }
}

public class MyInput : InputObjectGraphType
{
    public MyInput()
    {
        Name = "MyInput"; // changed from "MyInput "
        Field<NonNullGraphType<StringGraphType>>("id");
        Field<StringGraphType>("foo");
        Field<StringGraphType>("bar");
    }
}
