using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue1127 : QueryTestBase<Issue1127Schema>
{
    [Fact]
    public void Issue1127_Should_Work()
    {
        var query = @"
query {
  getsome(s2: null, s3: ""aaa"" input2: null, input3: { name: ""struct""})
}
";
        var expected = @"{
  ""getsome"": ""completed""
}";
        AssertQuerySuccess(query, expected, null);
    }
}

public class Issue1127Schema : Schema
{
    public Issue1127Schema()
    {
        Query = new Issue127Query();
    }
}

public class Issue127Query : ObjectGraphType
{
    public Issue127Query()
    {
        Field<StringGraphType>("getsome")
            .Arguments(
                new QueryArgument<StringGraphType> { Name = "s1", DefaultValue = "def1" },
                new QueryArgument<StringGraphType> { Name = "s2", DefaultValue = "def2" },
                new QueryArgument<StringGraphType> { Name = "s3", DefaultValue = "def3" },
                new QueryArgument<BaseInputType> { Name = "input1", DefaultValue = 1 },
                new QueryArgument<BaseInputType> { Name = "input2", DefaultValue = 2 },
                new QueryArgument<BaseInputType> { Name = "input3", DefaultValue = 3 }
            )
            .Resolve(ctx =>
            {
                ctx.Arguments["s1"].ShouldBe(new ArgumentValue("def1", ArgumentSource.FieldDefault));
                ctx.Arguments["s2"].ShouldBe(ArgumentValue.NullLiteral);
                ctx.Arguments["s3"].ShouldBe(new ArgumentValue("aaa", ArgumentSource.Literal));

                ctx.Arguments["input1"].ShouldBe(new ArgumentValue(1, ArgumentSource.FieldDefault));
                ctx.Arguments["input2"].ShouldBe(ArgumentValue.NullLiteral);
                ctx.Arguments["input3"].Value.ShouldNotBeNull();
                ctx.Arguments["input3"].Source.ShouldBe(ArgumentSource.Literal);

                (ctx.Arguments["input3"].Value as Dictionary<string, object>)["name"].ShouldBe("struct");
                (ctx.Arguments["input3"].Value as Dictionary<string, object>)["created"].ShouldBe(new DateTime(2000, 1, 1));
                (ctx.Arguments["input3"].Value as Dictionary<string, object>)["lastModified"].ShouldBe(new DateTime(2001, 1, 1));

                return "completed";
            });
    }
}

public class BaseInputType : InputObjectGraphType
{
    public BaseInputType()
    {
        Field<StringGraphType>("name");

        Field<DateTimeGraphType>("created");
        Field<DateTimeGraphType>("lastModified");

        Fields.SingleOrDefault(f => f.Name == "created").DefaultValue = new DateTime(2000, 1, 1);
        Fields.SingleOrDefault(f => f.Name == "lastModified").DefaultValue = new DateTime(2001, 1, 1);
    }
}
