using GraphQL.Types;

namespace GraphQL.Tests.Types;

// https://github.com/graphql/graphql-spec/issues/749
public class ListGraphTypeTests : QueryTestBase<ListSchema>
{
    [Fact]
    public void List_ResolvedType_And_Type_Should_Match()
    {
        var type = new NonNullGraphType<StringGraphType>();
        Should.Throw<ArgumentOutOfRangeException>(() => type.ResolvedType = new IntGraphType()).Message.ShouldStartWith("Type 'StringGraphType' should be assignable from ResolvedType 'IntGraphType'.");
    }

    [Fact]
    public void List_Name_Should_Be_Null()
    {
        new ListGraphType<StringGraphType>().Name.ShouldBeNull();
        new ListGraphType(new StringGraphType()).Name.ShouldBeNull();
    }

    [Fact]
    public void List_Should_Work()
    {
        AssertQuerySuccess(
            "{ list(ints: [1, 2, 3]) }",
            @"{ ""list"": [ 1, 2, 3] }");
    }

    [Fact]
    public void List_As_Scalar_Should_Work()
    {
        AssertQuerySuccess(
            "{ list(ints: 1) }",
            @"{ ""list"": [ 1 ] }");
    }

    [Fact]
    public void Empty_List_Should_Work()
    {
        AssertQuerySuccess(
            "{ list(ints: []) }",
            @"{ ""list"": [ ] }");
    }

    [Fact]
    public void List_Of_Lists_Should_Work()
    {
        AssertQuerySuccess(
            "{ listOfLists(ints: [1, [1, 2], [1, 2, 3], []]) }",
            @"{ ""listOfLists"": [ [1], [1, 2], [1, 2, 3], [] ] }");
    }
}

public class ListSchema : Schema
{
    public ListSchema()
    {
        var query = new ObjectGraphType();

        query.Field<ListGraphType<IntGraphType>>("list")
            .Argument<ListGraphType<IntGraphType>>("ints")
            .Resolve(c =>
            {
                var list = c.GetArgument<List<int>>("ints");
                return list;
            });

        query.Field<ListGraphType<ListGraphType<IntGraphType>>>("listOfLists")
            .Argument<ListGraphType<ListGraphType<IntGraphType>>>("ints")
            .Resolve(c =>
            {
                var list = c.GetArgument<List<List<int>>>("ints");
                return list;
            });

        Query = query;
    }
}
