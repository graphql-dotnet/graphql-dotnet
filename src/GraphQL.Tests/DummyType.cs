using GraphQL.Types;

namespace GraphQL.Tests;

internal class DummyType : ObjectGraphType
{
    public DummyType()
    {
        Name = "Dummy";
        Field<StringGraphType>("dummyField")
            .Resolve(_ => throw new NotImplementedException());
    }
}
