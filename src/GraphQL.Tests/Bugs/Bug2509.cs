using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/2509
public class RecordTest : QueryTestBase<RecordSchema>
{
    [Fact]
    public void Record_Should_Return_As_Is()
    {
        var query = @"
{
  search(input: {})
  {
    id
  }
}
";
        var expected = @"{
  ""search"": {
    ""id"": null
  }
}";
        AssertQuerySuccess(query, expected, null);
    }
}

public class RecordSchema : Schema
{
    public RecordSchema()
    {
        Query = new RecordQuery();
    }
}

public record RecordModel(Guid? Id = null);

public class RecordInput : InputObjectGraphType<RecordModel>
{
    public RecordInput()
    {
        Field(o => o.Id, nullable: true);
    }
}

public class RecordType : ObjectGraphType<RecordModel>
{
    public RecordType()
    {
        Field(o => o.Id, nullable: true);
    }
}

public class RecordQuery : ObjectGraphType
{
    public RecordQuery()
    {
        Field<RecordType>(
            "search",
            arguments: new QueryArguments(new QueryArgument<RecordInput> { Name = "input" }),
            resolve: ctx =>
            {
                var arg = ctx.GetArgument<RecordModel>("input");
                return arg;
            });
    }
}
