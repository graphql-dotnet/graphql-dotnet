using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/1285
public class Issue1285 : QueryTestBase<Issue1285Schema>
{
    [Fact]
    public void Issue1285_Should_Work()
    {
        var query = @"
query {
  getsome(input: { readOnlyProp: 7, privateSetProp: 3, valueProp: null, ints: null, ints2: [1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0], intsList: null, intsList2: [1,2,3,4,5,6,7,8,9,0,1,2,3,4,5,6,7,8,9,0] })
}
";
        var expected = @"{
  ""getsome"": null
}";
        AssertQuerySuccess(query, expected, null);
    }
}

public class Issue1285Schema : Schema
{
    public Issue1285Schema()
    {
        Query = new Issue1285Query();
    }
}

public class Issue1285Query : ObjectGraphType
{
    public Issue1285Query()
    {
        Field<ListGraphType<IntGraphType>>("getsome")
            .Argument<ArrayInputType>("input")
            .Resolve(ctx =>
            {
                var arg = ctx.GetArgument<ArrayInput>("input");

                arg.Ints.ShouldBeNull();
                arg.Ints2.ShouldNotBeNull();
                arg.Ints2.Length.ShouldBe(20);

                arg.IntsList.ShouldBeNull();
                arg.IntsList2.ShouldNotBeNull();
                arg.IntsList2.Count.ShouldBe(20);

                arg.ValueProp.ShouldBe(0);
                arg.ReadOnlyProp.ShouldBe(7);
                arg.PrivateSetProp.ShouldBe(3);

                return arg.Ints;
            });
    }
}

public class ArrayInput
{
    private ArrayInput(int readOnlyProP)
    {
        ReadOnlyProp = readOnlyProP;
    }

    public int[] Ints { get; set; }

    public int[] Ints2 { get; set; }

    public List<int> IntsList { get; set; }

    public List<int> IntsList2 { get; set; }

    public int ValueProp { get; set; }

    public int ReadOnlyProp { get; }

    public int PrivateSetProp { get; private set; }
}

public class ArrayInputType : InputObjectGraphType<ArrayInput>
{
    public ArrayInputType()
    {
        Field(o => o.Ints, nullable: true);
        Field(o => o.Ints2, nullable: true);
        Field(o => o.IntsList, nullable: true);
        Field(o => o.IntsList2, nullable: true);
        Field(o => o.ValueProp, nullable: true);
        Field(o => o.ReadOnlyProp, nullable: true);
        Field(o => o.PrivateSetProp, nullable: true);
    }
}
