using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Issue1927ResolvedTypeWithType
{
    [Fact]
    public void test_outputs()
    {
        var innerObject = new ObjectGraphType();
        innerObject.AddField(new FieldType { Name = "test", ResolvedType = new StringGraphType(), Type = typeof(StringGraphType) });
        var list = new ListGraphType(innerObject);
        var obj = new ObjectGraphType();
        obj.AddField(new FieldType { Name = "list", ResolvedType = list, Type = list.GetType() });
        var schema = new Schema
        {
            Query = obj
        };
        schema.Initialize();
    }

    [Fact]
    public void test_inputs()
    {
        var innerObject = new InputObjectGraphType();
        innerObject.AddField(new FieldType { Name = "test", ResolvedType = new StringGraphType(), Type = typeof(StringGraphType) });
        var list = new ListGraphType(innerObject);
        var inputObj = new InputObjectGraphType();
        inputObj.AddField(new FieldType { Name = "list", ResolvedType = list, Type = list.GetType() });
        var obj = new ObjectGraphType();
        var field = new FieldType
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
        Should.Throw<ArgumentOutOfRangeException>(() => new FieldType { Type = typeof(string) });
    }
}
