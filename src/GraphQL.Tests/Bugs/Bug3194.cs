using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/3194
public class Bug3194
{
    [Fact]
    public async Task InheritedMethodsWork()
    {
        var schema = Schema.For(
            @"
schema {
  query: Query
}

type Query {
  hello: String!
}
",
            c => c.Types.Include(typeof(QueryType)));
        schema.Initialize();
        var queryType = schema.AllTypes["Query"].ShouldBeAssignableTo<IComplexGraphType>();
        var fieldType = queryType.Fields.Find("hello").ShouldNotBeNull();
        var resolver = fieldType.Resolver.ShouldNotBeNull();
        var result = await resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false);
        result.ShouldBe("World");
    }

    [GraphQLMetadata(Name = "Query")]
    private class QueryType : BaseType
    {
    }

    private abstract class BaseType
    {
        public string Hello() => "World";
    }
}
