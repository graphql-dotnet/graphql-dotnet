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

    public class Type1 : ObjectGraphType<Model1>
    {
        public Type1()
        {
            Field(x => x.Field1);
        }
    }

    public class Type2 : ObjectGraphType<Model2>
    {
        public Type2()
        {
            Field(x => x.Field2);
        }
    }

    public class Model1
    {
        public int Field1 => 1;
    }

    public class Model2
    {
        public int Field2 => 2;
    }

    [Fact]
    public async Task supports_graphql_type_references()
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        var unionType = new UnionGraphType
        {
            Name = "UnionType",
            Types = new[] { typeof(GraphQLClrOutputTypeReference<Model1>) }
        };
        unionType.AddPossibleType(new GraphQLTypeReference("Type2"));
        queryType.Field("union1", unionType, resolve: _ => new Model1());
        queryType.Field("union2", unionType, resolve: _ => new Model2());

        var schema = new Schema() { Query = queryType };
        schema.RegisterTypeMapping<Model1, Type1>();
        schema.RegisterType<Type2>();
        schema.Initialize();

        var str1 = await schema.ExecuteAsync(o => o.Query = "{ union1 { ...frag } union2 { ...frag } } fragment frag on UnionType { ... on Type1 { field1 } ... on Type2 { field2 } }").ConfigureAwait(false);
        str1.ShouldBeCrossPlatJson(@"{""data"":{""union1"":{""field1"":1},""union2"":{""field2"":2}}}");
    }

    [Fact]
    public void invalid_type_reference_throws()
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        var unionType = new UnionGraphType
        {
            Name = "UnionType",
            Types = new[] { typeof(GraphQLClrOutputTypeReference<Model1>) }
        };
        queryType.Field("union1", unionType, resolve: _ => new Model1());

        var schema = new Schema() { Query = queryType };
        Should.Throw<InvalidOperationException>(() => schema.Initialize())
            .Message.ShouldBe("The GraphQL type 'GraphQLClrOutputTypeReference<Model1>' for union graph type 'UnionType' could not be derived implicitly. Could not find type mapping from CLR type 'GraphQL.Tests.Types.UnionGraphTypeTests+Model1' to GraphType. Did you forget to register the type mapping with the 'ISchema.RegisterTypeMapping'?");
    }
}
