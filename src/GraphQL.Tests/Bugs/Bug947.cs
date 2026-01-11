using System.Numerics;
using GraphQL.Execution;
using GraphQL.Federation.Types;
using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug947
{
    [Fact]
    public void GetArgument_Should_Return_Properly_Converted_Values()
    {
        var inputObjectGraphType = new InputObjectGraphType<SomeObject>();
        inputObjectGraphType.Field(x => x.inner_int).Type(new IntGraphType());
        inputObjectGraphType.Field(x => x.inner_string).Type(new StringGraphType());

        var context = new ResolveFieldContext
        {
            Arguments = new Dictionary<string, ArgumentValue>
            {
                { "int", new ArgumentValue(10, ArgumentSource.Literal) },
                { "string", new ArgumentValue("hello", ArgumentSource.Literal) },
                { "vector", new ArgumentValue(new Vector3(1.1f, 2.2f, 3.3f), ArgumentSource.Literal) },
                { "object", new ArgumentValue(new Dictionary<string, object>
                            {
                                { "inner_int", 15 },
                                { "inner_string", "ok" }
                            }, ArgumentSource.Literal)
                }
            },
            FieldDefinition = new FieldType
            {
                Arguments =
                [
                    new QueryArgument(new IntGraphType()) { Name = "int" },
                    new QueryArgument(new StringGraphType()) { Name = "string" },
                    new QueryArgument(new AnyScalarGraphType()) { Name = "vector" },
                    new QueryArgument(inputObjectGraphType) { Name = "object" },
                ],
            },
            Schema = new Schema(),
        };

        // int arg
        context.GetArgument("int", 100).ShouldBe(10);
        context.GetArgument<object>("int").ShouldBe(10);
        context.GetArgument("ints", 100).ShouldBe(100);

        // Vector3 arg
        context.GetArgument("vector", Vector3.One).ShouldBe(new Vector3(1.1f, 2.2f, 3.3f));
        context.GetArgument<object>("vector").ShouldBe(new Vector3(1.1f, 2.2f, 3.3f));
        context.GetArgument("vectors", Vector3.One).ShouldBe(Vector3.One);

        // string arg
        context.GetArgument("string", "bye").ShouldBe("hello");
        context.GetArgument<object>("string").ShouldBe("hello");
        context.GetArgument("strong", "bye").ShouldBe("bye");
        Should.Throw<InvalidOperationException>(() => context.GetArgument<ResolveFieldContext>("string"));

        // object arg
        context.GetArgument<object>("object").ShouldBeOfType<Dictionary<string, object>>();
        context.GetArgument<SomeObject>("object").inner_int.ShouldBe(15);
        Should.Throw<InvalidOperationException>(() => context.GetArgument<int>("object"));
        Should.Throw<InvalidOperationException>(() => context.GetArgument<string>("object"));
        Should.Throw<InvalidOperationException>(() => context.GetArgument<DateTime>("object"));

        Should.Throw<InvalidOperationException>(() => context.GetArgument<SomeOtherObject>("object"));
    }
}

public class SomeObject
{
    public int inner_int { get; set; }

    public string inner_string { get; set; }
}

public class SomeOtherObject
{
    public int unknown { get; set; }

    public string unknown2 { get; set; }
}
