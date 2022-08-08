using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class UnionGraphTypeTests
{
    [Fact]
    public void cannot_initialize_same_instance_twice()
    {
        var unionType = new UnionGraphType
        {
            Name = "UnionType",
            Types = new[] { typeof(Type1), typeof(Type2) }
        };
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field("union", unionType);
        var schema = new Schema() { Query = queryType };
        schema.Initialize();

        var queryType2 = new ObjectGraphType { Name = "Query" };
        queryType2.Field("union", unionType);
        var schema2 = new Schema() { Query = queryType2 };
        Should.Throw<InvalidOperationException>(
            () => schema2.Initialize())
            .Message.ShouldBe("This graph type 'UnionType' has already been initialized. Make sure that you do not use the same instance of a graph type in multiple schemas. It may be so if you registered this graph type as singleton; see https://graphql-dotnet.github.io/docs/getting-started/dependency-injection/ for more info.");
    }

    public class Type1 : ObjectGraphType
    {
        public Type1()
        {
            Field<IntGraphType>("field1");

            IsTypeOf = _ => true;
        }
    }

    public class Type2 : ObjectGraphType
    {
        public Type2()
        {
            Field<IntGraphType>("field1");

            IsTypeOf = _ => false;
        }
    }
}
