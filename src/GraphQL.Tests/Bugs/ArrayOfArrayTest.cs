using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class ArrayOfArrayTest : QueryTestBase<ArrayOfArraySchema>
{
    [Fact]
    public void ArrayOfArray_Should_Return_As_Is()
    {
        const string query = """
mutation {
  create(input: {ints: [[1],[2,2],[3,3,3]] })
  {
    ints
  }
}
""";
        const string expected = """
{
  "create": {
    "ints": [[1],[2,2],[3,3,3]]
  }
}
""";
        AssertQuerySuccess(query, expected, null);
    }
}

public class ArrayOfArraySchema : Schema
{
    public ArrayOfArraySchema()
    {
        Mutation = new ArrayOfArrayMutation();
    }
}

public class ArrayOfArrayModel
{
    public int[][] ints { get; set; }
}

public class ArrayOfArrayInput : InputObjectGraphType<ArrayOfArrayModel>
{
    public ArrayOfArrayInput()
    {
        Field(o => o.ints);
    }
}

public class ArrayOfArrayType : ObjectGraphType<ArrayOfArrayModel>
{
    public ArrayOfArrayType()
    {
        Field(o => o.ints);
    }
}

public class ArrayOfArrayMutation : ObjectGraphType
{
    public ArrayOfArrayMutation()
    {
        Field<ArrayOfArrayType>("create")
            .Argument<ArrayOfArrayInput>("input")
            .Resolve(ctx =>
            {
                var arg = ctx.GetArgument<ArrayOfArrayModel>("input");
                return arg;
            });
    }
}
