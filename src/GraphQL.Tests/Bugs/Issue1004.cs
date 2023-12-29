using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue1004 : QueryTestBase<DescriptionFromInterfaceSchema>
{
    [Fact]
    public void Should_Return_Field_Description_From_Interface_If_Not_Overridden()
    {
        const string query = """
{
  __type(name: "Query")
  {
    fields
    {
      description
    }
  }
}
""";
        const string expected = """
{
  "__type": {
    "fields": [
      {
        "description": "Very important field1"
      },
      {
        "description": "Not so important"
      },
      {
        "description": null
      }
    ]
  }
}
""";
        AssertQuerySuccess(query, expected, null);
    }
}

public class DescriptionFromInterfaceSchema : Schema
{
    public DescriptionFromInterfaceSchema()
    {
        Query = new Issue1004Query();
    }
}

public class Issue1004Query : ObjectGraphType
{
    public Issue1004Query()
    {
        Name = "Query";
        IsTypeOf = o => true;
        Field<StringGraphType>("field1").Resolve(_ => throw null!);
        Field<StringGraphType>("field2").Description("Not so important").Resolve(_ => throw null!);
        Field<StringGraphType>("nonInterfaceField").Resolve(_ => throw null!); // https://github.com/graphql-dotnet/graphql-dotnet/pull/3352
        Interface<Issue1004Interface>();
    }
}

public class Issue1004Interface : InterfaceGraphType
{
    public Issue1004Interface()
    {
        Field<StringGraphType>("field1").Description("Very important field1");
        Field<StringGraphType>("field2").Description("Very important field2");
    }
}
