using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/3046
public class Bug3046 : QueryTestBase<Bug3046.MySchema>
{
    [Fact]
    public void FailsSchemaInitialization()
    {
        Should.Throw<InvalidOperationException>(() => new MySchema())
            .Message.ShouldBe("Please use the proper FuncFieldResolver constructor for asynchronous delegates, or call FieldAsync when adding your field to the graph.");
    }

    [Fact]
    public void CreateDelegateThrows()
    {
        Should.Throw<InvalidOperationException>(() => new FuncFieldResolver<object>(MyFunc))
            .Message.ShouldBe("Please use the proper FuncFieldResolver constructor for asynchronous delegates, or call FieldAsync when adding your field to the graph.");

        static Task<string> MyFunc(IResolveFieldContext context) => null!;
    }

    [Fact]
    public void CreateDelegateThrows2()
    {
        Should.Throw<InvalidOperationException>(() => new FuncFieldResolver<string, object>(MyFunc))
            .Message.ShouldBe("Please use the proper FuncFieldResolver constructor for asynchronous delegates, or call FieldAsync when adding your field to the graph.");

        static Task<string> MyFunc(IResolveFieldContext<string> context) => null!;
    }

    public class MySchema : Schema
    {
        public MySchema()
        {
            Query = new MyQuery();
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            // note: the exception is thrown during execution of this instruction, so debugging can easily identify the incorrect code
            Field<StringGraphType>("test1").Resolve(HelloAsync);
        }

        public virtual Task<string> HelloAsync(IResolveFieldContext context)
            => Task.FromResult("Hello");
    }
}
