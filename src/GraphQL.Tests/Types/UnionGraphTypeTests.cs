using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class UnionGraphTypeTests
{
    /// <summary>
    /// This test ensures that ResolveType can pull instances by the CLR type of the graph type from the schema.
    /// </summary>
    [Fact]
    public async Task ResolveTypeDelegate()
    {
        var schema = new Schema();
        var queryType = new ObjectGraphType
        {
            Name = "Query",
        };
        queryType.Field<MyUnion>("Test1").Resolve(context => new Class1 { Name1 = "hello" });
        queryType.Field<MyUnion>("Test2").Resolve(context => new Class2 { Name2 = "hello" });
        schema.Query = queryType;
        schema.Initialize();

        var ret = await schema.ExecuteAsync(o =>
        {
            o.Schema = schema;
            o.Query = "{ test1 { ...union } test2 { ...union } } fragment union on MyUnion { ... on Class1Type { name1 } ... on Class2Type { name2 } }";
        }).ConfigureAwait(false);

        ret.ShouldBeCrossPlatJson("{\"data\":{\"test1\":{\"name1\":\"hello\"},\"test2\":{\"name2\":\"hello\"}}}");
    }

    private class MyUnion : UnionGraphType
    {
        public MyUnion()
        {
            Type<Class1Type>();
            Type<Class2Type>();
            ResolveType = (obj, schema) => obj switch
            {
                Class1 _ => schema.AllTypes[typeof(Class1Type)] as IObjectGraphType,
                Class2 _ => schema.AllTypes["Class2Type"] as IObjectGraphType,
                _ => null,
            };
        }
    }

    private class Class1
    {
        public string Name1 { get; set; }
    }

    private class Class2
    {
        public string Name2 { get; set; }
    }

    public class Class1Type : ObjectGraphType
    {
        public Class1Type()
        {
            Field<StringGraphType>("Name1");
        }
    }

    public class Class2Type : ObjectGraphType
    {
        public Class2Type()
        {
            Field<StringGraphType>("Name2");
        }
    }
}
