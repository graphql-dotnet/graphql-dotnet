using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/2279
public class Bug2279DuplicateType : QueryTestBase<Bug2279Schema>
{
    [Fact]
    public void NoDuplicateTypeNames()
    {
        var s = new Bug2279Schema();
        var e = Should.Throw<InvalidOperationException>(() => s.Initialize());
        var t1 = typeof(Bug2279GraphType<int>).FullName;
        var t2 = typeof(Bug2279GraphType<string>).FullName;
        e.Message.ShouldBe(@$"Unable to register GraphType 'Bug2279GraphType<Int32>' with the name 'Bug2279GraphType_1'. The name 'Bug2279GraphType_1' is already registered to 'Bug2279GraphType<String>'. Check your schema configuration.");
    }
}

public class Bug2279Schema : Schema
{
    public Bug2279Schema()
    {
        Query = new Bug2279Query();
    }
}

public class Bug2279Query : ObjectGraphType
{
    public Bug2279Query()
    {
        Field<Bug2279GraphType<string>>("string").Resolve(_ => "hello");
        Field<Bug2279GraphType<int>>("int").Resolve(_ => 3);
    }
}

public class Bug2279GraphType<T> : ObjectGraphType<T>
{
    public Bug2279GraphType()
    {
        if (typeof(T) == typeof(int))
        {
            Field<IntGraphType, T>("value").Resolve(x => x.Source);
        }
        else
        {
            Field<StringGraphType, T>("value").Resolve(x => x.Source);
        }
    }
}
