using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue1004 : QueryTestBase<DescriptionFromInterfaceSchema>
{
    [Fact]
    public void Should_Return_Field_Description_From_Interface_If_Not_Overridden()
    {
        var query = @"
{
  __type(name: ""Query"")
  {
    fields
    {
      description
    }
  }
}
";
        var expected = @"{
  ""__type"": {
    ""fields"": [
      {
        ""description"": ""Very important field1""
      },
      {
        ""description"": ""Not so important""
      }
    ]
  }
}";
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
        Field<StringGraphType>("field1", resolve: ctx => throw null);
        Field<StringGraphType>("field2", description: "Not so important", resolve: ctx => throw null);
        Interface<Issue1004Interface>();
    }
}

public class Issue1004Interface : InterfaceGraphType
{
    public Issue1004Interface()
    {
        Field<StringGraphType>("field1", "Very important field1");
        Field<StringGraphType>("field2", "Very important field2");
    }
}
