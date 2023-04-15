using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue1927ResolvedTypeWithType
{
    [Fact]
    public void test_outputs()
    {
        var innerObject = new ObjectGraphType() { Name = "inner" };
        innerObject.AddField(new ObjectFieldType { Name = "test", ResolvedType = new StringGraphType(), Type = typeof(StringGraphType) });
        var list = new ListGraphType(innerObject);
        var obj = new ObjectGraphType();
        obj.AddField(new ObjectFieldType { Name = "list", ResolvedType = list, Type = list.GetType() });
        var schema = new Schema
        {
            Query = obj
        };
        schema.Initialize();
    }

    [Fact]
    public void test_inputs()
    {
        var innerObject = new InputObjectGraphType() { Name = "inner" };
        innerObject.AddField(new InputFieldType { Name = "test", ResolvedType = new StringGraphType(), Type = typeof(StringGraphType) });
        var list = new ListGraphType(innerObject);
        var inputObj = new InputObjectGraphType();
        inputObj.AddField(new InputFieldType { Name = "list", ResolvedType = list, Type = list.GetType() });
        var obj = new ObjectGraphType();
        var field = new ObjectFieldType
        {
            Name = "hello",
            ResolvedType = new StringGraphType(),
            Type = typeof(StringGraphType),
            Arguments = new QueryArguments { new QueryArgument(inputObj) { Name = "input" } }
        };
        obj.AddField(field);
        var schema = new Schema
        {
            Query = obj
        };
        schema.Initialize();
    }

    [Fact]
    public void test_fieldtype_type()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ObjectFieldType { Type = typeof(string) });
    }
}
