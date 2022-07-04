using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/1837
public class Issue1837 : QueryTestBase<Issue1837Schema>
{
    [Fact]
    public void Issue1837_Should_Work()
    {
        new Issue1837Schema().Initialize();
    }
}

public class Issue1837Schema : Schema
{
    public Issue1837Schema()
    {
        Query = new Issue1837Query();
    }
}

public class Issue1837Query : ObjectGraphType
{
    public Issue1837Query()
    {
        Field<ListGraphType<IntGraphType>>(
            "getsome",
            arguments: new QueryArguments(
                new QueryArgument<Issue1837ArrayInputType> { Name = "input" }
            ),
            resolve: ctx =>
            {
                _ = ctx.GetArgument<Issue1837ArrayInput>("input");

                return null;
            });
    }
}

public class Issue1837ArrayInput
{
    public int[] Abc { get; set; }
}

public class OutputIntGraphType : ObjectGraphType
{
    public OutputIntGraphType()
    {
        Field<IntGraphType>("value", resolve: _ => 1);
    }
}

public class Issue1837ArrayInputType : InputObjectGraphType
{
    public Issue1837ArrayInputType()
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() =>
        {
            AddField(new FieldType
            {
                ResolvedType = new ListGraphType<OutputIntGraphType>(),
                Name = "abc"
            });
        });
        ex.Message.ShouldStartWith("Input type 'Issue1837ArrayInputType' can have fields only of input types: ScalarGraphType, EnumerationGraphType or IInputObjectGraphType. Field 'abc' has an output type.");

        AddField(new FieldType
        {
            ResolvedType = new ListGraphType<IntGraphType> { ResolvedType = new IntGraphType() },
            Name = "abc"
        });
    }
}
