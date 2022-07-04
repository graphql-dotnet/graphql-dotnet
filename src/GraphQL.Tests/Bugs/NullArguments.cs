using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class NullArguments : QueryTestBase<NullMutationSchema>
{
    [Fact]
    public void Supports_partially_nullable_fields_on_arguments()
    {
        var query = @"
mutation {
  run(input: {id:null, foo:null,bar:null})
}
";
        var expected = @"{
  ""run"": ""idfoobar""
}";
        AssertQuerySuccess(query, expected, null);
    }

    [Fact]
    public void Supports_non_null_int()
    {
        var query = @"
mutation {
  run(input: {id:105, foo:null,bar:{id: null, foo:""a"", bar:{id:101}}})
}
";

        var result = AssertQueryWithErrors(query, null, null, expectedErrorCount: 1, executed: false);

        var caughtError = result.Errors.Single();
        caughtError.ShouldNotBeNull();
        caughtError.InnerException.ShouldBeNull();
        caughtError.Message.Contains("In field \"bar\": In field \"id\": Expected \"Int!\", found null.");
    }

    [Fact]
    public void Supports_non_null_string()
    {
        var query = @"
mutation {
  run(input: {id:105, foo:null,bar:{id: 1, foo:null, bar:{id:101}}})
}
";

        var result = AssertQueryWithErrors(query, null, null, expectedErrorCount: 1, executed: false);

        var caughtError = result.Errors.Single();
        caughtError.ShouldNotBeNull();
        caughtError.InnerException.ShouldBeNull();
        caughtError.Message.Contains("In field \"foo\": Expected \"String!\", found null.");
    }

    [Fact]
    public void Supports_non_null_object()
    {
        var query = @"
mutation {
  run(input: {id:105, foo:null,bar:{id: 1, foo:""abc"", bar:null}})
}
";

        var result = AssertQueryWithErrors(query, null, null, expectedErrorCount: 1, executed: false);

        var caughtError = result.Errors.Single();
        caughtError.ShouldNotBeNull();
        caughtError.InnerException.ShouldBeNull();
        caughtError.Message.Contains("In field \"bar\": Expected \"NonNullSubChild!\", found null.");
    }
}

public class NullMutationSchema : Schema
{
    public NullMutationSchema()
    {
        Mutation = new NullMutation();
    }
}

public class NullMutation : ObjectGraphType
{
    public NullMutation()
    {
        Name = "MyMutation";
        Field<StringGraphType>(
            "run",
            arguments: new QueryArguments(new QueryArgument<NullInputRoot> { Name = "input" }),
            resolve: ctx =>
            {
                var arg = ctx.GetArgument<NullInputClass>("input");
                var r = (arg.Id == null ? "id" : string.Empty) +
                      (arg.Foo == null ? "foo" : string.Empty) +
                      (arg.Bar == null ? "bar" : string.Empty);
                return r;
            });
    }
}

public class NullInputClass
{
    public int? Id { get; set; }
    public string Foo { get; set; }
    public NullInputChildClass Bar { get; set; }
}

public class NullInputChildClass
{
    public int? Id { get; set; }
    public string Foo { get; set; }
    public NullInputSubChildClass Bar { get; set; }
}

public class NullInputSubChildClass
{
    public int? Id { get; set; }
}

public class NullInputRoot : InputObjectGraphType
{
    public NullInputRoot()
    {
        Name = "NullInputRoot";
        Field<IntGraphType>("id");
        Field<StringGraphType>("foo");
        Field<NonNullChild>("bar");
    }
}

public class NonNullChild : InputObjectGraphType
{
    public NonNullChild()
    {
        Name = "NonNullChild";
        Field<NonNullGraphType<IntGraphType>>("id");
        Field<NonNullGraphType<StringGraphType>>("foo");
        Field<NonNullGraphType<NonNullSubChild>>("bar");
    }
}

public class NonNullSubChild : InputObjectGraphType
{
    public NonNullSubChild()
    {
        Name = "NonNullSubChild";
        Field<NonNullGraphType<IntGraphType>>("id");
    }
}
