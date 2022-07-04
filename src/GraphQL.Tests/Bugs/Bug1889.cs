using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug1889WithCovariant : QueryTestBase<CovariantSchema>
{
    [Fact]
    public void supports_covariant_schemas()
    {
        string query = @"query { a { r { value } } }";
        string expected = @"{ ""a"": { ""r"": { ""value"": ""spec"" } } }";

        AssertQuerySuccess(query, expected);
    }
}

public class CovariantSchema : Schema
{
    public CovariantSchema()
    {
        Query = new CovariantQuery();
    }
}

public class CovariantQuery : ObjectGraphType
{
    public CovariantQuery()
    {
        Name = "CovariantQuery";

        Field<NonNullGraphType<SpecializedAGraphType>>("A", resolve: ctx => new SpecializedA());
    }
}

public class R
{
    public string Value = "base";
}

public class SpecializedR : R
{
    public new string Value = "spec";
}

public class A
{
    public R methodUsedToGetRValue() => new R();
}

public class SpecializedA : A
{
    public SpecializedR methodUsedToGetSpecializedRValue() => new SpecializedR();
}

public class RGraphInterface : InterfaceGraphType<R>
{
    public RGraphInterface()
    {
        Name = "RInterface";

        Field<NonNullGraphType<StringGraphType>>("Value", resolve: ctx => ctx.Source.Value);
    }
}

public class SpecializedRGraphType : ObjectGraphType<SpecializedR>
{
    public SpecializedRGraphType()
    {
        Interface<RGraphInterface>();

        Field<NonNullGraphType<StringGraphType>>("Value", resolve: ctx => ctx.Source.Value);
    }
}

public class AGraphInterface : InterfaceGraphType<A>
{
    public AGraphInterface()
    {
        Name = "AInterface";

        Field<NonNullGraphType<RGraphInterface>>("R", resolve: ctx => ctx.Source.methodUsedToGetRValue());
    }
}

public class SpecializedAGraphType : ObjectGraphType<SpecializedA>
{
    public SpecializedAGraphType()
    {
        Interface<AGraphInterface>();

        Field<NonNullGraphType<SpecializedRGraphType>>("R", resolve: ctx => ctx.Source.methodUsedToGetSpecializedRValue());
    }
}
